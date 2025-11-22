using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class ElevenLabsService : IElevenLabsService
{
    private readonly HttpClient _httpClient;
    private readonly ElevenLabsSettings _settings;
    private readonly ILogger<ElevenLabsService> _logger;

    public ElevenLabsService(
        HttpClient httpClient,
        IOptions<ElevenLabsSettings> settings,
        ILogger<ElevenLabsService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<byte[]?> GenerateAudioAsync(string text)
    {
        try
        {
            _logger.LogInformation("Generating audio for text length: {Length}", text.Length);

            var url = $"{_settings.ApiUrl}/{_settings.VoiceId}";

            var requestBody = new
            {
                text = text,
                model_id = _settings.ModelId,
                voice_settings = new
                {
                    stability = 0.5,
                    similarity_boost = 0.75
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("xi-api-key", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("ElevenLabs API error: {StatusCode}, {Response}",
                    response.StatusCode, errorContent);
                return null;
            }

            var audioBytes = await response.Content.ReadAsByteArrayAsync();
            _logger.LogInformation("Successfully generated audio, size: {Size} bytes", audioBytes.Length);

            return audioBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling ElevenLabs API");
            return null;
        }
    }
}
