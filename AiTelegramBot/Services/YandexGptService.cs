using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiTelegramBot.Models;
using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class YandexGptService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly YandexGptSettings _settings;
    private readonly ConversationSettings _conversationSettings;
    private readonly ILogger<YandexGptService> _logger;

    private string? _iamToken;
    private DateTime _iamTokenExpire = DateTime.MinValue;
    private DateTime _iamTokenLastFetch = DateTime.MinValue;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

    public YandexGptService(
        HttpClient httpClient,
        IOptions<YandexGptSettings> settings,
        IOptions<ConversationSettings> conversationSettings,
        ILogger<YandexGptService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _conversationSettings = conversationSettings.Value;
        _logger = logger;
    }

    private async Task<string> GetIamTokenAsync()
    {
        await _tokenSemaphore.WaitAsync();
        try
        {
            // Check if token is still valid (update every 2 hours)
            if (_iamToken != null && (DateTime.UtcNow - _iamTokenLastFetch).TotalHours < 2)
            {
                return _iamToken;
            }

            _logger.LogInformation("Fetching new Yandex IAM token");

            var requestBody = new { yandexPassportOauthToken = _settings.OAuthToken };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_settings.IamTokenUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Yandex IAM token: {StatusCode}, {Response}",
                    response.StatusCode, responseString);
                throw new Exception("Failed to get Yandex IAM token");
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            _iamToken = root.GetProperty("iamToken").GetString();
            _iamTokenExpire = root.GetProperty("expiresAt").GetDateTime();
            _iamTokenLastFetch = DateTime.UtcNow;

            _logger.LogInformation("Successfully fetched Yandex IAM token, expires at {ExpiresAt}", _iamTokenExpire);

            return _iamToken!;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    public async Task<string> GetResponseAsync(string userMessage, ConversationContext context)
    {
        try
        {
            // Get IAM token
            var iamToken = await GetIamTokenAsync();

            // Build system prompt with language instruction at the beginning
            var languageInstruction = context.Language switch
            {
                "tt" => "!!! КРИТИЧЕСКИ ВАЖНО !!! Ты ОБЯЗАН отвечать ИСКЛЮЧИТЕЛЬНО на ТАТАРСКОМ языке (татарча). " +
                        "Каждое слово твоего ответа должно быть на татарском языке. " +
                        "НЕ используй русский язык ни при каких обстоятельствах. " +
                        "Если не знаешь татарского перевода - используй простые татарские слова или опиши другими словами на татарском.\n\n",
                "ru" => "",
                _ => ""
            };

            var systemPrompt = languageInstruction + _conversationSettings.SystemPrompt;

            var messages = new List<object>
            {
                new { role = "system", text = systemPrompt }
            };

            // Add conversation history
            foreach (var msg in context.Messages.TakeLast(_conversationSettings.MaxHistoryMessages))
            {
                messages.Add(new { role = msg.Role, text = msg.Content });
            }

            // Add current message
            messages.Add(new { role = "user", text = userMessage });

            var requestBody = new
            {
                modelUri = $"gpt://{_settings.FolderId}/{_settings.Model}",
                completionOptions = new
                {
                    stream = false,
                    temperature = _settings.Temperature,
                    maxTokens = _settings.MaxTokens.ToString()
                },
                messages = messages.ToArray()
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", iamToken);

            _logger.LogInformation("Sending request to Yandex GPT API for user {UserId}", context.UserId);

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Yandex GPT API error: {StatusCode}, {Response}",
                    response.StatusCode, responseString);
                return "Извините, произошла ошибка при обработке вашего запроса. Попробуйте позже.";
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("result", out var result) &&
                result.TryGetProperty("alternatives", out var alternatives) &&
                alternatives.GetArrayLength() > 0)
            {
                var firstAlternative = alternatives[0];
                if (firstAlternative.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("text", out var textElement))
                {
                    var aiResponse = textElement.GetString() ?? string.Empty;
                    _logger.LogInformation("Received response from Yandex GPT for user {UserId}", context.UserId);
                    return aiResponse;
                }
            }

            _logger.LogWarning("Unexpected Yandex GPT API response format: {Response}", responseString);
            return "Не удалось получить ответ от AI.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Yandex GPT API for user {UserId}", context.UserId);
            return "Произошла ошибка при обращении к AI сервису.";
        }
    }
}
