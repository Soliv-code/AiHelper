using AiHelper.Models;
using AiHelper.Services;
using AiHelper.UI;
using Spectre.Console;

namespace AiHelper.Components;

public class KnowledgeBasePage(IAppContext appContext, User currentUser) : IComponent
{
    private readonly IDocumentRagService _documentRagService = appContext.GetService<IDocumentRagService>();

    public async Task RunAsync()
    {
        while (true)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Knowledge Base").Color(Color.DeepPink3_1));
            AnsiConsole.MarkupLine($"[dim]Пользователь: [cyan]{currentUser.Username}[/][/]\n");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan bold]📚 Управление базами знаний[/]")
                    .PageSize(10)
                    .AddChoices(
                        "⬅️ Назад в главное меню",
                        "✨ Создать новую базу знаний",
                        "📂 Просмотреть мои базы знаний",
                        "🌍 Просмотреть публичные базы знаний"
                    )
            );

            switch (choice)
            {
                case "⬅️ Назад в главное меню":
                    return;

                case "✨ Создать новую базу знаний":
                    await CreateKnowledgeBaseAsync();
                    break;

                case "📂 Просмотреть мои базы знаний":
                    await ViewKnowledgeBasesAsync(isPublic: false);
                    break;

                case "🌍 Просмотреть публичные базы знаний":
                    await ViewKnowledgeBasesAsync(isPublic: true);
                    break;
            }
        }
    }
    private async Task CreateKnowledgeBaseAsync()
    {
        string name;

        // Цикл защиты от ввода имени файла
        while (true)
        {
            name = AnsiConsole.Ask<string>(
                "[cyan bold]Введите название базы знаний (коллекции).[/]\n" +
                "[dim]Примеры: 'C# Стандарты', 'Документация HR', 'Проект Alpha'[/]\n" +
                "[yellow bold]Внимание: это не имя файла, а название папки для документов![/]\n" +
                "[cyan bold]Название:[/]"
            );

            if (name.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                ConsoleUi.ShowError($"⚠️ '{name}' похоже на имя файла.\nБаза знаний — это коллекция (папка). Назовите её обобщённо, например: 'Мои документы'.");
                Console.ReadKey();
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ConsoleUi.ShowError("❌ Название не может быть пустым.");
                Console.ReadKey();
                continue;
            }

            break;
        }

        var description = AnsiConsole.Ask<string>("[dim]Описание (необязательно, Enter для пропуска):[/]", defaultValue: "");
        var isPublic = AnsiConsole.Confirm("[cyan]Сделать базу публичной?[/]", defaultValue: false);

        var kb = await _documentRagService.CreateKnowledgeBaseAsync(currentUser.Id, name, string.IsNullOrWhiteSpace(description) ? null : description, isPublic);

        ConsoleUi.ShowInfo($"✅ База знаний [cyan]{kb.Name}[/] создана!");

        // 🔥 НОВОЕ: Сразу спрашиваем, хочет ли пользователь загрузить первый документ
        var loadFirstDoc = AnsiConsole.Confirm("[cyan]Хотите сразу загрузить первый документ в эту базу?[/]", defaultValue: true);

        if (loadFirstDoc)
        {
            await LoadDocumentAsync(kb); // Вызываем метод загрузки
        }
        else
        {
            ConsoleUi.ShowInfo($"💡 Вы можете загрузить документы позже через меню 'Просмотреть мои базы знаний'.");
            Console.ReadKey();
        }
    }
    private async Task ViewKnowledgeBasesAsync(bool isPublic)
    {
        var kbs = await _documentRagService.GetUserKnowledgeBasesAsync(currentUser.Id);

        // Фильтруем по типу (мои или публичные)
        var filteredKbs = isPublic
            ? kbs.Where(kb => kb.IsPublic == true && kb.UserId != currentUser.Id).ToList()
            : kbs.Where(kb => kb.UserId == currentUser.Id).ToList();

        if (filteredKbs.Count == 0)
        {
            ConsoleUi.ShowInfo(isPublic ? "🌍 Публичных баз знаний пока нет." : "📂 У вас пока нет баз знаний.");
            Console.ReadKey();
            return;
        }

        // 🔥 ИСПРАВЛЕНИЕ: Заменили [публичная] на 🌍 (Публичная), чтобы не ломать парсер Spectre
        var selectedKbName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title(isPublic ? "[cyan bold]🌍 Публичные базы знаний[/]" : "[cyan bold]📂 Ваши базы знаний[/]")
                .PageSize(10)
                .AddChoices(filteredKbs.Select(kb => $"{kb.Name} {(kb.IsPublic == true ? "🌍 (Публичная)" : "")}"))
        );

        var selectedKb = filteredKbs.First(kb => selectedKbName.StartsWith(kb.Name));

        await ManageKnowledgeBaseAsync(selectedKb);
    }
    private async Task ManageKnowledgeBaseAsync(KnowledgeBase kb)
    {
        while (true)
        {
            Console.Clear();
            AnsiConsole.MarkupLine($"[cyan bold]📚 База знаний: {kb.Name}[/]");
            AnsiConsole.MarkupLine($"[dim]{kb.Description ?? "Без описания"}[/]");
            AnsiConsole.MarkupLine($"[dim]ID: {kb.Id} | Публичная: {(kb.IsPublic == true ? "Да" : "Нет")}[/]\n");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan bold]🔧 Действия[/]")
                    .PageSize(10)
                    .AddChoices(
                        "⬅️ Назад к списку баз",
                        "📄 Загрузить документ",
                        "📋 Просмотреть документы",
                        "🗑️ Удалить базу знаний"
                    )
            );

            switch (choice)
            {
                case "⬅️ Назад к списку баз":
                    return;

                case "📄 Загрузить документ":
                    await LoadDocumentAsync(kb);
                    break;

                case "📋 Просмотреть документы":
                    await ViewDocumentsAsync(kb);
                    break;

                case "🗑️ Удалить базу знаний":
                    var confirm = AnsiConsole.Confirm(
                        $"[red bold]Вы уверены, что хотите удалить базу '[cyan]{kb.Name}[/]' и ВСЕ её документы?[/]\n" +
                        $"[yellow]⚠️ Это действие необратимо! Все чанки и векторы будут уничтожены.[/]"
                    );

                    if (confirm)
                    {
                        try
                        {
                            ConsoleUi.ShowInfo("⏳ Удаляю базу знаний и очищаю хранилище...");
                            await _documentRagService.DeleteKnowledgeBaseAsync(kb.Id, currentUser.Id);

                            AnsiConsole.MarkupLine($"\n[green bold]✅ База знаний [cyan]{kb.Name}[/] успешно удалена![/]");
                            AnsiConsole.MarkupLine("[dim]Нажмите любую клавишу для возврата в список...[/]");
                            Console.ReadKey();

                            return; // Выходим из метода управления, так как база удалена
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"\n[red bold]❌ Ошибка при удалении:[/] {ex.Message}");
                            AnsiConsole.MarkupLine("[dim]Нажмите любую клавишу для продолжения...[/]");
                            Console.ReadKey();
                        }
                    }
                    break;
            }
        }
    }
    private async Task LoadDocumentAsync(KnowledgeBase kb)
    {
        string filePath;

        // 1. ЦИКЛ: Даём пользователю вводить путь, пока он не укажет существующий файл или не отменит
        while (true)
        {
            var rawPath = AnsiConsole.Ask<string>(
                "[cyan bold]Введите путь к файлу:[/]\n" +
                "[dim]💡 Лайфхак: просто перетащи файл мышкой в это окно консоли![/]\n" +
                "[dim](Оставь пустым и нажми Enter, чтобы отменить)[/]\n" +
                "[cyan bold]Путь:[/] "
            );

            // 2. Агрессивная очистка пути от кавычек, пробелов и мусора, который добавляет терминал
            filePath = rawPath.Trim().Trim('"', '\'', ' ');

            // Если пользователь нажал Enter без ввода — отменяем операцию
            if (string.IsNullOrWhiteSpace(filePath))
            {
                ConsoleUi.ShowInfo("⏪ Загрузка отменена.");
                return; // Выходим из метода обратно в меню
            }

            // 3. ПРОВЕРКА: Если файла нет, ругаемся ЧЕСТНО и просим ввести заново (continue)
            if (!File.Exists(filePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n❌ Файл не найден: {filePath}");
                Console.WriteLine("   Проверь путь или просто перетащи файл мышкой заново.\n");
                Console.ResetColor();

                // НЕ делаем Console.ReadKey()! Просто продолжаем цикл и спрашиваем снова.
                continue;
            }

            // Если файл найден, прерываем цикл и идем к загрузке
            break;
        }

        // 4. ПРОЦЕСС ЗАГРУЗКИ (мы сюда попадем только с гарантированно существующим файлом)
        var fileName = Path.GetFileName(filePath);
        ConsoleUi.ShowInfo($"⏳ Обрабатываю документ: [cyan]{fileName}[/]...");

        try
        {
            var document = await _documentRagService.LoadDocumentFromFileAsync(kb.Id, filePath);
            var chunkCount = await _documentRagService.GetChunkCountAsync(document.Id);

            ConsoleUi.ShowInfo($"✅ Документ [cyan]{fileName}[/] успешно ЗАГРУЖЕН!\n   Создано чанков: [cyan]{chunkCount}[/]");
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMPTY_FILE")
        {
            // Прямой вывод без глобального ConsoleUi.ShowError, чтобы не было спама про Ollama
            AnsiConsole.MarkupLine($"\n[red bold]❌ Файл [cyan]{fileName}[/] пустой или не содержит текста.[/]");
            AnsiConsole.MarkupLine("[dim]   Пожалуйста, выберите файл с реальным содержимым.[/]\n");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("ALREADY_EXISTS:"))
        {
            AnsiConsole.MarkupLine($"\n[green bold]✅ Файл [cyan]{fileName}[/] уже загружен и не изменился. Пропускаем.[/]\n");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("NAME_COLLISION:"))
        {
            var parts = ex.Message.Split(':');
            var existingPath = parts.Length > 2 ? parts[2] : "неизвестный путь";
            AnsiConsole.MarkupLine($"\n[red bold]❌ Файл с именем [cyan]{fileName}[/] уже загружен из:[/]");
            AnsiConsole.MarkupLine($"[yellow]   {existingPath}[/]");
            AnsiConsole.MarkupLine("[dim]   Переименуйте файл или используйте другую Базу Знаний.[/]\n");
        }
        catch (Exception ex)
        {
            // Вот здесь спам про Ollama ОСТАЁТСЯ, потому что если мы дошли сюда, 
            // значит файл прочитался, и ошибка случилась именно при обращении к Ollama (векторизация)!
            AnsiConsole.MarkupLine($"\n[red bold]❌ Ошибка при обработке файла:[/] {ex.Message}");
            AnsiConsole.MarkupLine("[dim]💡 Убедитесь, что Ollama запущена и модель bge-m3 доступна.[/]\n");
        }

        AnsiConsole.MarkupLine("[dim]Нажмите любую клавишу для продолжения...[/]");
        Console.ReadKey();
    }
    private async Task ViewDocumentsAsync(KnowledgeBase kb)
    {
        var documents = await _documentRagService.GetDocumentsByKbIdAsync(kb.Id);

        if (documents.Count == 0)
        {
            ConsoleUi.ShowInfo("📄 В этой базе пока нет документов.");
            Console.ReadKey();
            return;
        }

        var table = new Table();
        table.AddColumn("Имя файла");
        table.AddColumn("Дата загрузки");
        table.AddColumn("ID");

        foreach (var doc in documents)
        {
            table.AddRow(doc.FileName, doc.UploadedAt?.ToString("dd.MM.yyyy HH:mm") ?? "N/A", doc.Id.ToString());
        }

        AnsiConsole.Write(table);
        Console.ReadKey();
    }
}