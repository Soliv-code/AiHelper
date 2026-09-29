
# 🤖 AiHelper

Консольное AI-приложение на **.NET 10**, использующее локальную **Ollama** для генерации ответов с потоковой передачей (streaming) и сохраняющее историю диалогов в **PostgreSQL 17 + pgVector**.

## 🚀 Возможности

- 📋 Интерактивный выбор модели из списка локально установленных в Ollama
- 👤 Идентификация пользователей (создание / выбор существующего)
- 💬 Стриминг ответов от AI прямо в консоль (токен за токеном)
- 💾 Сохранение всей истории диалогов в PostgreSQL
- 🔗 Связь: Пользователь → Чаты → Сообщения
- 🎨 Красивый UI на базе `Spectre.Console` с эмодзи

## 🛠 Технологический стек

| Слой | Технология |
|------|------------|
| Runtime | .NET 10 |
| AI | Ollama (нативно) + OllamaSharp |
| БД | PostgreSQL 17 + pgVector 0.8.0 |
| ORM | Entity Framework Core (Database-First) |
| UI | Spectre.Console |
| Инфраструктура | Docker Compose |

## 📁 Структура проекта
```
AiHelper/
├── Docker/
│ ├── docker-compose.yml # PostgreSQL + pgVector
│ └── SQL/
│ └── init.sql # Схема БД, индексы, триггеры
├── Models/ # Сущности EF Core (сгенерированы из БД)
├── Services/ # Бизнес-логика (Ollama, история, пользователи)
├── UI/ # Консольный интерфейс (Spectre.Console)
├── Data/ # AiHelperDbContext
├── Docs/ # Документация и TODO
├── Program.cs # Точка входа
├── appsettings.json # Конфигурация (локально, не в git)
└── appsettings.json.example # Шаблон конфигурации
```

## ⚡ Быстрый старт

### 1. Клонировать репозиторий
```powershell
git clone https://github.com/ТВОЙ_ЛОГИН/AiHelper.git
cd AiHelper
```

### 2. Запустить базу данных
```powershell
cd Docker
docker compose up -d
```

### 3. Настроить подключение к БД
Скопируй `appsettings.json.example` в `appsettings.json` и укажи свой пароль:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=aihelper_db;Username=postgres;Password=твой_пароль"
  }
}
```

### 4. Запустить Ollama
Убедись, что Ollama запущена локально и есть хотя бы одна скачанная модель:
```powershell
ollama list
```

### 5. Запустить приложение
Открой AiHelper.sln в Visual Studio 2026 и нажми F5.

## 🎮 Использование
1. При запуске выберите пользователя (или создайте нового)
2. Выберите модель из списка доступных в Ollama
3. Общайтесь с AI — ответы стримятся в реальном времени
4. Выход: /exit, /quit, /e или /q

## 📝 Лицензия
MIT
