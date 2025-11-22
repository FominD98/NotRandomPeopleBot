using AiTelegramBot.Models;
using AiTelegramBot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramMessage = Telegram.Bot.Types.Message;

namespace AiTelegramBot.Handlers;

public class CommandHandler
{
    private readonly IConversationService _conversationService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<CommandHandler> _logger;

    public CommandHandler(
        IConversationService conversationService,
        ILocalizationService localizationService,
        ILogger<CommandHandler> logger)
    {
        _conversationService = conversationService;
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task HandleCommandAsync(ITelegramBotClient botClient, TelegramMessage message)
    {
        if (message.Text == null) return;

        var userId = message.From?.Id ?? 0;
        var command = message.Text.Split(' ')[0].ToLowerInvariant();

        _logger.LogInformation("Processing command {Command} from user {UserId}", command, userId);

        // Get user's current language
        var languageCode = _conversationService.GetUserLanguage(userId);
        var strings = _localizationService.GetStrings(languageCode);

        // Handle language change commands
        if (command == "/lang_ru")
        {
            await HandleLanguageChange(botClient, message, userId, "ru");
            return;
        }
        else if (command == "/lang_tt")
        {
            await HandleLanguageChange(botClient, message, userId, "tt");
            return;
        }

        // Handle provider change commands
        if (command == "/provider_deepseek")
        {
            await HandleProviderChange(botClient, message, userId, "DeepSeek", languageCode);
            return;
        }
        else if (command == "/provider_openai")
        {
            await HandleProviderChange(botClient, message, userId, "OpenAI", languageCode);
            return;
        }
        else if (command == "/provider_yandex")
        {
            await HandleProviderChange(botClient, message, userId, "YandexGpt", languageCode);
            return;
        }

        // Handle tour command with location request
        if (command == "/tour")
        {
            await HandleTourCommand(botClient, message);
            return;
        }

        // Handle route command with location request
        if (command == "/route")
        {
            await HandleRouteCommand(botClient, message);
            return;
        }

        string response = command switch
        {
            "/start" => GetStartMessage(message.From?.FirstName, strings),
            "/help" => GetHelpMessage(strings),
            "/about" => GetAboutMessage(strings),
            "/reset" => HandleResetCommand(userId, strings),
            "/language" => strings.LanguageSelection,
            "/provider" => strings.ProviderSelection,
            _ => strings.UnknownCommand
        };

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: response
        );
    }

    private string GetStartMessage(string? userName, LocalizedStrings strings)
    {
        var greeting = string.IsNullOrEmpty(userName)
            ? "Привет!"
            : $"Привет, {userName}!";

        return greeting + "\n\n" +
               "🏛️ Я - электронный экскурсовод по Казани!\n\n" +
               "Отправьте мне свою геолокацию, и я расскажу вам:\n" +
               "• Историю места, где вы находитесь\n" +
               "• Интересные факты и легенды\n" +
               "• Архитектурные особенности\n" +
               "• И озвучу всё это в аудио-формате! 🎧\n\n" +
               "📍 /tour - Начать экскурсию об этом месте\n" +
               "🗺️ /route - Построить маршрут по достопримечательностям\n\n" +
               "💬 Также вы можете задать мне любой вопрос о Казани и Татарстане!";
    }

    private string GetHelpMessage(LocalizedStrings strings)
    {
        return "📖 Доступные команды:\n\n" +
               "🗺️ Экскурсии:\n" +
               "/tour - Аудио-экскурсия о месте (отправьте геолокацию)\n" +
               "/route - Построить маршрут по достопримечательностям\n" +
               "/start - Главное меню\n\n" +
               "💬 Общение:\n" +
               "Просто напишите мне вопрос о Казани, Татарстане или любую другую тему!\n\n" +
               "⚙️ Настройки:\n" +
               "/reset - Сбросить историю диалога\n" +
               "/language - Выбрать язык (русский/татарский)\n" +
               "/provider - Выбрать AI модель\n" +
               "/about - О боте\n\n" +
               "💡 Совет: используйте /tour для аудио-экскурсий и /route для построения маршрутов!";
    }

    private string GetAboutMessage(LocalizedStrings strings)
    {
        return "🏛️ Электронный экскурсовод по Казани\n\n" +
               "Я помогу вам узнать больше о городе Казань и Республике Татарстан!\n\n" +
               "🎯 Мои возможности:\n\n" +
               "📍 Аудио-экскурсии:\n" +
               "• Отправьте мне свою геолокацию\n" +
               "• Получите историческую справку о месте\n" +
               "• Послушайте аудио-озвучку экскурсии\n\n" +
               "💬 Консультации:\n" +
               "• Задайте вопрос о достопримечательностях\n" +
               "• Узнайте историю Казани и Татарстана\n" +
               "• Получите рекомендации по маршрутам\n\n" +
               "🌐 Языки:\n" +
               "• Русский\n" +
               "• Татарский (в разработке)\n\n" +
               "🤖 Технологии:\n" +
               "• YandexGPT - генерация экскурсий\n" +
               "• ElevenLabs - озвучка текста\n" +
               "• Yandex Geocoding - определение адресов\n\n" +
               "Приятных прогулок по Казани! 🚶‍♂️";
    }

    private string HandleResetCommand(long userId, LocalizedStrings strings)
    {
        _conversationService.ResetContext(userId);
        _logger.LogInformation("User {UserId} reset conversation context", userId);
        return strings.ResetConfirmation;
    }

    private async Task HandleLanguageChange(ITelegramBotClient botClient, TelegramMessage message, long userId, string languageCode)
    {
        _conversationService.SetUserLanguage(userId, languageCode);
        var strings = _localizationService.GetStrings(languageCode);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: strings.LanguageChanged
        );
    }

    private async Task HandleProviderChange(ITelegramBotClient botClient, TelegramMessage message, long userId, string provider, string languageCode)
    {
        _conversationService.SetUserAiProvider(userId, provider);
        var strings = _localizationService.GetStrings(languageCode);

        var providerName = provider switch
        {
            "DeepSeek" => strings.ProviderDeepSeek,
            "OpenAI" => strings.ProviderOpenAI,
            "YandexGpt" => strings.ProviderYandexGpt,
            _ => provider
        };

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: string.Format(strings.ProviderChanged, providerName)
        );
    }

    private async Task HandleTourCommand(ITelegramBotClient botClient, TelegramMessage message)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[]
            {
                KeyboardButton.WithRequestLocation("📍 Отправить мою геолокацию")
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "🗺️ Электронный экскурсовод по Казани\n\n" +
                  "Отправьте мне свою геолокацию, и я расскажу вам об этом месте:\n" +
                  "• Историческую справку\n" +
                  "• Интересные факты\n" +
                  "• Аудио-экскурсию\n\n" +
                  "Нажмите на кнопку ниже, чтобы отправить свое местоположение.",
            replyMarkup: keyboard
        );

        _logger.LogInformation("Sent location request to user {UserId}", message.From?.Id ?? 0);
    }

    private async Task HandleRouteCommand(ITelegramBotClient botClient, TelegramMessage message)
    {
        var userId = message.From?.Id ?? 0;

        // Set user mode to route
        MessageHandler.SetUserMode(userId, "route");

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[]
            {
                KeyboardButton.WithRequestLocation("📍 Отправить мою геолокацию")
            }
        })
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "🗺️ Построение маршрута по Казани\n\n" +
                  "Отправьте мне свою геолокацию, и я построю оптимальный маршрут:\n" +
                  "• По ближайшим достопримечательностям\n" +
                  "• С расчетом расстояний\n" +
                  "• Со ссылкой на Яндекс.Карты\n\n" +
                  "Нажмите на кнопку ниже, чтобы отправить свое местоположение.",
            replyMarkup: keyboard
        );

        _logger.LogInformation("Sent route location request to user {UserId}", userId);
    }
}
