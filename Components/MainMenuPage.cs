using System;
using System.Threading.Tasks;
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
        while (true)
        {
            Console.Clear();
            ConsoleUi.ShowWelcome();

            // Показываем текущее состояние
            ShowStatus();

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan bold]🚀 Главное меню AiHelper[/]")
                    .PageSize(10)
                    .AddChoices(
                        "💬 Начать чат",
                        "👤 Сменить пользователя",
                        "🤖 Сменить модель",
                        "🚪 Выход из приложения"
                    )
            );

            switch (choice)
            {
                case "🚪 Выход из приложения":
                    ConsoleUi.ShowGoodbye();
                    await Task.Delay(1000);
                    return; // Выход из метода, приложение завершается

                case "💬 Начать чат":
                    if (_currentUser == null || _selectedModel == null)
                    {
                        AnsiConsole.MarkupLine("\n[red]❌ Сначала выберите пользователя и модель![/]");
                        await Task.Delay(1500);
                    }
                    else
                    {
                        // Активируем компонент чата, передавая ему пользователя и модель
                        var chatPage = new ChatPage(_appContext, _currentUser, _selectedModel);
                        await chatPage.RunAsync();
                    }
                    break;

                case "👤 Сменить пользователя":
                    await SelectUserAsync();
                    break;

                case "🤖 Сменить модель":
                    await SelectModelAsync();
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

    private async Task SelectUserAsync()
    {
        var userService = _appContext.GetService<UserService>();
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
    }

    private async Task SelectModelAsync()
    {
        var ollamaService = _appContext.GetService<OllamaService>();
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
    }
}