using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IYandexGeocodingService
{
    Task<LocationInfo?> GetLocationInfoAsync(double latitude, double longitude);
}
