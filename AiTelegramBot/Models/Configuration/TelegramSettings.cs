namespace AiTelegramBot.Models.Configuration;

public class TelegramSettings
{
    public string BotToken { get; set; } = string.Empty;
    public List<long> AllowedUsers { get; set; } = new();
}
