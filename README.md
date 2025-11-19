# AI Telegram Bot

Telegram-бот с поддержкой нескольких AI провайдеров и многоязычным интерфейсом.

## Возможности

- **3 AI провайдера**: DeepSeek, OpenAI (GPT-4), Yandex GPT
- **Языки**: русский и татарский
- Сохранение контекста диалога (до 10 сообщений)
- Фильтрация нежелательного контента
- Асинхронная обработка запросов с индикацией набора текста
- Логирование в Grafana Loki + консоль
- Unit-тесты

## Архитектура

Проект построен на чистой архитектуре с разделением на слои:

**Handlers** → обрабатывают входящие сообщения от Telegram
**Services** → бизнес-логика (работа с AI, контекст, фильтрация)
**Models** → данные и настройки

### Как работает

1. `MessageHandler` получает сообщение от пользователя
2. Проверяет контент через `ContentFilterService`
3. Получает/создает контекст диалога через `ConversationService`
4. Выбирает AI провайдер через `AiServiceFactory`
5. Отправляет запрос к AI (DeepSeek/OpenAI/Yandex) с контекстом
6. Заменяет длинные тире на обычные в ответе
7. Сохраняет ответ в контекст и отправляет пользователю

Контекст хранится в памяти (словарь userId → ConversationContext).

## Логирование

Используется встроенный `ILogger` из ASP.NET Core:

- **Консоль**: все логи с уровнем Information и выше
- **Grafana Loki** (опционально): структурированные логи с метками (service, environment)

Что логируется:
- Входящие сообщения пользователей
- Запросы к AI API (без содержимого)
- Ошибки при обработке (с деталями)
- Смена языка/провайдера

Настройка в `appsettings.json` → `Logging`:
```json
{
  "Logging": {
    "ServiceName": "aiTelegramBot",
    "DeploymentEnvironment": "production",
    "LokiEndpoint": "https://logs-prod-039.grafana.net/loki/api/v1/push",
    "LokiUserId": "ваш_id",
    "GrafanaCloudAccessToken": "ваш_токен"
  }
}
```

Без настроек Loki - логи только в консоль.

## Требования

- .NET 8.0
- Telegram Bot Token (через @BotFather)
- API ключ хотя бы одного провайдера (DeepSeek, OpenAI или Yandex GPT)

## Настройка

1. Скопируйте `appsettings.example.json` в `appsettings.json`
2. Получите токен бота через [@BotFather](https://t.me/BotFather)
3. Получите API ключ провайдера:
   - DeepSeek: https://platform.deepseek.com
   - OpenAI: https://platform.openai.com
   - Yandex GPT: https://cloud.yandex.ru
4. Заполните `appsettings.json`

Минимальная конфигурация:
```json
{
  "Telegram": {
    "BotToken": "ваш_токен_бота"
  },
  "AiProvider": {
    "Provider": "DeepSeek"
  },
  "DeepSeek": {
    "ApiKey": "ваш_ключ"
  }
}
```

## Запуск

```bash
cd AiTelegramBot
dotnet restore
dotnet build
dotnet run
```

## Запуск тестов

```bash
cd AiTelegramBot.Tests
dotnet test
```

## Команды

- `/start` - начать диалог
- `/help` - список команд
- `/about` - информация о боте
- `/reset` - сбросить контекст
- `/language` - выбрать язык (ru/tt)
- `/provider` - сменить AI провайдер

## Тесты

Покрыты основные сервисы:
- `ConversationService` - управление контекстом, языками, провайдерами
- `ContentFilterService` - фильтрация запрещенных слов

```bash
dotnet test
```

## Структура проекта

```
AiTelegramBot/
├── Handlers/              # Обработчики команд и сообщений
├── Services/              # AI сервисы, контекст, фильтрация, локализация
├── Models/                # Конфигурация и модели данных
├── Logging/               # Loki интеграция
└── Program.cs

AiTelegramBot.Tests/       # Unit-тесты
```
