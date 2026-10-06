using AiHelper.Models;
using AiHelper.Services;
using AiHelper.UI;
using Spectre.Console;

namespace AiHelper.Components;

public class MainMenuPage : IComponent
{
    private readonly IAppContext _appContext;

    // Состояние главного меню
    private User? _currentUser;
    private string? _selectedModel;

    public MainMenuPage(IAppContext appContext)
    {
        _appContext = appContext;
    }

    public async Task RunAsync()
    {
        var userService = _appContext.GetService<UserService>();

        // 🔄 1. ВОССТАНОВЛЕНИЕ СОСТОЯНИЯ ПРИ ЗАПУСКЕ
        var lastUsername = await userService.GetLastUsernameAsync();
        if (!string.IsNullOrEmpty(lastUsername))
        {
            var users = await userService.GetAllUsersAsync();
            _currentUser = users.FirstOrDefault(u => u.Username == lastUsername);

            if (_currentUser != null)
            {
                _selectedModel = await userService.GetLastModelAsync(_currentUser.Id);
            }
        }

        // 🔄 2. ГЛАВНЫЙ ЦИКЛ МЕНЮ
        while (true)
        {
            Console.Clear();
            ConsoleUi.ShowWelcome();
            ShowStatus();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan bold]🚀 Главное меню AiHelper[/]")
                    .PageSize(10)
                    .AddChoices(
                        "💬 Начать чат",
                        "📚 Базы знаний", // <-- ДОБАВИТЬ
                        "👤 Сменить пользователя",
                        "🤖 Сменить модель",
                        "🚪 Выход из приложения"
                    )
            );

            switch (choice)
            {
                case "📚 Базы знаний":
                    if (_currentUser == null)
                    {
                        AnsiConsole.MarkupLine("\n[red]❌ Сначала выберите пользователя![/]");
                        await Task.Delay(1500);
                    }
                    else
                    {
                        var kbPage = new KnowledgeBasePage(_appContext, _currentUser);
                        await kbPage.RunAsync();
                    }
                    break;

                case "🚪 Выход из приложения":
                    ConsoleUi.ShowGoodbye();
                    await Task.Delay(1000);
                    return; // Выход из приложения

                case "💬 Начать чат":
                    if (_currentUser == null || _selectedModel == null)
                    {
                        AnsiConsole.MarkupLine("\n[red]❌ Сначала выберите пользователя и модель![/]");
                        await Task.Delay(1500);
                    }
                    else
                    {
                        var chatPage = new ChatPage(_appContext, _currentUser, _selectedModel);
                        await chatPage.RunAsync();
                    }
                    break;

                case "👤 Сменить пользователя":
                    await SelectUserAsync(userService);
                    break;

                case "🤖 Сменить модель":
                    await SelectModelAsync(userService);
                    break;
            }
        }
    }

    private void ShowStatus()
    {
        var userInfo = _currentUser != null
            ? $"[green]{_currentUser.Username}[/]"
            : "[red]не выбран[/]";

        var modelInfo = _selectedModel != null
            ? $"[green]{_selectedModel}[/]"
            : "[red]не выбрана[/]";

        AnsiConsole.MarkupLine($"👤 Пользователь: {userInfo}");
        AnsiConsole.MarkupLine($"🤖 Модель: {modelInfo}");
        AnsiConsole.WriteLine();
    }

    private async Task SelectUserAsync(UserService userService)
    {
        var users = await userService.GetAllUsersAsync();
        var userChoice = ConsoleUi.SelectOrCreateUser(users);

        if (userChoice == null || userChoice == "⬅️ Назад")
        {
            return; // Отмена, не меняем состояние
        }

        if (userChoice == "🆕 Создать нового пользователя")
        {
            var newUsername = ConsoleUi.AskForNewUsername();
            _currentUser = await userService.CreateUserAsync(newUsername);
            ConsoleUi.ShowInfo($"✅ Пользователь [cyan]{_currentUser.Username}[/] создан!");
        }
        else
        {
            _currentUser = users.FirstOrDefault(u => $"👤 {u.Username}" == userChoice);
            if (_currentUser != null)
            {
                ConsoleUi.ShowInfo($"✅ Приветствуем, [cyan]{_currentUser.Username}[/]!");
            }
        }

        // 💾 СОХРАНЯЕМ СОСТОЯНИЕ В БД
        if (_currentUser != null)
        {
            await userService.SetLastUsernameAsync(_currentUser.Username);
            // Сбрасываем модель, так как у нового пользователя может быть другая предпочтительная модель
            _selectedModel = await userService.GetLastModelAsync(_currentUser.Id);
        }
    }

    private async Task SelectModelAsync(UserService userService)
    {
        if (_currentUser == null)
        {
            ConsoleUi.ShowError("Сначала выберите пользователя!");
            return;
        }

        var ollamaService = _appContext.GetService<IOllamaService>(); // <-- Используем интерфейс!
        var models = await ollamaService.GetAvailableModelsAsync();

        if (models.Count == 0)
        {
            ConsoleUi.ShowNoModels();
            return;
        }

        var modelChoice = ConsoleUi.SelectModel(models);

        if (modelChoice == null || modelChoice == "⬅️ Назад")
        {
            return; // Отмена, не меняем состояние
        }

        _selectedModel = modelChoice;
        ConsoleUi.ShowModelSelected(_selectedModel);

        // 💾 СОХРАНЯЕМ СОСТОЯНИЕ В БД
        await userService.SaveLastModelAsync(_currentUser.Id, _selectedModel);
    }
}