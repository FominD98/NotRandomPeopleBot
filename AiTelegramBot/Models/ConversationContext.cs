namespace AiTelegramBot.Models;

public class ConversationContext
{
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string Language { get; set; } = "ru"; // "ru" for Russian, "tt" for Tatar
    public string AiProvider { get; set; } = "YandexGpt"; // "DeepSeek", "OpenAI", or "YandexGpt"
    public List<Message> Messages { get; set; } = new();
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
}

public class Message
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
