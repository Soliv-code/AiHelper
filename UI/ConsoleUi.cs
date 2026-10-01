using AiHelper.Models;
using Spectre.Console;

namespace AiHelper.UI;

public static class ConsoleUi
{
    // Чат: Выводим приветственное сообщение
    public static void ShowWelcome()
        => AnsiConsole.MarkupLine("[cyan bold]👋 Привет! Запускаем AiHelper...[/]\n");
    // Чат: Сообщение что нет моделей
    public static void ShowNoModels()
    {
        AnsiConsole.MarkupLine("[red]❌ Локальные модели не найдены.[/]");
        AnsiConsole.MarkupLine("[yellow]💡 Запустите в PowerShell: ollama pull qwen2.5-coder:latest[/]");
    }
    // Меню: Создание нового пользователя или выбор уже существующего
    public static string? SelectOrCreateUser(List<User> users)
    {
        var choices = new List<string>
    {
        "⬅️ Назад",
        "🆕 Создать нового пользователя"
    };
        choices.AddRange(users.Select(u => $"👤 {u.Username}"));

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green bold]👨‍💻 Кто вы?[/]")
                .PageSize(10)
                .AddChoices(choices)
        );
    }
    // Меню: Создание нового пользователя => Введите имя пользователя
    public static string AskForNewUsername()
    {
        return AnsiConsole.Ask<string>("[cyan bold]Введите имя нового пользователя:[/]");
    }
    // Меню: Выбора модели
    public static string? SelectModel(List<string> models)
    {
        var choices = new List<string> { "⬅️ Назад" };
        choices.AddRange(models);

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[green bold]🤖 Выберите модель для чата:[/]")
                .PageSize(10)
                .AddChoices(choices)
        );
    }
    // Меню: Создать новый чат или продолжить
    public static (string Action, ChatSession? Session) SelectChatAction(List<ChatSession> sessions)
    {
        var choicesMap = new Dictionary<string, ChatSession?>
        {
            ["✨ Создать новый чат"] = null,
            ["✏️ Переименовать чат"] = null,  // Специальный маркер
            ["🗑️ Удалить чат"] = null        // Специальный маркер
        };

        foreach (var session in sessions)
        {
            var title = string.IsNullOrWhiteSpace(session.Title) ? "Без названия" : session.Title;
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

        // Возвращаем действие и саму сессию (если она выбрана)
        if (selectedText == "✏️ Переименовать чат") return ("Rename", null);
        if (selectedText == "🗑️ Удалить чат") return ("Delete", null);
        if (selectedText == "✨ Создать новый чат") return ("CreateNew", null);

        return ("Select", choicesMap[selectedText]);
    }
    // Меню: Удаление чата
    public static ChatSession? SelectChatFromList(List<ChatSession> sessions, string title = "📂 Выберите чат:")
    {
        if (sessions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]⚠️ У вас пока нет чатов.[/]");
            return null;
        }

        var choicesMap = new Dictionary<string, ChatSession>();

        foreach (var session in sessions)
        {
            var sessionTitle = string.IsNullOrWhiteSpace(session.Title) ? "Без названия" : session.Title;
            var dateStr = session.UpdatedAt?.ToString("dd.MM HH:mm") ?? "Неизвестно";
            var displayText = $"💬 {sessionTitle} (Обновлен: {dateStr})";
            choicesMap[displayText] = session;
        }

        var selectedText = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
            .Title($"[green bold]{title}[/]")
            .PageSize(10)
            .AddChoices(choicesMap.Keys)
        );
        return choicesMap[selectedText];
    }
    // Чат: Сообщение о выбранной модели
    public static void ShowModelSelected(string modelName)
    {
        AnsiConsole.MarkupLine($"\n[green]✅ Модель [cyan]{modelName}[/] выбрана![/]");
        AnsiConsole.MarkupLine("[yellow]💬 Начинаем чат. Введите 'exit' или 'выход' для завершения.[/]\n");
    }
    // Чат: 👨‍💻 Вы:
    public static string GetUserInput()
        => AnsiConsole.Ask<string>("[cyan bold]👨‍💻 Вы:[/]");
    // Чат: 🤖 Ассистент:
    public static void StartAssistantResponse()
        => AnsiConsole.Markup("\n[green bold]🤖 AiHelper:[/] ");
    // Чат: Конец сообщения 🤖 ассистента (тупо переносим строку)
    public static void EndAssistantResponse()
        => Console.WriteLine("\n");
    // Чат: Сообщение об окончании работы
    public static void ShowGoodbye()
        => AnsiConsole.MarkupLine("\n[yellow]👋 Завершаем работу. До встречи![/]");
    // Чат: Обработчик ошибок 
    public static void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"\n[red bold]❌ Ошибка:[/] {Markup.Escape(message)}");
        AnsiConsole.MarkupLine("[yellow]💡 Убедитесь, что Ollama запущена и доступна по http://localhost:11434[/]");
    }
    // Чат: Информация
    public static void ShowInfo(string msg) => AnsiConsole.MarkupLine($"[blue]{msg}[/]");
    // Чат: Найдено похожих сообщений в чате
    public static void ShowContext(List<ChatMessage> messages)
    {
        // Заголовок
        AnsiConsole.MarkupLine($"[dim] Найдено [bold]{messages.Count}[/] похожих вопросов в твоей истории:[/]");

        // Список найденных вопросов
        foreach (var msg in messages)
        {
            // Обязательно используем Markup.Escape, чтобы скобки в тексте не ломали рендер
            AnsiConsole.MarkupLine($"[dim]  • {Markup.Escape(msg.Content)}[/]");
        }

        // Пустая строка для отступа
        AnsiConsole.WriteLine();
    }

}
