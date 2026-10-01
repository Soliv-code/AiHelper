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
        var ollamaService = _appContext.GetService<OllamaService>();

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

            // RAG: Поиск контекста ДО сохранения сообщения
            var currentMessageVector = await ollamaService.GetEmbeddingAsync(userMessage);
            if (currentMessageVector != null)
            {
                var similarMessages = await historyService.SearchRelevantContextAsync(
                    _currentUser.Id,
                    _selectedModel,
                    currentMessageVector,
                    limit: 3);

                if (similarMessages.Any())
                {
                    ConsoleUi.ShowContext(similarMessages);
                    var contextPrompt = "Контекст из прошлых чатов пользователя (модель " + _selectedModel + "):\n";
                    foreach (var msg in similarMessages)
                    {
                        contextPrompt += $"- {msg.Content}\n";
                    }
                    memoryHistory.Add(("system", contextPrompt));
                }
            }

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