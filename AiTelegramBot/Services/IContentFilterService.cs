namespace AiTelegramBot.Services;

public interface IContentFilterService
{
    bool ContainsBlockedContent(string message);
    string GetWarningMessage();
}
