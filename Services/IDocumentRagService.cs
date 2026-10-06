using AiHelper.Models;

namespace AiHelper.Services;

public interface IDocumentRagService
{
    // Управление базами знаний
    Task<KnowledgeBase> CreateKnowledgeBaseAsync(Guid userId, string name, string? description = null, bool isPublic = false);
    Task<List<KnowledgeBase>> GetUserKnowledgeBasesAsync(Guid userId);
    Task<KnowledgeBase?> GetKnowledgeBaseByIdAsync(Guid kbId);

    // Загрузка документов
    Task<Document> LoadDocumentFromFileAsync(Guid kbId, string filePath);
    Task<List<Document>> GetDocumentsByKbIdAsync(Guid kbId);

    // Поиск по базам знаний (для RAG в чате)
    Task<List<DocumentChunk>> SearchRelevantChunksAsync(List<Guid> kbIds, string query, int limit = 3);

    // Подключение БЗ к чату
    Task AttachKbToSessionAsync(Guid sessionId, Guid kbId);
    Task DetachKbFromSessionAsync(Guid sessionId, Guid kbId);
    Task<int> GetChunkCountAsync(Guid documentId);
    Task<List<KnowledgeBase>> GetAttachedKbsForSessionAsync(Guid sessionId);
    Task DeleteKnowledgeBaseAsync(Guid kbId, Guid userId);
}
