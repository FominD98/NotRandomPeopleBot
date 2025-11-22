namespace AiTelegramBot.Models;

public class TourGuideResult
{
    public string Text { get; set; } = string.Empty;
    public byte[]? AudioData { get; set; }
    public LocationInfo? Location { get; set; }
}
