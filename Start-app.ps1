# Start-App.ps1
# Скрипт для автоматического запуска AiHelper

Write-Host "🚀 Запуск AiHelper..." -ForegroundColor Cyan

# 1. Проверяем и запускаем Docker Compose
Write-Host "🐳 Проверка состояния Docker..." -ForegroundColor Yellow
$dockerStatus = docker ps --filter "name=aihelper_postgres" --format "{{.Names}}"
if ($dockerStatus -eq "aihelper_postgres") {
    Write-Host "   ✅ Контейнер PostgreSQL уже работает." -ForegroundColor Green
} else {
    Write-Host "   ⚙️ Запускаем PostgreSQL через Docker Compose..." -ForegroundColor Yellow
    docker compose -f Docker/docker-compose.yml up -d
    Start-Sleep -Seconds 3 # Даем базе пару секунд на инициализацию
    Write-Host "   ✅ База данных запущена!" -ForegroundColor Green
}

# 2. Проверяем Ollama через HTTP-запрос (намного надежнее, чем ollama list)
Write-Host "🦙 Проверка состояния Ollama..." -ForegroundColor Yellow
try {
    # Делаем быстрый запрос к корню API Ollama с таймаутом 3 секунды
    $response = Invoke-WebRequest -Uri "http://localhost:11434" -UseBasicParsing -TimeoutSec 3 -ErrorAction Stop
    
    if ($response.StatusCode -eq 200) {
        Write-Host "   ✅ Ollama работает и отвечает (API доступен)." -ForegroundColor Green
    }
} catch {
    Write-Host "   ❌ Ollama не отвечает на http://localhost:11434" -ForegroundColor Red
    Write-Host "   💡 Действия:" -ForegroundColor Yellow
    Write-Host "      1. Найди иконку 🦙 Ollama в трее (возле часов Windows)."
    Write-Host "      2. Нажми правой кнопкой -> 'Quit Ollama'."
    Write-Host "      3. Запусти Ollama заново из меню Пуск."
    Write-Host "      4. Повтори запуск этого скрипта."
    exit
}

# 3. Сборка и запуск .NET приложения
Write-Host "🔨 Сборка и запуск приложения..." -ForegroundColor Yellow
# dotnet run автоматически выполнит build, если код изменился, и запустит из правильной директории
dotnet run --project AiHelper.csproj