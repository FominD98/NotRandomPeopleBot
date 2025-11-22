using AiTelegramBot.Models;
using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class TourGuideService : ITourGuideService
{
    private readonly IYandexGeocodingService _geocodingService;
    private readonly YandexGptService _gptService;
    private readonly IElevenLabsService _elevenLabsService;
    private readonly ConversationSettings _conversationSettings;
    private readonly ILogger<TourGuideService> _logger;

    public TourGuideService(
        IYandexGeocodingService geocodingService,
        YandexGptService gptService,
        IElevenLabsService elevenLabsService,
        IOptions<ConversationSettings> conversationSettings,
        ILogger<TourGuideService> logger)
    {
        _geocodingService = geocodingService;
        _gptService = gptService;
        _elevenLabsService = elevenLabsService;
        _conversationSettings = conversationSettings.Value;
        _logger = logger;
    }

    public async Task<TourGuideResult?> GenerateTourAsync(double latitude, double longitude)
    {
        try
        {
            _logger.LogInformation("Generating tour for coordinates: {Latitude}, {Longitude}", latitude, longitude);

            var locationInfo = await _geocodingService.GetLocationInfoAsync(latitude, longitude);
            if (locationInfo == null)
            {
                _logger.LogWarning("Could not get location info for coordinates");
                return null;
            }

            var tourPrompt = $@"Ты - электронный экскурсовод по Казани. Пользователь находится по адресу: {locationInfo.Address}.

Расскажи интересную историческую справку об этом месте в Казани. Если это известный объект культурного наследия, памятник архитектуры или историческое место - расскажи:
- Краткую историю объекта (когда построен, кем, для чего)
- Интересные факты и легенды, связанные с этим местом
- Архитектурные особенности (если применимо)
- Значение для истории Казани и Татарстана

Если это обычное место без исторической значимости - расскажи о районе, улице или ближайших достопримечательностях.

Ответ должен быть:
- Информативным и интересным
- Длиной 200-300 слов
- Подходящим для аудио-озвучки (без сложных терминов)
- На русском языке

Начни сразу с рассказа, без приветствий.";

            var context = new ConversationContext
            {
                UserId = 0,
                UserName = "TourGuide",
                Language = "ru",
                Messages = new List<Models.Message>()
            };

            var tourText = await _gptService.GetResponseAsync(tourPrompt, context);

            if (string.IsNullOrEmpty(tourText))
            {
                _logger.LogWarning("Could not generate tour text");
                return null;
            }

            _logger.LogInformation("Generated tour text, length: {Length}", tourText.Length);

            var audioData = await _elevenLabsService.GenerateAudioAsync(tourText);

            return new TourGuideResult
            {
                Text = tourText,
                AudioData = audioData,
                Location = locationInfo
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tour");
            return null;
        }
    }
}
