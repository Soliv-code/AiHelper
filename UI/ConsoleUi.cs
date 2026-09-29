using AiHelper.Models;
using Spectre.Console;

namespace AiHelper.UI;

public static class ConsoleUi
{
    public static string SelectOrCreateUser(List<User> users)
    {
        var choices = new List<string> { "🆕 Создать нового пользователя" };
        choices.AddRange(users.Select(u => $"👤 {u.Username}"));

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green bold]👨‍💻 Кто вы?[/]")
                .PageSize(10)
                .AddChoices(choices)
        );
    }

    public static string AskForNewUsername()
    {
        return AnsiConsole.Ask<string>("[cyan bold]Введите имя нового пользователя:[/]");
    }

    // Выводим приветственное сообщение:
    public static void ShowWelcome()
        =>  AnsiConsole.MarkupLine("[cyan bold]👋 Привет! Запускаем AiHelper...[/]\n");
    // Сообщение что нет моделей:
    public static void ShowNoModels()
    {
        AnsiConsole.MarkupLine("[red]❌ Локальные модели не найдены.[/]");
        AnsiConsole.MarkupLine("[yellow]💡 Запустите в PowerShell: ollama pull qwen2.5-coder:latest[/]");
    }
    // Меню выбора модели:
    public static string SelectModel(List<string> models)
    {
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green bold]🤖 Выберите модель для чата:[/]")
                .PageSize(10)
                .AddChoices(models)
        );
    }
    // Выбор модели:
    public static void ShowModelSelected(string modelName)
    {
        AnsiConsole.MarkupLine($"\n[green]✅ Модель [cyan]{modelName}[/] выбрана![/]");
        AnsiConsole.MarkupLine("[yellow]💬 Начинаем чат. Введите 'exit' или 'выход' для завершения.[/]\n");
    }
    // 👨‍💻 Вы:
    public static string GetUserInput()
        => AnsiConsole.Ask<string>("[cyan bold]👨‍💻 Вы:[/]");

    // 🤖 Ассистент:
    public static void StartAssistantResponse()
        => AnsiConsole.Markup("\n[green bold]🤖 AiHelper:[/] ");

    // Конец сообщения ассистента (тупо переносим строку):
    public static void EndAssistantResponse()
        => Console.WriteLine("\n");

    // Сообщение об окончании работы:
    public static void ShowGoodbye()
        => AnsiConsole.MarkupLine("\n[yellow]👋 Завершаем работу. До встречи![/]");

    // Ошибочки: 
    public static void ShowError(string message)
    {
        // Markup.Escape защищает текст от парсинга, если в message есть символы '[' или ']'
        AnsiConsole.MarkupLine($"\n[red bold]❌ Ошибка:[/] {Markup.Escape(message)}");
        AnsiConsole.MarkupLine("[yellow]💡 Убедитесь, что Ollama запущена и доступна по http://localhost:11434[/]");
    }

    public static void ShowInfo(string msg) => AnsiConsole.MarkupLine($"[blue]{msg}[/]");
    // 
    public static ChatSession? SelectChatAction(List<ChatSession> sessions)
    {
        var choicesMap = new Dictionary<string, ChatSession?>
        {
            ["✨ Создать новый чат"] = null
        };

        foreach (var session in sessions)
        {
            // Безопасно берем заголовок
            var title = string.IsNullOrWhiteSpace(session.Title) ? "Без названия" : session.Title;

            // Безопасно форматируем дату: если UpdatedAt не null, форматируем, иначе пишем "Неизвестно"
            var dateStr = session.UpdatedAt?.ToString("dd.MM HH:mm") ?? "Неизвестно";

            var displayText = $"💬 {title} (Обновлен: {dateStr})";
            choicesMap[displayText] = session;
        }

        var selectedText = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green bold]📂 Что будем делать?[/]")
                .PageSize(10)
                .AddChoices(choicesMap.Keys)
        );

        return choicesMap[selectedText];
    }
}
