using AiTelegramBot.Models;

namespace AiTelegramBot.Services;

public interface IHeritageService
{
    Task<List<HeritageObject>> GetNearbyObjectsAsync(double latitude, double longitude, double radiusKm = 2.0);
    Task<HeritageObject?> GetObjectByIdAsync(string id);
    Task<List<HeritageObject>> GetAllObjectsAsync();
    Task<List<HeritageObject>> GetByDistrictAsync(string district);
    Task<List<HeritageObject>> SearchByNameAsync(string query);
    Task<List<string>> GetDistrictsAsync();
}
