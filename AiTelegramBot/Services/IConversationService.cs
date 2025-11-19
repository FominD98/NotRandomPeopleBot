using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IConversationService
{
    ConversationContext GetOrCreateContext(long userId, string? userName = null);
    void AddMessage(long userId, string role, string content);
    void ResetContext(long userId);
    void CleanupOldContexts(TimeSpan maxAge);
    void SetUserLanguage(long userId, string languageCode);
    string GetUserLanguage(long userId);
    void SetUserAiProvider(long userId, string provider);
    string GetUserAiProvider(long userId);
}
