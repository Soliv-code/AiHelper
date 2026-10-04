using Microsoft.Extensions.Configuration;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;

namespace AiHelper.Services;

public class OllamaService : IOllamaService
{
    private readonly OllamaApiClient _ollamaClient;
    private readonly List<string> _excludedModels;
    private readonly string _embeddingModel; // <-- ДОБАВЛЕНО: поле для хранения имени модели эмбеддинга

    public OllamaService(IConfiguration configuration, string apiUri = "http://127.0.0.1:11434")
    {
        _ollamaClient = new OllamaApiClient(apiUri);

        _excludedModels = configuration
            .GetSection("Ollama:ExcludedModels")
            .Get<List<string>>() ?? new List<string>();

        // <-- ДОБАВЛЕНО: читаем модель эмбеддинга из конфига один раз при старте
        _embeddingModel = configuration["Ollama:EmbeddingModel"] ?? "bge-m3";
    }

    public async Task<List<string>> GetAvailableModelsAsync()
    {
        var models = await _ollamaClient.ListLocalModelsAsync();
        // Сокращаем .ToList через выражение коллекции
        return [.. models
            .Where(m => !_excludedModels.Contains(m.Name)) // Фильтр по моделям из конфига
            .Select(m => m.Name).OrderBy(n => n)
        ];
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
                Model = _embeddingModel, // <-- ИСПОЛЬЗУЕМ ПОЛЕ, а не хардкод!
                // Оборачиваем текст в массив, так как Ollama API принимает коллекцию строк
                Input = [text]
            };

            // Вызываем метод EmbedAsync
            var response = await _ollamaClient.EmbedAsync(request);
            var floats = response.Embeddings?.FirstOrDefault();

            // Ollama возвращает список векторов (по одному на каждый входной текст). 
            // Берем первый (и единственный) вектор.
            return floats != null ? new Vector(floats) : null;
        }
        catch (Exception ex)
        {
            // Логируем ошибку, чтобы видеть, если модель вдруг не скачана
            Console.WriteLine($"[Ошибка эмбеддинга] {ex.Message}");
            return null;
        }
    }
}