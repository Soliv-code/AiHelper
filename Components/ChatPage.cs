using AiHelper.Models;
using AiHelper.Services;
using AiHelper.UI;
using Spectre.Console;

namespace AiHelper.Components;

public class ChatPage : IComponent
{
    private readonly IAppContext _appContext;
    private readonly User _currentUser;
    private readonly string _selectedModel;

    public ChatPage(IAppContext appContext, User currentUser, string selectedModel)
    {
        _appContext = appContext;
        _currentUser = currentUser;
        _selectedModel = selectedModel;
    }

    public async Task RunAsync()
    {
        var historyService = _appContext.GetService<ChatHistoryService>();
        var ollamaService = _appContext.GetService<IOllamaService>();
        var documentRagService = _appContext.GetService<IDocumentRagService>(); // <-- ДОБАВИТЬ ЭТО


        // 1. Выбор или создание чата
        var userSessions = await historyService.GetUserSessionsAsync(_currentUser.Id, _selectedModel);
        var (chatAction, session) = ConsoleUi.SelectChatAction(userSessions);

        ChatSession currentSession;
        var memoryHistory = new List<(string role, string content)>();

        switch (chatAction)
        {
            case "Back":
                return; // Возврат в главное меню

            case "Delete":
                await HandleDeleteChatAsync(historyService, userSessions);
                return; // Возврат в главное меню

            case "Rename":
                await HandleRenameChatAsync(historyService, userSessions);
                return; // Возврат в главное меню

            case "CreateNew":
                currentSession = await historyService.CreateNewSessionAsync(_currentUser.Id, _selectedModel);
                ConsoleUi.ShowInfo($"💾 Новый чат создан. ID: {currentSession.Id}");
                break;

            default: // "Select"
                var loadedSession = await historyService.GetSessionWithMessagesAsync(session!.Id);
                if (loadedSession == null)
                {
                    ConsoleUi.ShowError("Не удалось загрузить выбранный чат. Возможно, он был удален.");
                    return;
                }
                currentSession = loadedSession;

                if (currentSession.ChatMessages != null)
                {
                    memoryHistory = [.. currentSession.ChatMessages
                .OrderBy(m => m.CreatedAt)
                .Select(m => (m.Role, m.Content))];
                }

                var title = string.IsNullOrWhiteSpace(currentSession.Title) ? "Без названия" : currentSession.Title;
                ConsoleUi.ShowInfo($"💬 Загружен чат: [cyan]{title}[/] ({memoryHistory.Count} сообщений в истории)");
                break;
        }

        // ==========================================
        // 📜 ШПАРГАЛКА ПО КОМАНДАМ (строго для чата)
        // ==========================================
        AnsiConsole.MarkupLine("\n[cyan bold]💡 Доступные команды в этом чате:[/]");
        AnsiConsole.MarkupLine("  [yellow]/list_kbs[/]                   - Показать доступные базы знаний");
        AnsiConsole.MarkupLine("  [yellow]/attach_kb \"Имя\"[/]          - Подключить базу знаний к этому чату");
        AnsiConsole.MarkupLine("  [yellow]/detach_kb \"Имя\"[/]          - Отключить базу знаний");
        AnsiConsole.MarkupLine("  [yellow]/help[/] или [yellow]/?[/]     - Показать эту подсказку");
        AnsiConsole.MarkupLine("  [yellow]/exit[/], [yellow]/quit[/], [yellow]/e[/], [yellow]/q[/]  - Выйти в главное меню\n");
        // ==========================================

        // 2. Основной цикл чата
        while (true)
        {
            var userMessage = ConsoleUi.GetUserInput();

            if (string.IsNullOrWhiteSpace(userMessage))
                continue;

            // Обработка inline-команд
            if (userMessage.Trim().ToLower() is "/exit" or "/quit" or "/e" or "/q")
            {
                ConsoleUi.ShowGoodbye();
                await Task.Delay(1500);
                break; // Возврат в главное меню
            }

            if (userMessage.Trim().ToLower() == "/delete")
            {
                var confirm = AnsiConsole.Confirm($"[red]Удалить текущий чат?[/]");
                if (confirm)
                {
                    await historyService.DeleteSessionAsync(currentSession.Id);
                    ConsoleUi.ShowInfo($"🗑️ Чат удален.");
                    await Task.Delay(1500);
                    break; // Возврат в главное меню
                }
                continue;
            }

            if (userMessage.Trim().ToLower() == "/rename")
            {
                var newTitle = AnsiConsole.Ask<string>("[cyan bold]Введите новое название:[/]");
                await historyService.RenameSessionAsync(currentSession.Id, newTitle);
                ConsoleUi.ShowInfo($"✏️ Чат переименован в '[cyan]{newTitle}[/]'.");
                continue;
            }

            // ==========================================
            // 📚 ОБРАБОТКА КОМАНД БАЗ ЗНАНИЙ
            // ==========================================
            var trimmedMsg = userMessage.Trim();

            // 1. Список баз
            if (trimmedMsg.Equals("/list_kbs", StringComparison.OrdinalIgnoreCase))
            {
                var attachedKbs = await documentRagService.GetAttachedKbsForSessionAsync(currentSession.Id);
                var allKbs = await documentRagService.GetUserKnowledgeBasesAsync(_currentUser.Id);

                AnsiConsole.MarkupLine("\n[cyan bold]📚 Доступные Базы Знаний:[/]");
                if (!allKbs.Any())
                {
                    AnsiConsole.MarkupLine("  [dim]У вас пока нет баз знаний. Создайте их в главном меню.[/]");
                }
                else
                {
                    foreach (var kb in allKbs)
                    {
                        var isAttached = attachedKbs.Any(k => k.Id == kb.Id);
                        var status = isAttached ? "[green]✅ Подключена[/]" : "[dim]○ Не подключена[/]";
                        AnsiConsole.MarkupLine($"  {status} [cyan]{kb.Name}[/]");
                    }
                }
                AnsiConsole.MarkupLine("");
                continue; // Пропускаем отправку сообщения в LLM
            }

            // 2. Подключение базы
            if (trimmedMsg.StartsWith("/attach_kb ", StringComparison.OrdinalIgnoreCase))
            {
                var kbName = trimmedMsg.Substring("/attach_kb ".Length).Trim().Trim('"');
                var availableKbs = await documentRagService.GetUserKnowledgeBasesAsync(_currentUser.Id);
                var kbToAttach = availableKbs.FirstOrDefault(k => k.Name.Equals(kbName, StringComparison.OrdinalIgnoreCase));

                if (kbToAttach == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ База знаний '{kbName}' не найдена. Проверьте название или используйте /list_kbs.[/]");
                }
                else
                {
                    await documentRagService.AttachKbToSessionAsync(currentSession.Id, kbToAttach.Id);
                    AnsiConsole.MarkupLine($"[green]✅ База знаний [cyan]{kbToAttach.Name}[/] успешно подключена к этому чату![/]");
                }
                continue; // Пропускаем отправку сообщения в LLM
            }

            // 3. Отключение базы
            if (trimmedMsg.StartsWith("/detach_kb ", StringComparison.OrdinalIgnoreCase))
            {
                var kbName = trimmedMsg.Substring("/detach_kb ".Length).Trim().Trim('"');
                var attachedKbs = await documentRagService.GetAttachedKbsForSessionAsync(currentSession.Id);
                var kbToDetach = attachedKbs.FirstOrDefault(k => k.Name.Equals(kbName, StringComparison.OrdinalIgnoreCase));

                if (kbToDetach == null)
                {
                    AnsiConsole.MarkupLine($"[red]❌ База знаний '{kbName}' не подключена к этому чату.[/]");
                }
                else
                {
                    await documentRagService.DetachKbFromSessionAsync(currentSession.Id, kbToDetach.Id);
                    AnsiConsole.MarkupLine($"[yellow]⛔ База знаний [cyan]{kbToDetach.Name}[/] отключена от этого чата.[/]");
                }
                continue; // Пропускаем отправку сообщения в LLM
            }
            // 4. Помощь
            if (trimmedMsg.Equals("/help", StringComparison.OrdinalIgnoreCase) || trimmedMsg.Equals("/?", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("\n[cyan bold]💡 Доступные команды чата:[/]");
                AnsiConsole.MarkupLine("  [yellow]/list_kbs[/]        - Показать доступные базы знаний");
                AnsiConsole.MarkupLine("  [yellow]/attach_kb \"Имя\"[/] - Подключить базу знаний к этому чату");
                AnsiConsole.MarkupLine("  [yellow]/detach_kb \"Имя\"[/] - Отключить базу знаний");
                AnsiConsole.MarkupLine("  [yellow]/exit[/], [yellow]/quit[/], [yellow]/e[/], [yellow]/q[/]   - Выйти в главное меню\n");
                continue;
            }
            // ==========================================

            // RAG: Поиск контекста ДО сохранения сообщения
            // ==========================================
            // 🧠 RAG: ПОИСК КОНТЕКСТА ДО ОТВЕТА МОДЕЛИ
            // ==========================================
            var currentMessageVector = await ollamaService.GetEmbeddingAsync(userMessage, isQuery: true);

            if (currentMessageVector != null)
            {
                // 1. Поиск в истории прошлых чатов (как было)
                var similarMessages = await historyService.SearchRelevantContextAsync(
                    _currentUser.Id,
                    _selectedModel,
                    currentMessageVector,
                    limit: 3);

                if (similarMessages.Any())
                {
                    var chatContextPrompt = "[ИСТОРИЯ ЧАТОВ]: В прошлых диалогах обсуждалось:\n";
                    foreach (var msg in similarMessages)
                    {
                        chatContextPrompt += $"- {msg.Content}\n";
                    }
                    memoryHistory.Add(("system", chatContextPrompt));
                }

                // 2. 🔥 НОВОЕ: Поиск в подключенных Базах Знаний (Document RAG)
                var attachedKbs = await documentRagService.GetAttachedKbsForSessionAsync(currentSession.Id);

                if (attachedKbs.Any())
                {
                    var attachedKbIds = attachedKbs.Select(kb => kb.Id).ToList();

                    // Ищем релевантные чанки в подключенных базах
                    var relevantChunks = await documentRagService.SearchRelevantChunksAsync(
                        attachedKbIds,
                        userMessage,
                        limit: 3); // Можно увеличить до 5, если нужно больше контекста

                    if (relevantChunks.Any())
                    {
                        /*
                        // 🔍 DEBUG: Выводим сами чанки, чтобы увидеть, что именно нашла система
                        AnsiConsole.MarkupLine("\n[yellow bold]🐛 DEBUG: Найденные чанки для промпта:[/]");
                        foreach (var chunk in relevantChunks)
                        {
                            AnsiConsole.MarkupLine($"[dim]📍 Источник: {chunk.BreadcrumbPath}[/]");
                            AnsiConsole.MarkupLine($"[cyan]{chunk.ChunkText.Trim()}[/]");
                            AnsiConsole.MarkupLine("[dim]---[/]");
                        }
                        AnsiConsole.MarkupLine(""); // Пустая строка для красоты
                        */

                        var docContextPrompt = "\n[СТРОГАЯ ИНСТРУКЦИЯ]: Ниже приведена информация из подключенных баз знаний. Ты ОБЯЗАН отвечать ИСКЛЮЧИТЕЛЬНО на основе этого текста. Если в тексте нет прямого ответа на вопрос, так и скажи: 'В предоставленных документах нет информации об этом'. НЕ ВЫДУМЫВАЙ факты и не используй свои общие знания, если они противоречат документу!\n\n";
                        foreach (var chunk in relevantChunks)
                        {
                            docContextPrompt += $"📍 Источник: {chunk.BreadcrumbPath}\n";
                            docContextPrompt += $"📄 Текст: {chunk.ChunkText.Trim()}\n";
                            docContextPrompt += "---\n";
                        }
                        memoryHistory.Add(("system", docContextPrompt));

                        AnsiConsole.MarkupLine($"[dim]🔍 Найдено {relevantChunks.Count} релевантных фрагментов в документах.[/]");
                    }
                }
            }
            // ==========================================

            // Сохранение сообщения пользователя
            await historyService.SaveMessageAsync(currentSession.Id, "user", userMessage);
            memoryHistory.Add(("user", userMessage));

            // Получение ответа от AI
            ConsoleUi.StartAssistantResponse();
            var assistantResponse = string.Empty;

            await foreach (var token in ollamaService.StreamChatResponseAsync(_selectedModel, userMessage, memoryHistory))
            {
                Console.Write(token);
                assistantResponse += token;
            }
            Console.WriteLine();

            // Сохранение ответа ассистента
            await historyService.SaveMessageAsync(currentSession.Id, "assistant", assistantResponse);
            memoryHistory.Add(("assistant", assistantResponse));

            ConsoleUi.EndAssistantResponse();
        }
    }

    private async Task HandleDeleteChatAsync(ChatHistoryService historyService, List<ChatSession> sessions)
    {
        var sessionToDelete = ConsoleUi.SelectChatFromList(sessions, "🗑️ Какой чат удалить?");
        if (sessionToDelete != null)
        {
            var confirm = AnsiConsole.Confirm($"[red]Удалить чат '[cyan]{Markup.Escape(sessionToDelete.Title ?? "Без названия")}[/]'?[/]");
            if (confirm)
            {
                await historyService.DeleteSessionAsync(sessionToDelete.Id);
                ConsoleUi.ShowInfo($"🗑️ Чат удален.");
            }
        }
    }

    private async Task HandleRenameChatAsync(ChatHistoryService historyService, List<ChatSession> sessions)
    {
        var sessionToRename = ConsoleUi.SelectChatFromList(sessions, "✏️ Какой чат переименовать?");
        if (sessionToRename != null)
        {
            var newTitle = AnsiConsole.Ask<string>("[cyan bold]Введите новое название:[/]");
            await historyService.RenameSessionAsync(sessionToRename.Id, newTitle);
            ConsoleUi.ShowInfo($"✏️ Чат переименован.");
        }
    }
}