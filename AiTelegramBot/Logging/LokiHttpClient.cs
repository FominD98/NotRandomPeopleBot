using Microsoft.Extensions.Configuration;
using Serilog.Sinks.Http;

namespace AiTelegramBot.Logging;

public class LokiHttpClient : IHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _userId;
    private readonly string _token;

    public LokiHttpClient(string userId, string token)
    {
        _httpClient = new HttpClient();
        _userId = userId;
        _token = token;
    }

    public void Configure(IConfiguration configuration)
    {
        // Configuration is handled through constructor parameters
        // This method is required by IHttpClient interface but not used in our case
    }

    public async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream)
    {
        // Добавляем правильный заголовок авторизации для Grafana Cloud
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_userId}:{_token}");

        var content = new StreamContent(contentStream);
        content.Headers.Add("Content-Type", "application/json");

        return await _httpClient.PostAsync(requestUri, content);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
