using AiTelegramBot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace AiTelegramBot.Handlers;

public class MessageHandler
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly IConversationService _conversationService;
    private readonly IContentFilterService _contentFilterService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        IAiServiceFactory aiServiceFactory,
        IConversationService conversationService,
        IContentFilterService contentFilterService,
        ILocalizationService localizationService,
        ILogger<MessageHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _conversationService = conversationService;
        _contentFilterService = contentFilterService;
        _localizationService = localizationService;
        _logger = logger;
    }

    public async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message)
    {
        if (message.Text == null || message.From == null) return;

        var userId = message.From.Id;
        var userName = message.From.FirstName;
        var userMessage = message.Text;

        // Get user's language
        var languageCode = _conversationService.GetUserLanguage(userId);
        var strings = _localizationService.GetStrings(languageCode);

        _logger.LogInformation("Received message from user {UserId}: {Message}",
            userId, userMessage);

        // Check for blocked content
        if (_contentFilterService.ContainsBlockedContent(userMessage))
        {
            var warning = _contentFilterService.GetWarningMessage();
            if (!string.IsNullOrEmpty(warning))
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: warning
                );
            }
            return;
        }

        // Send typing indicator
        await botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing
        );

        try
        {
            // Get or create conversation context
            var context = _conversationService.GetOrCreateContext(userId, userName);

            // Add user message to context
            _conversationService.AddMessage(userId, "user", userMessage);

            // Get user's selected AI provider
            var provider = _conversationService.GetUserAiProvider(userId);
            var aiService = _aiServiceFactory.CreateService(provider);

            // Get AI response
            var aiResponse = await aiService.GetResponseAsync(userMessage, context);

            // Replace em dash with regular dash
            aiResponse = aiResponse.Replace("—", "-");

            // Add AI response to context
            _conversationService.AddMessage(userId, "assistant", aiResponse);

            // Send response to user
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: aiResponse
            );

            _logger.LogInformation("Sent response to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from user {UserId}", userId);
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: strings.ErrorProcessing
            );
        }
    }
}
