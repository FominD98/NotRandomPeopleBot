using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class AiServiceFactory : IAiServiceFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<DeepSeekSettings> _deepSeekSettings;
    private readonly IOptions<OpenAISettings> _openAISettings;
    private readonly IOptions<YandexGptSettings> _yandexGptSettings;
    private readonly IOptions<ConversationSettings> _conversationSettings;
    private readonly ILoggerFactory _loggerFactory;

    public AiServiceFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<DeepSeekSettings> deepSeekSettings,
        IOptions<OpenAISettings> openAISettings,
        IOptions<YandexGptSettings> yandexGptSettings,
        IOptions<ConversationSettings> conversationSettings,
        ILoggerFactory loggerFactory)
    {
        _httpClientFactory = httpClientFactory;
        _deepSeekSettings = deepSeekSettings;
        _openAISettings = openAISettings;
        _yandexGptSettings = yandexGptSettings;
        _conversationSettings = conversationSettings;
        _loggerFactory = loggerFactory;
    }

    public IAiService CreateService(string provider)
    {
        var httpClient = _httpClientFactory.CreateClient();

        return provider.ToLower() switch
        {
            "openai" => new OpenAIService(
                httpClient,
                _openAISettings,
                _conversationSettings,
                _loggerFactory.CreateLogger<OpenAIService>()),

            "yandexgpt" or "yandex" => new YandexGptService(
                httpClient,
                _yandexGptSettings,
                _conversationSettings,
                _loggerFactory.CreateLogger<YandexGptService>()),

            "deepseek" or _ => new DeepSeekService(
                httpClient,
                _deepSeekSettings,
                _conversationSettings,
                _loggerFactory.CreateLogger<DeepSeekService>())
        };
    }
}
