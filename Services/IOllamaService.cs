using Pgvector;

namespace AiHelper.Services;
public interface IOllamaService
{
    Task<List<string>> GetAvailableModelsAsync();
    IAsyncEnumerable<string> StreamChatResponseAsync(string modelName, string userMessage, List<(string role, string content)> history);

    // ИСПРАВЛЕНО: добавлен параметр isQuery
    Task<Vector?> GetEmbeddingAsync(string text, bool isQuery = false);
}
