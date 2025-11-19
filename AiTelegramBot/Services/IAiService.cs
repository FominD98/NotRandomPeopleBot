using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IAiService
{
    Task<string> GetResponseAsync(string userMessage, ConversationContext context);
}
