using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiTelegramBot.Models;
using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class DeepSeekService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly DeepSeekSettings _settings;
    private readonly ConversationSettings _conversationSettings;
    private readonly ILogger<DeepSeekService> _logger;

    public DeepSeekService(
        HttpClient httpClient,
        IOptions<DeepSeekSettings> settings,
        IOptions<ConversationSettings> conversationSettings,
        ILogger<DeepSeekService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _conversationSettings = conversationSettings.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task<string> GetResponseAsync(string userMessage, ConversationContext context)
    {
        try
        {
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
                new { role = "system", content = systemPrompt }
            };

            // Add conversation history
            foreach (var msg in context.Messages.TakeLast(_conversationSettings.MaxHistoryMessages))
            {
                messages.Add(new { role = msg.Role, content = msg.Content });
            }

            // Add current message
            messages.Add(new { role = "user", content = userMessage });

            var requestBody = new
            {
                model = _settings.Model,
                messages,
                temperature = _settings.Temperature,
                max_tokens = _settings.MaxTokens
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending request to DeepSeek API for user {UserId}", context.UserId);

            var response = await _httpClient.PostAsync(_settings.ApiUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("DeepSeek API error: {StatusCode}, {Response}",
                    response.StatusCode, responseContent);
                return "Извините, произошла ошибка при обработке вашего запроса. Попробуйте позже.";
            }

            using var doc = JsonDocument.Parse(responseContent);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentElement))
                {
                    var aiResponse = contentElement.GetString() ?? string.Empty;
                    _logger.LogInformation("Received response from DeepSeek for user {UserId}", context.UserId);
                    return aiResponse;
                }
            }

            _logger.LogWarning("Unexpected DeepSeek API response format");
            return "Не удалось получить ответ от AI.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling DeepSeek API for user {UserId}", context.UserId);
            return "Произошла ошибка при обращении к AI сервису.";
        }
    }
}
