namespace AiTelegramBot.Models.Configuration;

public class YandexGptSettings
{
    public string OAuthToken { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
    public string Model { get; set; } = "yandexgpt/latest";
    public double Temperature { get; set; } = 0.6;
    public int MaxTokens { get; set; } = 1000;
    public string ApiUrl { get; set; } = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";
    public string IamTokenUrl { get; set; } = "https://iam.api.cloud.yandex.net/iam/v1/tokens";
}
