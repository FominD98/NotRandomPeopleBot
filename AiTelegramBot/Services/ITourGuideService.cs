using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface ITourGuideService
{
    Task<TourGuideResult?> GenerateTourAsync(double latitude, double longitude);
}
