using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Добавлено для работы с коллекциями (First)
using System.Threading.Tasks;
using AiHelper.Data;
using AiHelper.Models; // Добавлено для класса User
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

            User currentUser;
            if (userChoice == "🆕 Создать нового пользователя")
            {
                var newUsername = ConsoleUi.AskForNewUsername();
                currentUser = await userService.CreateUserAsync(newUsername);
                ConsoleUi.ShowInfo($"✅ Пользователь [cyan]{currentUser.Username}[/] создан!");
            }
            else
            {
                currentUser = users.First(u => $"👤 {u.Username}" == userChoice);
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

            // 5. Создаем сессию, передавая ID текущего пользователя!
            var currentSession = await historyService.CreateNewSessionAsync(currentUser.Id, selectedModel);
            ConsoleUi.ShowInfo($"💾 Чат создан. ID: {currentSession.Id}");

            var memoryHistory = new List<(string role, string content)>();

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
        }
    }
}