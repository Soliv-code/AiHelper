using AiHelper.UI;
using Spectre.Console;

namespace AiHelper.Components;

public class MainMenuPage : IComponent
{
    private readonly IAppContext _appContext;

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

            // Пока заглушка статуса, на следующих шагах добавим сюда текущего пользователя и модель
            AnsiConsole.MarkupLine("[dim]Статус: Готов к работе[/]\n");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan bold]🚀 Главное меню AiHelper[/]")
                    .PageSize(5)
                    .AddChoices(
                        "💬 Начать чат",
                        "🚪 Выход из приложения"
                    )
            );

            if (choice == "🚪 Выход из приложения")
            {
                ConsoleUi.ShowGoodbye();
                await Task.Delay(1000);
                break; // Выходим из цикла, приложение завершается
            }

            if (choice == "💬 Начать чат")
            {
                // ВРЕМЕННАЯ ЗАГЛУШКА для проверки навигации
                AnsiConsole.MarkupLine("\n[yellow]⚠️ Логика чата будет перенесена сюда на Шаге 2.[/]");
                AnsiConsole.MarkupLine("[dim]Нажмите любую клавишу для возврата в меню...[/]");
                Console.ReadKey();
            }
        }
        // После выхода из цикла приложение корректно завершает работу
    }
}