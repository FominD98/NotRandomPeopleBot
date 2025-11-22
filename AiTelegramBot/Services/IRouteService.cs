using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IRouteService
{
    Task<TourRoute?> BuildRouteAsync(double startLatitude, double startLongitude, int maxPoints = 7);
    Task<TourRoute?> BuildRouteBetweenPointsAsync(double startLatitude, double startLongitude,
        double endLatitude, double endLongitude, int maxPoints = 7);
    Task<TourRoute?> BuildHeritageOnlyRouteAsync(double startLatitude, double startLongitude, int maxPoints = 7, double radiusKm = 5.0);
}
