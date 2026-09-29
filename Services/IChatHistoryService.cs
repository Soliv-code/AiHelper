using AiHelper.Models;

namespace AiHelper.Services;

public interface IChatHistoryService
{
    Task<ChatSession> CreateNewSessionAsync(Guid userId, string modelName);
    Task SaveMessageAsync(Guid sessionId, string role, string content);
    Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId);
}
