namespace AiTelegramBot.Models;

public class UserRouteState
{
    public long UserId { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }
    public string Mode { get; set; } = "route"; // "route" or "route_between"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
