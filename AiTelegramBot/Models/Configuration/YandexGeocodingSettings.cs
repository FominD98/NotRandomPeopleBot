namespace AiTelegramBot.Models.Configuration;

public class YandexGeocodingSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://geocode-maps.yandex.ru/1.x/";
}
