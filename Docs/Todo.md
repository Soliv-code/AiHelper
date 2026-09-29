# AiHelper - TODO

## 🎯 О проекте
Консольное AI-приложение на .NET 10 с локальной Ollama, стримингом ответов и сохранением истории в PostgreSQL.

## ✅ Выполнено
- [x] Инициализация проекта .NET 10 Console App (`AiHelper`)
- [x] Установка пакетов: `OllamaSharp`, `Spectre.Console`, `EF Core`, `Npgsql`, `Configuration`
- [x] Получение списка локальных моделей из Ollama
- [x] Базовый стриминг чата с выбором модели (через `await foreach`)
- [x] Рефакторинг: чистая структура (Services, UI, Models, Data, Docker)
- [x] Настройка Docker Compose с PostgreSQL 17 + pgVector (`pgvector/pgvector:pg17`)
- [x] Database-First: SQL-скрипт `init.sql` для создания таблиц, индексов, триггеров
- [x] Генерация моделей из БД через `Scaffold-DbContext`
- [x] Вынос строки подключения в `appsettings.json`
- [x] Идентификация пользователя (создание / выбор из списка)
- [x] Привязка чатов к `user_id` в БД

## 🚧 В работе / Следущие шаги
- [ ] Меню выбора существующих чатов (продолжить / создать новый)
- [ ] Загрузка истории чата из БД в контекст Ollama
- [ ] (Опционально) Удаление / переименование чатов
- [ ] (Будущее) Интеграция pgVector для RAG

## 📝 Заметки
- OS: Windows, PowerShell
- IDE: Visual Studio 2026
- Ollama: нативно (без Docker)
- PostgreSQL: через Docker Compose (папка `Docker/`)
- Эмодзи в UI: 👨‍💻 (пользователь), 🤖 (AI)
- Архитектура: один проект, строгая папочная структура
- Подход к БД: Database-First (SQL-скрипты, без Code-First миграций)