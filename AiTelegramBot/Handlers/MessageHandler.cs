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
    private readonly ITourGuideService _tourGuideService;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(
        IAiServiceFactory aiServiceFactory,
        IConversationService conversationService,
        IContentFilterService contentFilterService,
        ILocalizationService localizationService,
        ITourGuideService tourGuideService,
        ILogger<MessageHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _conversationService = conversationService;
        _contentFilterService = contentFilterService;
        _localizationService = localizationService;
        _tourGuideService = tourGuideService;
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

    public async Task HandleLocationMessageAsync(ITelegramBotClient botClient, Message message)
    {
        if (message.Location == null || message.From == null) return;

        var userId = message.From.Id;
        var location = message.Location;

        _logger.LogInformation("Received location from user {UserId}: {Latitude}, {Longitude}",
            userId, location.Latitude, location.Longitude);

        await botClient.SendChatActionAsync(
            chatId: message.Chat.Id,
            chatAction: ChatAction.Typing
        );

        try
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Генерирую экскурсию для вашего местоположения..."
            );

            var result = await _tourGuideService.GenerateTourAsync(location.Latitude, location.Longitude);

            if (result == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Не удалось сгенерировать экскурсию для данного местоположения."
                );
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"📍 {result.Location?.Address}\n\n{result.Text}"
            );

            if (result.AudioData != null && result.AudioData.Length > 0)
            {
                using var audioStream = new MemoryStream(result.AudioData);
                await botClient.SendVoiceAsync(
                    chatId: message.Chat.Id,
                    voice: InputFile.FromStream(audioStream, "tour.mp3"),
                    caption: "🎧 Аудио-экскурсия"
                );

                _logger.LogInformation("Sent tour guide response with audio to user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Audio generation failed for user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing location from user {UserId}", userId);
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Произошла ошибка при генерации экскурсии. Попробуйте позже."
            );
        }
    }
}
