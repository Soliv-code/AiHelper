```text
AiHelper/
├── Components
│   ├── AppContext.cs
│   ├── ChatPage.cs
│   ├── IAppContext.cs
│   └── MainMenuPage.cs
├── Data
│   └── AiHelperDbContext.cs
├── Docker
│   ├── SQL
│   │   └── init.sql
│   └── docker-compose.yml
├── Docs
│   └── Todo.md
├── Models
│   ├── AppState.cs
│   ├── ChatMessage.cs
│   ├── ChatSession.cs
│   ├── User.cs
│   └── UserPreference.cs
├── Services
│   ├── ChatHistoryService.cs
│   ├── IChatHistoryService.cs
│   ├── InfrastructureValidator.cs
│   ├── IOllamaService.cs
│   ├── IUserService.cs
│   ├── OllamaService.cs
│   └── UserService.cs
├── UI
│   └── ConsoleUi.cs
├── .gitattributes
├── .gitignore
├── AiHelper.csproj
├── AiHelper.slnx
├── appsettings.json                # Создайте сами, чтобы подключить БД
├── appsettings.json.example        # Пример для подключения БД
├── Program.cs
├── README.md
└── Start-app.ps1
```