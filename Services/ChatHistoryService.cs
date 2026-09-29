using AiHelper.Data;
using AiHelper.Models;
using Microsoft.EntityFrameworkCore;

namespace AiHelper.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly AiHelperDbContext _dbContext;

    public ChatHistoryService(AiHelperDbContext dbContext)
    {
        _dbContext = dbContext;
    }

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

    public async Task SaveMessageAsync(Guid sessionId, string role, string content)
    {
        var message = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = role,
            Content = content,
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

    public async Task<ChatSession?> GetSessionWithMessagesAsync(Guid sessionId)
    {
        return await _dbContext.ChatSessions
            .Include(s => s.ChatMessages.OrderBy(m => m.CreatedAt)) // <-- ИСПРАВЛЕНО: было m.Timestamp
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }
}