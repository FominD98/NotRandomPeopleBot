using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IRouteService
{
    Task<TourRoute?> BuildRouteAsync(double startLatitude, double startLongitude, int maxPoints = 5);
}
