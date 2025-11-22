using AiTelegramBot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AiTelegramBot.Handlers;

public class MessageHandler
{
    private readonly IAiServiceFactory _aiServiceFactory;
    private readonly IConversationService _conversationService;
    private readonly IContentFilterService _contentFilterService;
    private readonly ILocalizationService _localizationService;
    private readonly ITourGuideService _tourGuideService;
    private readonly IRouteService _routeService;
    private readonly ILogger<MessageHandler> _logger;
    private static readonly Dictionary<long, string> _userModes = new();

    public MessageHandler(
        IAiServiceFactory aiServiceFactory,
        IConversationService conversationService,
        IContentFilterService contentFilterService,
        ILocalizationService localizationService,
        ITourGuideService tourGuideService,
        IRouteService routeService,
        ILogger<MessageHandler> logger)
    {
        _aiServiceFactory = aiServiceFactory;
        _conversationService = conversationService;
        _contentFilterService = contentFilterService;
        _localizationService = localizationService;
        _tourGuideService = tourGuideService;
        _routeService = routeService;
        _logger = logger;
    }

    public static void SetUserMode(long userId, string mode)
    {
        _userModes[userId] = mode;
    }

    public static void ClearUserMode(long userId)
    {
        _userModes.Remove(userId);
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

        // Check if user is in route mode
        if (_userModes.TryGetValue(userId, out var mode) && mode == "route")
        {
            await HandleRouteLocationAsync(botClient, message, location);
            ClearUserMode(userId);
            return;
        }

        // Default to tour mode
        try
        {
            // Remove custom keyboard
            var removeKeyboard = new ReplyKeyboardRemove();

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🔍 Изучаю ваше местоположение...\n📝 Готовлю экскурсию...",
                replyMarkup: removeKeyboard
            );

            var result = await _tourGuideService.GenerateTourAsync(location.Latitude, location.Longitude);

            if (result == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "😔 К сожалению, не удалось сгенерировать экскурсию для данного местоположения.\n\n" +
                          "Попробуйте:\n" +
                          "• Отправить геолокацию в другой точке Казани\n" +
                          "• Проверить подключение к интернету\n" +
                          "• Повторить попытку позже"
                );
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"📍 *{result.Location?.Address}*\n\n{result.Text}\n\n" +
                      "🎧 Слушайте аудио-версию ниже 👇",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown
            );

            if (result.AudioData != null && result.AudioData.Length > 0)
            {
                using var audioStream = new MemoryStream(result.AudioData);
                await botClient.SendVoiceAsync(
                    chatId: message.Chat.Id,
                    voice: InputFile.FromStream(audioStream, "tour.mp3"),
                    caption: $"🎧 Аудио-экскурсия\n📍 {result.Location?.Address}"
                );

                _logger.LogInformation("Sent tour guide response with audio to user {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Audio generation failed for user {UserId}", userId);
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "ℹ️ Аудио-версия недоступна, но текст экскурсии выше!\n\n" +
                          "💡 Отправьте /tour чтобы узнать о другом месте"
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing location from user {UserId}", userId);
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Произошла ошибка при генерации экскурсии.\n\n" +
                      "Пожалуйста, попробуйте:\n" +
                      "• Отправить /tour и геолокацию снова\n" +
                      "• Проверить подключение к интернету\n" +
                      "• Написать администратору, если проблема повторяется"
            );
        }
    }

    private async Task HandleRouteLocationAsync(ITelegramBotClient botClient, Message message, Location location)
    {
        var userId = message.From?.Id ?? 0;

        try
        {
            var removeKeyboard = new ReplyKeyboardRemove();

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🗺️ Ищу ближайшие достопримечательности...\n📍 Строю оптимальный маршрут...",
                replyMarkup: removeKeyboard
            );

            var route = await _routeService.BuildRouteAsync(location.Latitude, location.Longitude, maxPoints: 7);

            if (route == null)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "😔 К сожалению, произошла ошибка при построении маршрута.\n\n" +
                          "Попробуйте:\n" +
                          "• Отправить /route и геолокацию снова\n" +
                          "• Проверить подключение к интернету"
                );
                return;
            }

            if (route.Points.Count == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "😔 К сожалению, не удалось построить маршрут.\n\n" +
                          "Попробуйте отправить /route снова."
                );
                return;
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: route.Description
            );

            if (!string.IsNullOrEmpty(route.YandexMapsUrl))
            {
                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithUrl("🗺️ Открыть маршрут в Яндекс.Картах", route.YandexMapsUrl)
                });

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "📱 Нажмите на кнопку ниже, чтобы открыть маршрут в Яндекс.Картах с пошаговой навигацией:",
                    replyMarkup: keyboard
                );
            }

            _logger.LogInformation("Sent route to user {UserId} with {Count} points", userId, route.Points.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building route for user {UserId}", userId);
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Произошла ошибка при построении маршрута.\n\n" +
                      "Пожалуйста, попробуйте:\n" +
                      "• Отправить /route и геолокацию снова\n" +
                      "• Проверить подключение к интернету\n" +
                      "• Написать администратору, если проблема повторяется"
            );
        }
    }
}
