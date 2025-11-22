using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IHeritageService
{
    Task<List<HeritageObject>> GetNearbyObjectsAsync(double latitude, double longitude, double radiusKm = 2.0);
    Task<HeritageObject?> GetObjectByIdAsync(string id);
    Task<List<HeritageObject>> GetAllObjectsAsync();
}
