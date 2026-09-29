using AiHelper.Models;
using Pgvector;

namespace AiHelper.Services;

public interface IChatHistoryService
{
    Task<ChatSession> CreateNewSessionAsync(Guid userId, string modelName);
    Task SaveMessageAsync(Guid sessionId, string role, string content);
    Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId);
    Task<List<ChatSession>> GetUserSessionsAsync(Guid userId);
    Task<List<ChatMessage>> SearchRelevantContextAsync(Guid userId, Vector queryVector, int limit = 3);
}
