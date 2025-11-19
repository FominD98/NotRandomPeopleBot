namespace AiTelegramBot.Models.Configuration;

public class DeepSeekSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-chat";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string ApiUrl { get; set; } = "https://api.deepseek.com/v1/chat/completions";
}
