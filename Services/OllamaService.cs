using OllamaSharp;

namespace AiHelper.Services;

public class OllamaService : IOllamaService
{
    private readonly OllamaApiClient _ollamaClient;
    public OllamaService(string apiUri = "http://127.0.0.1:11434")
    {
        _ollamaClient = new OllamaApiClient(apiUri);
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        var models = await _ollamaClient.ListLocalModelsAsync();
        // Тоже самое что и models.Select(m => m.Name).OrderBy(n => n).ToList();, т.е. сокращаем .ToList
        return [.. models.Select(m => m.Name).OrderBy(n => n)];
    }

    public async IAsyncEnumerable<string> StreamChatResponseAsync(
        string modelName, 
        string userMessage, 
        List<(string role, string content)> history
        )
    {
        // Установили модель для общения:
        _ollamaClient.SelectedModel = modelName;

        // Инициализируем экземпляр класса Chat
        var chat = new Chat(_ollamaClient);

        // Восстанавливаем историю 
        foreach (var (role, content) in history)
        {
            chat.Messages.Add(new OllamaSharp.Models.Chat.Message
            {
                Role = role,
                Content = content
            });
        }

        // Стримим ответ
        await foreach (var token in chat.SendAsync(userMessage))
        {
            yield return token;
        }

    }
}
