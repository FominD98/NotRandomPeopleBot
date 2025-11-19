namespace AiTelegramBot.Models.Configuration;

public class ContentFilterSettings
{
    public bool EnableFiltering { get; set; } = true;
    public List<string> BlockedWords { get; set; } = new();
    public bool WarnOnDetection { get; set; } = true;
}
