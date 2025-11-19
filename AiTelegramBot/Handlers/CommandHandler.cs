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
        if (string.IsNullOrEmpty(userName))
        {
            return strings.StartMessage;
        }
        return string.Format(strings.StartMessageWithName, userName);
    }

    private string GetHelpMessage(LocalizedStrings strings)
    {
        return strings.HelpMessage +
               strings.HelpCommandStart + "\n" +
               strings.HelpCommandHelp + "\n" +
               strings.HelpCommandAbout + "\n" +
               strings.HelpCommandReset + "\n" +
               strings.HelpCommandLanguage + "\n" +
               strings.HelpCommandProvider +
               strings.HelpMessageFooter;
    }

    private string GetAboutMessage(LocalizedStrings strings)
    {
        return strings.AboutMessage +
               strings.AboutDescription + "\n" +
               strings.AboutCapabilities +
               strings.AboutCapability1 + "\n" +
               strings.AboutCapability2 + "\n" +
               strings.AboutCapability3 + "\n" +
               strings.AboutCapability4 + "\n" +
               strings.AboutCapability5;
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
}
