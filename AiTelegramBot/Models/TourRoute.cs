namespace AiTelegramBot.Models;

public class TourRoute
{
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public List<RoutePoint> Points { get; set; } = new();
    public double TotalDistance { get; set; } // в метрах
    public string Description { get; set; } = string.Empty;
    public string YandexMapsUrl { get; set; } = string.Empty;
}
