namespace AiTelegramBot.Models.Configuration;

public class OpenAISettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
}
