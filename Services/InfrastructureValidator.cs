using AiHelper.Data;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;

// Fail-Fast Pattern: Rapid Error Detection for Robust ...
namespace AiHelper.Services;

public static class InfrastructureValidator
{
    public static async Task ValidateAsync(AiHelperDbContext dbContext)
    {
        // 1. Проверяем Ollama (быстрый HTTP-запрос)
        bool isOllamaOk = await CheckOllamaAsync();

        // 2. Проверяем БД (самый надежный способ для EF Core - попытка подключиться)
        bool isDbOk = await CheckDatabaseAsync(dbContext);

        // Если всё ОК, просто выходим из метода и продолжаем работу
        if (isOllamaOk && isDbOk)
        {
            return;
        }

        // 🛑 FAIL FAST: Если что-то не так, рисуем красивую ошибку и убиваем процесс
        PrintErrorPanel(isOllamaOk, isDbOk);

        AnsiConsole.MarkupLine("\n[yellow]Нажмите любую клавишу для выхода...[/]");
        Console.ReadKey();

        // Жёсткий выход. Весь остальной код в Program.cs НЕ будет выполнен.
        Environment.Exit(1);
    }
    private static async Task<bool> CheckOllamaAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            // Эндпоинт /api/tags возвращает 200 OK, если Ollama жива
            var response = await client.GetAsync("http://localhost:11434/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    private static async Task<bool> CheckDatabaseAsync(AiHelperDbContext dbContext)
    {
        try
        {
            // Пытаемся установить соединение. Это проверяет и Docker, и пароль, и порт.
            return await dbContext.Database.CanConnectAsync();
        }
        catch
        {
            return false;
        }
    }
    private static void PrintErrorPanel(bool isOllamaOk, bool isDbOk)
    {
        // Формируем статусы с цветными индикаторами
        var ollamaStatus = isOllamaOk ? "[green]✅ Работает[/]" : "[red]❌ Не отвечает[/]";
        var dbStatus = isDbOk ? "[green]✅ Работает[/]" : "[red]❌ Недоступна[/]";

        var panel = new Panel(
            $"[red bold]!!! ОШИБКА: ИНФРАСТРУКТУРА НЕДОСТУПНА !!![/]\n\n" +
            $"[yellow]• Ollama (localhost:11434) :[/] {ollamaStatus}\n" +
            $"[yellow]• PostgreSQL (Docker)      :[/] {dbStatus}\n\n" +
            "[white bold]Возможные причины и решения:[/]\n" +
            "1. [cyan]Docker Desktop[/] не запущен или контейнер остановлен.\n" +
            "   → Выполни: [cyan]cd Docker && docker compose up -d[/]\n" +
            "2. [cyan]Ollama[/] не запущена.\n" +
            "   → Запусти её из системного трея (иконка 🦙) или через меню Пуск.\n" +
            "3. Порты [cyan]5432[/] или [cyan]11434[/] заняты другими приложениями."
        )
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red),
            Padding = new Padding(2, 1, 2, 1)
        };

        AnsiConsole.Write(panel);
    }
}
