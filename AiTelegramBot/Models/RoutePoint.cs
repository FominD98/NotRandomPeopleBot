namespace AiTelegramBot.Models;

public class RoutePoint
{
    public HeritageObject HeritageObject { get; set; } = null!;
    public double DistanceFromPrevious { get; set; } // в метрах
    public int Order { get; set; }
}
