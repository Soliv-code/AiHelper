using AiHelper.Data;
using AiHelper.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace AiHelper.Services;

public class ChatHistoryService(AiHelperDbContext _dbContext, IOllamaService _ollamaService) : IChatHistoryService
{
    //private readonly AiHelperDbContext _dbContext = dbContext;
    //private readonly IOllamaService _ollamaService = ollamaService;
    // Создаём новую сессию чата
    public async Task<ChatSession> CreateNewSessionAsync(Guid userId, string modelName)
    {
        var session = new ChatSession
        {
            UserId = userId,
            ModelName = modelName
        };

        _dbContext.ChatSessions.Add(session);
        await _dbContext.SaveChangesAsync();
        return session;
    }
    // Сохранение чата
    public async Task SaveMessageAsync(Guid sessionId, string role, string content)
    {
        // 1. Генерируем эмбеддинг для текста сообщения
        // ИЗМЕНЕНО: используем Vector? вместо float[]?
        Vector? embedding = null;

        if (role == "user") // Векторизуем пока только вопросы пользователя для экономии ресурсов
        {
            embedding = await _ollamaService.GetEmbeddingAsync(content);
        }

        var message = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = role,
            Content = content,
            Embedding = embedding,      // <--- СОХРАНЯЕМ ВЕКТОР В БД
            CreatedAt = DateTime.UtcNow // <-- ИСПРАВЛЕНО: было Timestamp
        };

        _dbContext.ChatMessages.Add(message);

        // Обновляем время последнего обновления чата и ставим заголовок
        var session = await _dbContext.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.UpdatedAt = DateTime.UtcNow;

            // Если это первое сообщение пользователя, делаем его заголовком чата
            if (role == "user" && string.IsNullOrEmpty(session.Title))
            {
                session.Title = content.Length > 30 ? content[..30] + "..." : content;
            }
        }

        await _dbContext.SaveChangesAsync();
    }
    // Ищем только пользовательские сообщения, у которых есть вектор, принадлежащие этому пользователю
    // И ВАЖНО: только из чатов, созданных для текущей модели!
    // Сортируем по косинусному расстоянию (чем меньше число, тем точнее совпадение по смыслу)
    public async Task<List<ChatMessage>> SearchRelevantContextAsync(Guid userId, string modelName, Vector queryVector, int limit = 3)
    {
        return await _dbContext.ChatMessages
            .Where(m => m.Role == "user"
                     && m.ChatSession.UserId == userId
                     && m.ChatSession.ModelName == modelName // <-- ВОТ ЭТО ГЛАВНОЕ ИЗМЕНЕНИЕ
                     && m.Embedding != null)
            .OrderBy(m => m.Embedding.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync();
    }
    // 
    public async Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId)
        => await _dbContext.ChatSessions
            .Include(s => s.ChatMessages.OrderBy(m => m.CreatedAt)) // <-- ИСПРАВЛЕНО: было m.Timestamp
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    // 
    public async Task<List<ChatSession>> GetUserSessionsAsync(Guid userId, string modelName)
        => await _dbContext.ChatSessions
            .Where(s => s.UserId == userId 
                     && s.ModelName == modelName)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync();
    // Переименовываем чат:
    public async Task RenameSessionAsync(Guid sessionId, string newTitle)
    {
        var session = await _dbContext.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            session.Title = string.IsNullOrWhiteSpace(newTitle) ? "Без названия" : newTitle;
            session.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }
    // Удаляем чат:
    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.ChatSessions.FindAsync(sessionId);
        if (session != null)
        {
            _dbContext.ChatSessions.Remove(session);
            await _dbContext.SaveChangesAsync();
        }
    }
}