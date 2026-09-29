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
            optionsBuilder.UseNpgsql(connectionString);

            using var dbContext = new AiHelperDbContext(optionsBuilder.Options);

            // Инициализируем сервисы
            var userService = new UserService(dbContext);
            var ollamaService = new OllamaService();
            var historyService = new ChatHistoryService(dbContext);

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

                    await Task.Delay(5000); // 5 секунд
                    break;
                }

                await historyService.SaveMessageAsync(currentSession.Id, "user", userMessage);
                memoryHistory.Add(("user", userMessage));

                ConsoleUi.StartAssistantResponse();

                var assistantResponse = string.Empty;

                await foreach (var token in ollamaService.StreamChatResponseAsync(selectedModel, userMessage, memoryHistory))
                {
                    Console.Write(token);
                    assistantResponse += token;
                }

                await historyService.SaveMessageAsync(currentSession.Id, "assistant", assistantResponse);
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