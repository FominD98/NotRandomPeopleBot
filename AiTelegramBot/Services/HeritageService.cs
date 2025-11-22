using System.Text.Json;
using AiTelegramBot.Models;
using Microsoft.Extensions.Logging;

namespace AiTelegramBot.Services;

public class HeritageService : IHeritageService
{
    private readonly ILogger<HeritageService> _logger;
    private List<HeritageObject> _heritageObjects = new();
    private readonly string _dataFilePath;

    public HeritageService(ILogger<HeritageService> logger)
    {
        _logger = logger;
        _dataFilePath = Path.Combine(AppContext.BaseDirectory, "Data", "heritage_objects_full.json");
        LoadHeritageObjects();
    }

    private void LoadHeritageObjects()
    {
        try
        {
            if (!File.Exists(_dataFilePath))
            {
                _logger.LogWarning("Heritage objects file not found at {Path}", _dataFilePath);
                return;
            }

            var jsonData = File.ReadAllText(_dataFilePath);
            var data = JsonSerializer.Deserialize<HeritageObjectsData>(jsonData);

            if (data?.Objects != null)
            {
                _heritageObjects = data.Objects;
                _logger.LogInformation("Loaded {Count} heritage objects", _heritageObjects.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading heritage objects from {Path}", _dataFilePath);
        }
    }

    public Task<List<HeritageObject>> GetNearbyObjectsAsync(double latitude, double longitude, double radiusKm = 2.0)
    {
        var nearbyObjects = _heritageObjects
            .Select(obj => new
            {
                Object = obj,
                Distance = CalculateDistance(latitude, longitude, obj.Latitude, obj.Longitude)
            })
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .Select(x => x.Object)
            .ToList();

        _logger.LogInformation("Found {Count} objects within {Radius}km of ({Lat}, {Lon})",
            nearbyObjects.Count, radiusKm, latitude, longitude);

        return Task.FromResult(nearbyObjects);
    }

    public Task<HeritageObject?> GetObjectByIdAsync(string id)
    {
        var obj = _heritageObjects.FirstOrDefault(o => o.Id == id);
        return Task.FromResult(obj);
    }

    public Task<List<HeritageObject>> GetAllObjectsAsync()
    {
        return Task.FromResult(_heritageObjects);
    }

    public Task<List<HeritageObject>> GetByDistrictAsync(string district)
    {
        var objects = _heritageObjects
            .Where(o => o.District.Contains(district, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(objects);
    }

    public Task<List<HeritageObject>> SearchByNameAsync(string query)
    {
        var objects = _heritageObjects
            .Where(o => o.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(objects);
    }

    public Task<List<string>> GetDistrictsAsync()
    {
        var districts = _heritageObjects
            .Select(o => o.District)
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Distinct()
            .OrderBy(d => d)
            .ToList();
        return Task.FromResult(districts);
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

    private class HeritageObjectsData
    {
        public List<HeritageObject> Objects { get; set; } = new();
    }
}
