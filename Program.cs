using AiHelper.Data;
using AiHelper.Models; // Добавлено для класса User и ChatSession
using AiHelper.Services;
using AiHelper.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AiHelper;

public class Program
{
    static async Task Main(string[] args)
    {
        // Устанавливаем кодировку UTF8 для отображения emoji в консоли
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        // 
        ConsoleUi.ShowWelcome();

        try
        {
            // 1. Читаем конфигурацию из appsettings.json
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // 2. Настраиваем DbContext
            var optionsBuilder = new DbContextOptionsBuilder<AiHelperDbContext>();
            // ДОБАВЛЕНО: o => o.UseVector() включает поддержку типа vector в EF Core
            optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());

            using var dbContext = new AiHelperDbContext(optionsBuilder.Options);

            // Инициализируем сервисы
            var userService = new UserService(dbContext);
            var ollamaService = new OllamaService();
            //var historyService = new ChatHistoryService(dbContext);
            var historyService = new ChatHistoryService(dbContext, ollamaService);

            // 3. Логика выбора или создания пользователя
            var users = await userService.GetAllUsersAsync();
            var userChoice = ConsoleUi.SelectOrCreateUser(users);

            User? currentUser = null;
            if (userChoice == "🆕 Создать нового пользователя")
            {
                var newUsername = ConsoleUi.AskForNewUsername();
                currentUser = await userService.CreateUserAsync(newUsername);

                // Используем !, так как мы только что создали/получили пользователя и знаем, что он не null
                ConsoleUi.ShowInfo($"✅ Пользователь [cyan]{currentUser!.Username}[/] создан!");
            }
            else
            {
                // FirstOrDefault безопаснее, чем First
                currentUser = users.FirstOrDefault(u => $"👤 {u.Username}" == userChoice);

                if (currentUser == null)
                {
                    ConsoleUi.ShowError("Пользователь не найден. Попробуйте запустить приложение заново.");
                    return;
                }

                ConsoleUi.ShowInfo($"✅ Приветствуем, [cyan]{currentUser.Username}[/]!");
            }

            // 4. Выбор модели
            var models = await ollamaService.GetAvailableModelsAsync();

            if (models.Count == 0)
            {
                ConsoleUi.ShowNoModels();
                return;
            }

            var selectedModel = ConsoleUi.SelectModel(models);
            ConsoleUi.ShowModelSelected(selectedModel);

            // 5. Получаем список чатов пользователя и даем выбор (Новая логика!)
            var userSessions = await historyService.GetUserSessionsAsync(currentUser.Id);
            var selectedSession = ConsoleUi.SelectChatAction(userSessions);

            ChatSession currentSession;
            var memoryHistory = new List<(string role, string content)>();

            if (selectedSession == null)
            {
                // Выбираем "Создать новый чат"
                currentSession = await historyService.CreateNewSessionAsync(currentUser.Id, selectedModel);
                ConsoleUi.ShowInfo($"💾 Новый чат создан. ID: {currentSession.Id}");
            }
            else
            {
                // Выбираем существующий чат
                var loadedSession = await historyService.GetSessionWithMessagesAsync(selectedSession.Id);

                // Явная проверка на null удовлетворяет компилятор
                if (loadedSession == null)
                {
                    ConsoleUi.ShowError("Не удалось загрузить выбранный чат. Возможно, он был удален.");
                    return;
                }

                // Теперь компилятор на 100% уверен, что currentSession не null!
                currentSession = loadedSession;

                // Загружаем историю из БД в память для контекста Ollama
                // (Если VS подчеркивает ChatMessages красным, просто замени на Messages)
                if (currentSession.ChatMessages != null)
                {
                    /*
                    memoryHistory = currentSession.ChatMessages
                        .OrderBy(m => m.CreatedAt) // На всякий случай явно сортируем по времени
                        .Select(m => (m.Role, m.Content))
                        .ToList();
                    */
                    // По новым стандартам упрощаем ".ToList();":
                    memoryHistory = [.. currentSession.ChatMessages
                        .OrderBy(m => m.CreatedAt) // На всякий случай явно сортируем по времени
                        .Select(m => (m.Role, m.Content))];
                }

                // Безопасно получаем заголовок
                var title = string.IsNullOrWhiteSpace(currentSession.Title) ? "Без названия" : currentSession.Title;
                ConsoleUi.ShowInfo($"💬 Загружен чат: [cyan]{title}[/] ({memoryHistory.Count} сообщений в истории)");
            }

            // 6. Основной цикл чата
            while (true)
            {
                var userMessage = ConsoleUi.GetUserInput();

                if (string.IsNullOrWhiteSpace(userMessage) || userMessage.Trim().ToLower() is "/exit" or "/quit" or "/e" or "/q")
                {
                    ConsoleUi.ShowGoodbye();
                    await Task.Delay(2000); // 2 секунды достаточно, чтобы прочитать прощание
                    break;
                }

                // 1. СНАЧАЛА получаем вектор текущего сообщения
                var currentMessageVector = await ollamaService.GetEmbeddingAsync(userMessage);

                // 2. Ищем похожие вопросы ДО сохранения текущего сообщения в БД 
                // (чтобы текущий вопрос не нашел сам себя с расстоянием 0)
                if (currentMessageVector != null)
                {
                    var similarMessages = await historyService.SearchRelevantContextAsync(currentUser.Id, currentMessageVector, limit: 3);

                    if (similarMessages.Any())
                    {
                        // Показываем контекст пользователю в консоли
                        ConsoleUi.ShowContext(similarMessages);

                        // Формируем скрытый промпт для AI
                        var contextPrompt = "Контекст из прошлых чатов пользователя:\n";
                        foreach (var msg in similarMessages)
                        {
                            contextPrompt += $"- {msg.Content}\n";
                        }

                        // Добавляем в историю как системное сообщение
                        memoryHistory.Add(("system", contextPrompt));
                    }
                }

                // 3. ТЕПЕРЬ сохраняем сообщение пользователя в БД (СТРОГО ОДИН РАЗ!)
                await historyService.SaveMessageAsync(currentSession.Id, "user", userMessage);

                // И обязательно добавляем его в локальную историю для контекста текущего диалога
                memoryHistory.Add(("user", userMessage));

                // 4. Получаем ответ от AI
                ConsoleUi.StartAssistantResponse();
                var assistantResponse = string.Empty;

                await foreach (var token in ollamaService.StreamChatResponseAsync(selectedModel, userMessage, memoryHistory))
                {
                    Console.Write(token);
                    assistantResponse += token;
                }
                Console.WriteLine(); // Перенос строки после завершения стриминга

                // 5. Сохраняем ответ ассистента в БД (СТРОГО ОДИН РАЗ!)
                await historyService.SaveMessageAsync(currentSession.Id, "assistant", assistantResponse);

                // И добавляем его в локальную историю
                memoryHistory.Add(("assistant", assistantResponse));

                ConsoleUi.EndAssistantResponse();
            }
        }
        catch (Exception ex)
        {
            ConsoleUi.ShowError(ex.Message);
            Console.ReadLine();
        }
    }
}