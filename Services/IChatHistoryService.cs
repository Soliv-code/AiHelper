using AiHelper.Models;
using Pgvector;

namespace AiHelper.Services;

public interface IChatHistoryService
{
    Task<ChatSession> CreateNewSessionAsync(Guid userId, string modelName);
    Task SaveMessageAsync(Guid sessionId, string role, string content);
    Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId);
    // ДОБАВЛЕНО: modelName для фильтрации истории
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId, string modelName);
    // ДОБАВЛЕНО: modelName для фильтрации контекста
    Task<List<ChatMessage>> SearchRelevantContextAsync(Guid userId, string modelName, Vector queryVector, int limit = 3);
}
