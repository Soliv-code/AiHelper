using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;
using System.Runtime.InteropServices;

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

    public async IAsyncEnumerable<string> StreamChatResponseAsync(string modelName, string userMessage, List<(string role, string content)> history)
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
    public async Task<Vector?> GetEmbeddingAsync(string text)
    {
        try
        {
            // Создаем запрос на генерацию эмбеддинга
            var request = new EmbedRequest
            {
                Model = "nomic-embed-text",
                // Оборачиваем текст в массив, так как Ollama API принимает коллекцию строк
                Input = [text]
            };

            // Вызываем метод EmbedAsync (убедись, что переменная клиента называется так же, как у тебя в классе: _ollamaApiClient или _ollamaClient)
            var response = await _ollamaClient.EmbedAsync(request);
            var floats = response.Embeddings?.FirstOrDefault();

            // Ollama возвращает список векторов (по одному на каждый входной текст). 
            // Берем первый (и единственный) вектор.
            return floats != null ? new Vector(floats) : null;
        }
        catch (Exception)
        {
            // Если модель не скачана или произошла ошибка сети, возвращаем null
            return null;
        }
    }
}
