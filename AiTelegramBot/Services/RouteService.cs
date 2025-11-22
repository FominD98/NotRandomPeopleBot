using System.Text;
using AiTelegramBot.Models;
using Microsoft.Extensions.Logging;

namespace AiTelegramBot.Services;

public class RouteService : IRouteService
{
    private readonly IHeritageService _heritageService;
    private readonly ILogger<RouteService> _logger;

    public RouteService(IHeritageService heritageService, ILogger<RouteService> logger)
    {
        _heritageService = heritageService;
        _logger = logger;
    }

    public async Task<TourRoute?> BuildRouteAsync(double startLatitude, double startLongitude, int maxPoints = 5)
    {
        try
        {
            _logger.LogInformation("Building route from ({Lat}, {Lon}) with max {MaxPoints} points",
                startLatitude, startLongitude, maxPoints);

            // Получаем ближайшие объекты в радиусе 2 км
            var nearbyObjects = await _heritageService.GetNearbyObjectsAsync(startLatitude, startLongitude, 2.0);

            if (nearbyObjects.Count == 0)
            {
                _logger.LogWarning("No nearby objects found");
                return null;
            }

            // Ограничиваем количество точек
            var selectedObjects = nearbyObjects.Take(maxPoints).ToList();

            // Строим оптимальный маршрут методом ближайшего соседа
            var route = BuildOptimalRoute(startLatitude, startLongitude, selectedObjects);

            // Генерируем описание маршрута
            route.Description = GenerateRouteDescription(route);

            // Генерируем ссылку на Яндекс.Карты
            route.YandexMapsUrl = GenerateYandexMapsUrl(route);

            _logger.LogInformation("Route built successfully with {Count} points, total distance: {Distance:F2}km",
                route.Points.Count, route.TotalDistance / 1000);

            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building route");
            return null;
        }
    }

    private TourRoute BuildOptimalRoute(double startLat, double startLon, List<HeritageObject> objects)
    {
        var route = new TourRoute
        {
            StartLatitude = startLat,
            StartLongitude = startLon
        };

        if (objects.Count == 0)
            return route;

        var unvisited = new List<HeritageObject>(objects);
        var currentLat = startLat;
        var currentLon = startLon;
        var order = 1;
        double totalDistance = 0;

        // Жадный алгоритм ближайшего соседа
        while (unvisited.Count > 0)
        {
            var nearest = unvisited
                .Select(obj => new
                {
                    Object = obj,
                    Distance = CalculateDistance(currentLat, currentLon, obj.Latitude, obj.Longitude)
                })
                .OrderBy(x => x.Distance)
                .First();

            var distanceMeters = nearest.Distance * 1000; // конвертируем в метры
            totalDistance += distanceMeters;

            route.Points.Add(new RoutePoint
            {
                HeritageObject = nearest.Object,
                DistanceFromPrevious = distanceMeters,
                Order = order++
            });

            currentLat = nearest.Object.Latitude;
            currentLon = nearest.Object.Longitude;
            unvisited.Remove(nearest.Object);
        }

        route.TotalDistance = totalDistance;
        return route;
    }

    private string GenerateRouteDescription(TourRoute route)
    {
        if (route.Points.Count == 0)
            return "Маршрут пуст";

        var sb = new StringBuilder();
        sb.AppendLine($"📍 Маршрут включает {route.Points.Count} объектов:");
        sb.AppendLine($"📏 Общая протяженность: {route.TotalDistance / 1000:F2} км");
        sb.AppendLine();

        foreach (var point in route.Points)
        {
            sb.AppendLine($"{point.Order}. {point.HeritageObject.Name}");
            sb.AppendLine($"   📂 {point.HeritageObject.Category}");
            if (point.Order > 1)
            {
                sb.AppendLine($"   🚶 {point.DistanceFromPrevious:F0} м от предыдущей точки");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string GenerateYandexMapsUrl(TourRoute route)
    {
        if (route.Points.Count == 0)
            return string.Empty;

        // Формируем URL для Яндекс.Карт с маршрутом
        // Формат: https://yandex.ru/maps/?rtext=lat1,lon1~lat2,lon2~lat3,lon3&rtt=pd

        var points = new List<string>
        {
            $"{route.StartLatitude},{route.StartLongitude}"
        };

        points.AddRange(route.Points.Select(p => $"{p.HeritageObject.Latitude},{p.HeritageObject.Longitude}"));

        var rtext = string.Join("~", points);
        return $"https://yandex.ru/maps/?rtext={rtext}&rtt=pd";
    }

    // Haversine formula для расчета расстояния между двумя точками на Земле
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        lat1 = DegreesToRadians(lat1);
        lat2 = DegreesToRadians(lat2);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
