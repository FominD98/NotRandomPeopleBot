namespace AiTelegramBot.Models.Configuration;

public class LoggingSettings
{
    public string ServiceName { get; set; } = "aiTelegramBot";
    public string DeploymentEnvironment { get; set; } = "production";
    public string GrafanaCloudAccessToken { get; set; } = string.Empty;
    public string LokiEndpoint { get; set; } = string.Empty;
}
