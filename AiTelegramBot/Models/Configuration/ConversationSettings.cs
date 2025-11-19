namespace AiTelegramBot.Models.Configuration;

public class ConversationSettings
{
    public int MaxHistoryMessages { get; set; } = 10;
    public string SystemPrompt { get; set; } = string.Empty;
}
