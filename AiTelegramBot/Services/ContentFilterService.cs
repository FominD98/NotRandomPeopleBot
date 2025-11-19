using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class ContentFilterService : IContentFilterService
{
    private readonly ContentFilterSettings _settings;
    private readonly ILogger<ContentFilterService> _logger;

    public ContentFilterService(
        IOptions<ContentFilterSettings> settings,
        ILogger<ContentFilterService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool ContainsBlockedContent(string message)
    {
        if (!_settings.EnableFiltering)
            return false;

        var lowerMessage = message.ToLowerInvariant();

        foreach (var blockedWord in _settings.BlockedWords)
        {
            if (lowerMessage.Contains(blockedWord.ToLowerInvariant()))
            {
                _logger.LogWarning("Blocked content detected: {Word}", blockedWord);
                return true;
            }
        }

        return false;
    }

    public string GetWarningMessage()
    {
        return _settings.WarnOnDetection
            ? "Ваше сообщение содержит недопустимый контент. Пожалуйста, перефразируйте."
            : string.Empty;
    }
}
