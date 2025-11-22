namespace AiTelegramBot.Models;

public class HeritageObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string History { get; set; } = string.Empty;
    public List<string> InterestingFacts { get; set; } = new();
    public int? YearBuilt { get; set; }
    public bool IsUnescoSite { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string ProtectionCategory { get; set; } = string.Empty; // federal, regional, local
    public string RegistrationNumber { get; set; } = string.Empty;
}
