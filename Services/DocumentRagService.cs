using AiHelper.Data;
using AiHelper.Models;
using Markdig;
using Markdig.Syntax;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using Spectre.Console;
using System.Security.Cryptography;
using System.Text;

namespace AiHelper.Services;

public class DocumentRagService(AiHelperDbContext _dbContext, IOllamaService _ollamaService) : IDocumentRagService
{
    //private readonly AiHelperDbContext _dbContext = dbContext;
    //private readonly IOllamaService _ollamaService = ollamaService;

    // ===== Управление базами знаний =====

    public async Task<KnowledgeBase> CreateKnowledgeBaseAsync(Guid userId, string name, string? description = null, bool isPublic = false)
    {
        var kb = new KnowledgeBase
        {
            UserId = userId,
            Name = name,
            Description = description,
            IsPublic = isPublic
        };

        _dbContext.KnowledgeBases.Add(kb);
        await _dbContext.SaveChangesAsync();
        return kb;
    }

    public async Task<List<KnowledgeBase>> GetUserKnowledgeBasesAsync(Guid userId)
    {
        return await _dbContext.KnowledgeBases
            .Where(kb => kb.UserId == userId || kb.IsPublic == true)
            .OrderBy(kb => kb.Name)
            .ToListAsync();
    }

    public async Task<KnowledgeBase?> GetKnowledgeBaseByIdAsync(Guid kbId)
    {
        return await _dbContext.KnowledgeBases.FindAsync(kbId);
    }

    // ===== Загрузка документов =====

    // ===== УМНАЯ ЗАГРУЗКА ДОКУМЕНТА (с SHA256 и аудитом) =====

    public async Task<Document> LoadDocumentFromFileAsync(Guid kbId, string sourceFilePath)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Файл не найден: {sourceFilePath}");

        var originalFileName = Path.GetFileName(sourceFilePath);
        var fileContent = await File.ReadAllTextAsync(sourceFilePath);
        var fileSize = new FileInfo(sourceFilePath).Length;

        // 🔥 НОВАЯ ПРОВЕРКА: Запрещаем загрузку пустых файлов или файлов без текста
        if (fileSize == 0 || string.IsNullOrWhiteSpace(fileContent))
        {
            throw new InvalidOperationException("EMPTY_FILE");
        }

        var contentHash = ComputeSha256Hash(fileContent);

        // 1. Проверяем: есть ли в этой БЗ документ с таким именем?
        var existingDocument = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.KbId == kbId && d.FileName == originalFileName);

        if (existingDocument != null)
        {
            // Файл с таким именем уже существует в БЗ
            if (existingDocument.SourcePath == sourceFilePath && existingDocument.ContentHash == contentHash)
            {
                // Путь и хэш совпадают → файл не изменился
                throw new InvalidOperationException($"ALREADY_EXISTS:{originalFileName}");
            }
            else if (existingDocument.SourcePath != sourceFilePath)
            {
                // Путь отличается → запрещаем загрузку
                throw new InvalidOperationException($"NAME_COLLISION:{originalFileName}:{existingDocument.SourcePath}");
            }
            // else: путь совпадает, но хэш отличается → файл изменился, нужно перезаписать
            // (это решение принимает UI, передавая флаг forceOverwrite)
        }

        // 2. Если дошли сюда — создаём или перезаписываем документ
        return await SaveDocumentAsync(kbId, originalFileName, sourceFilePath, fileContent, fileSize, contentHash, existingDocument);
    }

    // Перегрузка с флагом принудительной перезаписи (для случая, когда файл изменился)
    public async Task<Document> LoadDocumentFromFileAsync(Guid kbId, string sourceFilePath, bool forceOverwrite)
    {
        if (!File.Exists(sourceFilePath))
            throw new FileNotFoundException($"Файл не найден: {sourceFilePath}");

        var originalFileName = Path.GetFileName(sourceFilePath);
        var fileContent = await File.ReadAllTextAsync(sourceFilePath);
        var fileSize = new FileInfo(sourceFilePath).Length;


        // 🔥 НОВАЯ ПРОВЕРКА: Запрещаем загрузку пустых файлов или файлов без текста
        if (fileSize == 0 || string.IsNullOrWhiteSpace(fileContent))
        {
            throw new InvalidOperationException("EMPTY_FILE");
        }

        var contentHash = ComputeSha256Hash(fileContent);

        var existingDocument = await _dbContext.Documents
            .FirstOrDefaultAsync(d => d.KbId == kbId && d.FileName == originalFileName);

        if (existingDocument != null && !forceOverwrite)
        {
            // Если файл существует и мы не хотим перезаписывать — проверяем
            if (existingDocument.SourcePath == sourceFilePath && existingDocument.ContentHash == contentHash)
            {
                throw new InvalidOperationException($"ALREADY_EXISTS:{originalFileName}");
            }
            else if (existingDocument.SourcePath != sourceFilePath)
            {
                throw new InvalidOperationException($"NAME_COLLISION:{originalFileName}:{existingDocument.SourcePath}");
            }
        }

        return await SaveDocumentAsync(kbId, originalFileName, sourceFilePath, fileContent, fileSize, contentHash, existingDocument);
    }
    // Вспомогательный метод: сохранение документа (создание или перезапись)
    private async Task<Document> SaveDocumentAsync(
        Guid kbId,
        string originalFileName,
        string sourceFilePath,
        string fileContent,
        long fileSize,
        string contentHash,
        Document? existingDocument)
    {
        // Если документ уже существует — удаляем его (вместе с чанками благодаря CASCADE)
        if (existingDocument != null)
        {
            // Удаляем физический старый файл
            var oldStoragePath = Path.Combine(AppContext.BaseDirectory, "Storage", "Documents", kbId.ToString(), $"{existingDocument.Id}_{originalFileName}");
            if (File.Exists(oldStoragePath))
            {
                File.Delete(oldStoragePath);
            }

            _dbContext.Documents.Remove(existingDocument);
            await _dbContext.SaveChangesAsync();
        }

        // Создаём новый документ
        var newDocumentId = Guid.NewGuid();
        var safeFileName = $"{newDocumentId}_{originalFileName}";

        var storageDir = Path.Combine(AppContext.BaseDirectory, "Storage", "Documents", kbId.ToString());
        Directory.CreateDirectory(storageDir);
        var destinationPath = Path.Combine(storageDir, safeFileName);

        File.Copy(sourceFilePath, destinationPath, overwrite: true);

        var document = new Document
        {
            Id = newDocumentId,
            KbId = kbId,
            FileName = originalFileName,
            FileContent = fileContent,
            ContentHash = contentHash,
            SourcePath = sourceFilePath,
            FileSize = fileSize
        };

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();

        // Разбиваем на чанки и векторизуем
        await ChunkAndEmbedDocumentAsync(document.Id, fileContent, originalFileName);

        return document;
    }

    public async Task<List<Document>> GetDocumentsByKbIdAsync(Guid kbId)
    {
        return await _dbContext.Documents
            .Where(d => d.KbId == kbId)
            .OrderBy(d => d.UploadedAt)
            .ToListAsync();
    }

    // Вычисление SHA256 хэша строки
    private static string ComputeSha256Hash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var builder = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    // ===== Чанкинг и векторизация (самое важное!) =====

    private async Task ChunkAndEmbedDocumentAsync(Guid documentId, string markdownContent, string fileName)
    {
        var pipeline = new MarkdownPipelineBuilder().Build();
        var markdownDoc = Markdown.Parse(markdownContent, pipeline);

        var chunks = new List<(string text, string breadcrumb)>();
        var currentBreadcrumb = fileName;
        var currentChunkText = "";
        var chunkIndex = 0;

        foreach (var block in markdownDoc)
        {
            if (block is HeadingBlock heading)
            {
                // Сохраняем предыдущий чанк
                if (!string.IsNullOrWhiteSpace(currentChunkText))
                {
                    chunks.Add((currentChunkText.Trim(), currentBreadcrumb));
                    currentChunkText = "";
                }

                // 🚀 ПУЛЕНЕПРОБИВАЕМОЕ ИЗВЛЕЧЕНИЕ ЗАГОЛОВКА через Span
                var headingText = markdownContent.Substring(heading.Span.Start, heading.Span.Length)
                                                   .Replace("#", "")
                                                   .Trim();
                currentBreadcrumb = $"{fileName} > {headingText}";
            }
            else if (block is LeafBlock leafBlock) // LeafBlock - это базовый класс для ParagraphBlock, FencedCodeBlock, CodeBlock и т.д.
            {
                // 🚀 ПУЛЕНЕПРОБИВАЕМОЕ ИЗВЛЕЧЕНИЕ ТЕКСТА через Span
                var blockText = markdownContent.Substring(leafBlock.Span.Start, leafBlock.Span.Length).Trim();

                if (!string.IsNullOrWhiteSpace(blockText))
                {
                    currentChunkText += blockText + "\n\n";
                }
            }

            // Жёсткий лимит: если чанк стал слишком большим, разбиваем его
            if (currentChunkText.Length > 2500)
            {
                chunks.Add((currentChunkText.Trim(), currentBreadcrumb));
                currentChunkText = "";
            }
        }

        // Сохраняем последний чанк
        if (!string.IsNullOrWhiteSpace(currentChunkText))
        {
            chunks.Add((currentChunkText.Trim(), currentBreadcrumb));
        }

        // Векторизуем и сохраняем чанки с красивым прогресс-баром
        await AnsiConsole.Progress()
            .AutoClear(false)
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new ElapsedTimeColumn(),
                new SpinnerColumn()
            })
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[cyan]Векторизация чанков[/]", maxValue: chunks.Count);

                foreach (var (text, breadcrumb) in chunks)
                {
                    var embedding = await _ollamaService.GetEmbeddingAsync(text, isQuery: false);

                    if (embedding != null)
                    {
                        var chunk = new DocumentChunk
                        {
                            DocumentId = documentId,
                            ChunkIndex = chunkIndex++,
                            BreadcrumbPath = breadcrumb,
                            ChunkText = text,
                            Embedding = embedding
                        };

                        _dbContext.DocumentChunks.Add(chunk);
                    }

                    task.Increment(1);
                }
            });

        await _dbContext.SaveChangesAsync();
    }

    // ===== Поиск по базам знаний (для RAG) =====

    public async Task<List<DocumentChunk>> SearchRelevantChunksAsync(List<Guid> kbIds, string query, int limit = 3)
    {
        if (kbIds.Count == 0)
            return new List<DocumentChunk>();

        var queryEmbedding = await _ollamaService.GetEmbeddingAsync(query, isQuery: true);
        if (queryEmbedding == null)
            return new List<DocumentChunk>();

        // Ищем ближайшие чанки из подключенных БЗ
        var relevantChunks = await _dbContext.DocumentChunks
            .Where(c => kbIds.Contains(c.Document.KbId))
            .OrderBy(c => c.Embedding.CosineDistance(queryEmbedding))
            .Take(limit)
            .Select(c => new DocumentChunk
            {
                Id = c.Id,
                DocumentId = c.DocumentId,
                ChunkIndex = c.ChunkIndex,
                BreadcrumbPath = c.BreadcrumbPath,
                ChunkText = c.ChunkText
            })
            .ToListAsync();

        return relevantChunks;
    }

    // ===== Подключение БЗ к чату (через навигационные свойства M:N) =====

    public async Task AttachKbToSessionAsync(Guid sessionId, Guid kbId)
    {
        var session = await _dbContext.ChatSessions
            .Include(s => s.Kbs)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        var kb = await _dbContext.KnowledgeBases.FindAsync(kbId);

        if (session != null && kb != null && !session.Kbs.Any(k => k.Id == kbId))
        {
            session.Kbs.Add(kb);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task DetachKbFromSessionAsync(Guid sessionId, Guid kbId)
    {
        var session = await _dbContext.ChatSessions
            .Include(s => s.Kbs)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session != null)
        {
            var kbToRemove = session.Kbs.FirstOrDefault(k => k.Id == kbId);
            if (kbToRemove != null)
            {
                session.Kbs.Remove(kbToRemove);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<List<KnowledgeBase>> GetAttachedKbsForSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.ChatSessions
            .Include(s => s.Kbs)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        return session?.Kbs.ToList() ?? new List<KnowledgeBase>();
    }

    public async Task<int> GetChunkCountAsync(Guid documentId)
    {
        return await _dbContext.DocumentChunks.CountAsync(c => c.DocumentId == documentId);
    }

    public async Task DeleteKnowledgeBaseAsync(Guid kbId, Guid userId)
    {
        // 1. Проверяем, что база существует и принадлежит этому пользователю (безопасность)
        var kb = await _dbContext.KnowledgeBases
            .FirstOrDefaultAsync(k => k.Id == kbId && k.UserId == userId);

        if (kb == null)
            throw new InvalidOperationException("База знаний не найдена или у вас нет прав на её удаление.");

        // 2. Удаляем физические файлы с диска (папку со всеми документами этой БЗ)
        var storageDir = Path.Combine(AppContext.BaseDirectory, "Storage", "Documents", kbId.ToString());
        if (Directory.Exists(storageDir))
        {
            Directory.Delete(storageDir, recursive: true);
        }

        // 3. Удаляем запись из БД. 
        // Благодаря ON DELETE CASCADE в БД, документы и их чанки удалятся автоматически!
        _dbContext.KnowledgeBases.Remove(kb);
        await _dbContext.SaveChangesAsync();
    }
}