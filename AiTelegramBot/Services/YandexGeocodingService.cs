using System.Text.Json;
using AiTelegramBot.Models;
using AiTelegramBot.Models.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AiTelegramBot.Services;

public class YandexGeocodingService : IYandexGeocodingService
{
    private readonly HttpClient _httpClient;
    private readonly YandexGeocodingSettings _settings;
    private readonly ILogger<YandexGeocodingService> _logger;

    public YandexGeocodingService(
        HttpClient httpClient,
        IOptions<YandexGeocodingSettings> settings,
        ILogger<YandexGeocodingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<LocationInfo?> GetLocationInfoAsync(double latitude, double longitude)
    {
        try
        {
            var geocode = $"{longitude},{latitude}";
            var url = $"{_settings.ApiUrl}?apikey={_settings.ApiKey}&geocode={geocode}&format=json&lang=ru_RU";

            _logger.LogInformation("Requesting geocoding for coordinates: {Latitude}, {Longitude}", latitude, longitude);

            var response = await _httpClient.GetAsync(url);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Yandex Geocoding API error: {StatusCode}, {Response}",
                    response.StatusCode, responseString);
                return null;
            }

            using var doc = JsonDocument.Parse(responseString);
            var root = doc.RootElement;

            if (root.TryGetProperty("response", out var responseElement) &&
                responseElement.TryGetProperty("GeoObjectCollection", out var collection) &&
                collection.TryGetProperty("featureMember", out var featureMembers) &&
                featureMembers.GetArrayLength() > 0)
            {
                var firstFeature = featureMembers[0];
                if (firstFeature.TryGetProperty("GeoObject", out var geoObject))
                {
                    var locationInfo = new LocationInfo
                    {
                        Latitude = latitude,
                        Longitude = longitude
                    };

                    if (geoObject.TryGetProperty("metaDataProperty", out var metaData) &&
                        metaData.TryGetProperty("GeocoderMetaData", out var geocoderMeta) &&
                        geocoderMeta.TryGetProperty("text", out var textElement))
                    {
                        locationInfo.Address = textElement.GetString() ?? string.Empty;
                    }

                    if (geoObject.TryGetProperty("metaDataProperty", out var meta) &&
                        meta.TryGetProperty("GeocoderMetaData", out var geocoderMetaData) &&
                        geocoderMetaData.TryGetProperty("Address", out var address))
                    {
                        if (address.TryGetProperty("Components", out var components))
                        {
                            foreach (var component in components.EnumerateArray())
                            {
                                if (component.TryGetProperty("kind", out var kind) &&
                                    component.TryGetProperty("name", out var name))
                                {
                                    var kindStr = kind.GetString();
                                    var nameStr = name.GetString() ?? string.Empty;

                                    switch (kindStr)
                                    {
                                        case "locality":
                                            locationInfo.City = nameStr;
                                            break;
                                        case "street":
                                            locationInfo.Street = nameStr;
                                            break;
                                        case "district":
                                            locationInfo.District = nameStr;
                                            break;
                                    }
                                }
                            }
                        }
                    }

                    _logger.LogInformation("Successfully geocoded location: {Address}", locationInfo.Address);
                    return locationInfo;
                }
            }

            _logger.LogWarning("No geocoding results found for coordinates: {Latitude}, {Longitude}", latitude, longitude);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Yandex Geocoding API");
            return null;
        }
    }
}
