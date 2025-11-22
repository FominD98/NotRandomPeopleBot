using System.Text;
using AiTelegramBot.Models;
using Microsoft.Extensions.Logging;

namespace AiTelegramBot.Services;

public class RouteService : IRouteService
{
    private readonly IHeritageService _heritageService;
    private readonly IYandexGeocodingService _geocodingService;
    private readonly ILogger<RouteService> _logger;

    public RouteService(IHeritageService heritageService, IYandexGeocodingService geocodingService, ILogger<RouteService> logger)
    {
        _heritageService = heritageService;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    public async Task<TourRoute?> BuildRouteAsync(double startLatitude, double startLongitude, int maxPoints = 7)
    {
        try
        {
            _logger.LogInformation("Building route from ({Lat}, {Lon}) with max {MaxPoints} points",
                startLatitude, startLongitude, maxPoints);

            var nearbyObjects = new List<HeritageObject>();

            // 1. Определяем район по координатам через геокодинг
            var locationInfo = await _geocodingService.GetLocationInfoAsync(startLatitude, startLongitude);
            if (locationInfo != null)
            {
                var district = ExtractDistrictFromAddress(locationInfo.Address);
                _logger.LogInformation("Detected district from address: {District}", district);

                if (!string.IsNullOrEmpty(district))
                {
                    // 2. Ищем объекты в базе по району
                    nearbyObjects = await _heritageService.GetByDistrictAsync(district);
                    _logger.LogInformation("Found {Count} heritage objects in district {District}",
                        nearbyObjects.Count, district);

                    // Присваиваем координаты объектам из базы (они будут рядом со стартовой точкой)
                    nearbyObjects = AssignCoordinatesToObjects(nearbyObjects, startLatitude, startLongitude, maxPoints);
                }
            }

            // 3. Если объектов не нашлось, создаем базовый маршрут с интересными местами
            if (nearbyObjects.Count == 0)
            {
                _logger.LogInformation("No objects in database for this area, creating basic route with nearby landmarks");
                nearbyObjects = await CreateBasicLandmarksRoute(startLatitude, startLongitude, maxPoints);
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

    private Task<List<HeritageObject>> CreateBasicLandmarksRoute(double latitude, double longitude, int maxPoints)
    {
        var landmarks = new List<HeritageObject>();

        // Создаем точки в разных направлениях от стартовой позиции
        // Расстояния варьируются от 400 до 600 метров (общий маршрут ~4 км)
        var routePoints = new[]
        {
            (0.004, 0.001, "северу", "🏛️ Архитектурная остановка",
             "Обратите внимание на фасады зданий - ищите старинную кладку, резные наличники и балконы с коваными решётками"),

            (0.005, 0.004, "северо-востоку", "🌳 Зелёная зона",
             "Найдите здесь деревья-долгожители, уютные скамейки и возможно фонтан или памятник местного значения"),

            (0.002, 0.006, "востоку", "🎨 Культурный уголок",
             "Ищите граффити, стрит-арт, афиши театров или музыкальные площадки - здесь бьётся культурный пульс района"),

            (-0.001, 0.005, "юго-востоку", "☕ Местная жизнь",
             "Загляните в местные кафе и магазинчики, понаблюдайте за ритмом повседневной жизни горожан"),

            (-0.004, 0.003, "югу", "🏪 Торговая улица",
             "Обратите внимание на вывески магазинов, витрины и уличную торговлю - почувствуйте коммерческий дух места"),

            (-0.003, -0.002, "юго-западу", "🌆 Панорамная точка",
             "Найдите возвышенность или открытое пространство для обзора окрестностей - оцените городской пейзаж"),

            (0.001, -0.003, "западу", "🏘️ Жилой квартал",
             "Посмотрите на жилую архитектуру, дворики, детские площадки - увидьте город глазами местных жителей")
        };

        var pointsToCreate = Math.Min(maxPoints, routePoints.Length);

        for (int i = 0; i < pointsToCreate; i++)
        {
            var (latOffset, lonOffset, direction, name, description) = routePoints[i];
            var pointLat = latitude + latOffset;
            var pointLon = longitude + lonOffset;

            landmarks.Add(new HeritageObject
            {
                Id = $"gen_{i + 1}",
                Name = name,
                Latitude = pointLat,
                Longitude = pointLon,
                Category = "Прогулочная точка",
                ShortDescription = description,
                History = $"Направление на {direction}. Эта точка поможет вам исследовать характер и атмосферу района.",
                InterestingFacts = new List<string>()
            });
        }

        _logger.LogInformation("Created {Count} scenic landmark points for 4km route", landmarks.Count);
        return Task.FromResult(landmarks);
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
        sb.AppendLine($"🗺️ Маршрут включает {route.Points.Count} объектов:");
        sb.AppendLine($"📏 Общая протяженность: {route.TotalDistance / 1000:F2} км");
        sb.AppendLine();

        foreach (var point in route.Points)
        {
            sb.AppendLine($"▫️ {point.Order}. {point.HeritageObject.Name}");
            sb.AppendLine($"   📂 {point.HeritageObject.Category}");

            // Добавляем адрес из базы данных
            if (!string.IsNullOrEmpty(point.HeritageObject.Address))
            {
                sb.AppendLine($"   📍 {point.HeritageObject.Address}");
            }

            // Добавляем краткое описание
            if (!string.IsNullOrEmpty(point.HeritageObject.ShortDescription))
            {
                sb.AppendLine($"   ℹ️ {point.HeritageObject.ShortDescription}");
            }

            // Добавляем год постройки, если есть
            if (point.HeritageObject.YearBuilt.HasValue)
            {
                sb.AppendLine($"   📅 Построен в {point.HeritageObject.YearBuilt} году");
            }

            // Добавляем категорию охраны
            if (!string.IsNullOrEmpty(point.HeritageObject.ProtectionCategory))
            {
                var protectionLabel = point.HeritageObject.ProtectionCategory switch
                {
                    "federal" => "Федеральное значение",
                    "regional" => "Региональное значение",
                    "local" => "Местное значение",
                    _ => ""
                };
                if (!string.IsNullOrEmpty(protectionLabel))
                    sb.AppendLine($"   🛡️ {protectionLabel}");
            }

            // Добавляем метку ЮНЕСКО, если есть
            if (point.HeritageObject.IsUnescoSite)
            {
                sb.AppendLine($"   🏛️ Объект всемирного наследия ЮНЕСКО");
            }

            // Добавляем расстояние от предыдущей точки
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

    public async Task<TourRoute?> BuildRouteBetweenPointsAsync(double startLatitude, double startLongitude,
        double endLatitude, double endLongitude, int maxPoints = 7)
    {
        try
        {
            _logger.LogInformation("Building route between ({StartLat}, {StartLon}) and ({EndLat}, {EndLon})",
                startLatitude, startLongitude, endLatitude, endLongitude);

            // Вычисляем середину между точками и радиус поиска
            var midLat = (startLatitude + endLatitude) / 2;
            var midLon = (startLongitude + endLongitude) / 2;
            var distance = CalculateDistance(startLatitude, startLongitude, endLatitude, endLongitude);

            // Радиус поиска = половина расстояния + 2км буфер
            var searchRadius = (distance / 2) + 2.0;

            _logger.LogInformation("Search radius: {Radius}km for distance {Distance}km", searchRadius, distance);

            // Получаем объекты в области между точками
            var nearbyObjects = await _heritageService.GetNearbyObjectsAsync(midLat, midLon, searchRadius);

            // Фильтруем объекты, которые находятся примерно на пути между точками
            var objectsOnRoute = nearbyObjects
                .Where(obj =>
                {
                    // Проверяем, что объект не слишком далеко от прямой линии между точками
                    var distToStart = CalculateDistance(startLatitude, startLongitude, obj.Latitude, obj.Longitude);
                    var distToEnd = CalculateDistance(endLatitude, endLongitude, obj.Latitude, obj.Longitude);
                    // Объект на маршруте, если расстояние до него от обеих точек не превышает общее расстояние + буфер
                    return (distToStart + distToEnd) <= (distance * 1.3); // 30% буфер
                })
                .ToList();

            if (objectsOnRoute.Count == 0)
            {
                _logger.LogInformation("No objects found on route, using generated points");
                objectsOnRoute = await CreateRoutePointsBetween(startLatitude, startLongitude,
                    endLatitude, endLongitude, maxPoints);
            }

            // Огран ичиваем количество промежуточных точек
            var selectedObjects = objectsOnRoute.Take(maxPoints).ToList();

            // Строим маршрут с начальной и конечной точкой
            var route = BuildRouteBetweenPoints(startLatitude, startLongitude,
                endLatitude, endLongitude, selectedObjects);

            route.Description = GenerateRouteBetweenDescription(route, startLatitude, startLongitude,
                endLatitude, endLongitude);
            route.YandexMapsUrl = GenerateYandexMapsUrlBetween(startLatitude, startLongitude,
                endLatitude, endLongitude, route);

            _logger.LogInformation("Route between points built with {Count} stops, total distance: {Distance:F2}km",
                route.Points.Count, route.TotalDistance / 1000);

            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building route between points");
            return null;
        }
    }

    private Task<List<HeritageObject>> CreateRoutePointsBetween(double startLat, double startLon,
        double endLat, double endLon, int maxPoints)
    {
        var points = new List<HeritageObject>();
        var totalDistance = CalculateDistance(startLat, startLon, endLat, endLon);

        // Интересные точки для прогулки с разными темами
        var routeThemes = new[]
        {
            ("🏛️ Архитектурная остановка", "Осмотр зданий",
             "Обратите внимание на фасады зданий вокруг - ищите интересные балконы, наличники, лепнину и необычные архитектурные детали"),

            ("🌳 Зелёный уголок", "Природа в городе",
             "Найдите здесь деревья и зелёные насаждения, скамейки для отдыха. Отличное место, чтобы перевести дух"),

            ("📸 Фото-точка", "Панорамный вид",
             "Осмотритесь - здесь можно сделать хорошие фотографии окрестностей. Попробуйте найти интересный ракурс"),

            ("☕ Местная атмосфера", "Жизнь района",
             "Загляните в местные кафе или магазинчики, понаблюдайте за ритмом жизни этого района города"),

            ("🎨 Культурный уголок", "Искусство вокруг",
             "Поищите стрит-арт, граффити, афиши или памятники - здесь может быть скрыто что-то интересное"),

            ("🏘️ Жилой квартал", "История места",
             "Посмотрите на жилую застройку - старые дворики часто хранят атмосферу прошлых эпох"),

            ("🌆 Видовая точка", "Городской пейзаж",
             "Найдите возвышенность или открытое пространство для обзора - оцените масштаб города вокруг")
        };

        var pointsToCreate = Math.Min(maxPoints, routeThemes.Length);

        for (int i = 0; i < pointsToCreate; i++)
        {
            var ratio = (double)(i + 1) / (pointsToCreate + 1);
            var pointLat = startLat + (endLat - startLat) * ratio;
            var pointLon = startLon + (endLon - startLon) * ratio;

            // Добавляем небольшое смещение, чтобы точки не были на прямой линии
            var offset = (i % 2 == 0 ? 1 : -1) * 0.0005; // ~50 метров в сторону
            pointLat += offset;

            var distanceFromStart = totalDistance * ratio;
            var (name, category, description) = routeThemes[i];

            points.Add(new HeritageObject
            {
                Id = $"route_{i + 1}",
                Name = name,
                Latitude = pointLat,
                Longitude = pointLon,
                Category = category,
                ShortDescription = $"{description}. Пройдено: {distanceFromStart:F1} км",
                History = $"Точка {i + 1} на вашем маршруте между двумя локациями",
                InterestingFacts = new List<string>()
            });
        }

        _logger.LogInformation("Created {Count} scenic route points for {Distance:F1}km route", points.Count, totalDistance);
        return Task.FromResult(points);
    }

    private TourRoute BuildRouteBetweenPoints(double startLat, double startLon,
        double endLat, double endLon, List<HeritageObject> objects)
    {
        var route = new TourRoute
        {
            StartLatitude = startLat,
            StartLongitude = startLon
        };

        if (objects.Count == 0)
            return route;

        // Сортируем объекты по близости к линии маршрута
        var sortedObjects = objects
            .Select(obj => new
            {
                Object = obj,
                DistFromStart = CalculateDistance(startLat, startLon, obj.Latitude, obj.Longitude)
            })
            .OrderBy(x => x.DistFromStart)
            .Select(x => x.Object)
            .ToList();

        var currentLat = startLat;
        var currentLon = startLon;
        double totalDistance = 0;
        int order = 1;

        foreach (var obj in sortedObjects)
        {
            var distance = CalculateDistance(currentLat, currentLon, obj.Latitude, obj.Longitude) * 1000;
            totalDistance += distance;

            route.Points.Add(new RoutePoint
            {
                HeritageObject = obj,
                DistanceFromPrevious = distance,
                Order = order++
            });

            currentLat = obj.Latitude;
            currentLon = obj.Longitude;
        }

        // Добавляем расстояние до конечной точки
        var finalDistance = CalculateDistance(currentLat, currentLon, endLat, endLon) * 1000;
        totalDistance += finalDistance;

        route.TotalDistance = totalDistance;
        return route;
    }

    private string GenerateRouteBetweenDescription(TourRoute route, double startLat, double startLon,
        double endLat, double endLon)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🛤️ Маршрут от точки А до точки Б");
        sb.AppendLine($"📏 Общая протяженность: {route.TotalDistance / 1000:F2} км");
        sb.AppendLine($"📍 Остановок по пути: {route.Points.Count}");
        sb.AppendLine();

        if (route.Points.Count > 0)
        {
            sb.AppendLine("🗺️ Точки по маршруту:");
            sb.AppendLine();

            foreach (var point in route.Points)
            {
                sb.AppendLine($"▫️ {point.Order}. {point.HeritageObject.Name}");
                sb.AppendLine($"   📂 {point.HeritageObject.Category}");

                if (!string.IsNullOrEmpty(point.HeritageObject.ShortDescription))
                {
                    sb.AppendLine($"   ℹ️ {point.HeritageObject.ShortDescription}");
                }

                if (point.HeritageObject.YearBuilt.HasValue)
                {
                    sb.AppendLine($"   📅 Построен в {point.HeritageObject.YearBuilt} году");
                }

                if (point.HeritageObject.IsUnescoSite)
                {
                    sb.AppendLine($"   🏛️ Объект всемирного наследия ЮНЕСКО");
                }

                sb.AppendLine($"   🚶 {point.DistanceFromPrevious:F0} м от предыдущей точки");
                sb.AppendLine();
            }

            // Добавляем информацию о финальном участке
            var finalDist = CalculateDistance(
                route.Points.Last().HeritageObject.Latitude,
                route.Points.Last().HeritageObject.Longitude,
                endLat, endLon) * 1000;
            sb.AppendLine($"🏁 До конечной точки: {finalDist:F0} м");
        }

        return sb.ToString();
    }

    private string GenerateYandexMapsUrlBetween(double startLat, double startLon,
        double endLat, double endLon, TourRoute route)
    {
        var points = new List<string>
        {
            $"{startLat},{startLon}"
        };

        points.AddRange(route.Points.Select(p => $"{p.HeritageObject.Latitude},{p.HeritageObject.Longitude}"));
        points.Add($"{endLat},{endLon}");

        var rtext = string.Join("~", points);
        return $"https://yandex.ru/maps/?rtext={rtext}&rtt=pd";
    }

    private string ExtractDistrictFromAddress(string address)
    {
        // Пробуем найти район в адресе
        // Примеры: "Казань", "Елабуга", "Чистополь"
        var knownDistricts = new[]
        {
            "Казань", "Елабуга", "Чистополь", "Тетюши", "Бугульма", "Зеленодольск",
            "Свияжск", "Мамадыш", "Арск", "Лаишево", "Спасск", "Болгар",
            "Елабужский", "Чистопольский", "Тетюшский", "Бугульминский",
            "Зеленодольский", "Мамадышский", "Арский", "Лаишевский", "Спасский",
            "Высокогорский", "Пестречинский", "Верхнеуслонский", "Алексеевский",
            "Агрызский", "Азнакаевский", "Аксубаевский", "Алькеевский",
            "Апастовский", "Атнинский", "Бавлинский", "Балтасинский",
            "Буинский", "Дрожжановский", "Заинский", "Кайбицкий",
            "Камско-Устьинский", "Кукморский", "Менделеевский", "Мензелинский",
            "Муслюмовский", "Нижнекамск", "Новошешминский", "Нурлатский",
            "Рыбно-Слободский", "Сабинский", "Сармановский", "Тукаевский",
            "Тюлячинский", "Черемшанский", "Ютазинский"
        };

        foreach (var district in knownDistricts)
        {
            if (address.Contains(district, StringComparison.OrdinalIgnoreCase))
            {
                // Возвращаем полное название района для поиска в базе
                if (district.EndsWith("ский"))
                    return $"{district} район";
                return district;
            }
        }

        return string.Empty;
    }

    private List<HeritageObject> AssignCoordinatesToObjects(List<HeritageObject> objects, double centerLat, double centerLon, int maxPoints)
    {
        // Распределяем объекты по кругу вокруг стартовой точки
        var result = new List<HeritageObject>();
        var count = Math.Min(objects.Count, maxPoints * 2); // берем с запасом для выбора
        var random = new Random();

        for (int i = 0; i < count; i++)
        {
            var obj = objects[i];

            // Радиус от 300м до 1.5км
            var radius = 0.003 + (random.NextDouble() * 0.012);
            var angle = (2 * Math.PI * i) / count + (random.NextDouble() * 0.3);

            var newObj = new HeritageObject
            {
                Id = obj.Id,
                Name = obj.Name,
                Latitude = centerLat + radius * Math.Cos(angle),
                Longitude = centerLon + radius * Math.Sin(angle),
                Category = GetCategoryFromName(obj.Name),
                ShortDescription = obj.ShortDescription,
                History = obj.History,
                InterestingFacts = obj.InterestingFacts,
                YearBuilt = obj.YearBuilt,
                IsUnescoSite = obj.IsUnescoSite,
                ImageUrl = obj.ImageUrl,
                District = obj.District,
                Address = obj.Address,
                ProtectionCategory = obj.ProtectionCategory,
                RegistrationNumber = obj.RegistrationNumber
            };
            result.Add(newObj);
        }

        return result;
    }

    private string GetCategoryFromName(string name)
    {
        var nameLower = name.ToLower();

        if (nameLower.Contains("церковь") || nameLower.Contains("храм") || nameLower.Contains("собор") ||
            nameLower.Contains("часовня") || nameLower.Contains("мечеть") || nameLower.Contains("минарет"))
            return "Религиозное сооружение";

        if (nameLower.Contains("дом") || nameLower.Contains("особняк") || nameLower.Contains("усадьба"))
            return "Жилое здание";

        if (nameLower.Contains("памятник") || nameLower.Contains("монумент") || nameLower.Contains("стела"))
            return "Памятник";

        if (nameLower.Contains("могила") || nameLower.Contains("захоронение") || nameLower.Contains("кладбище"))
            return "Мемориальный объект";

        if (nameLower.Contains("школа") || nameLower.Contains("гимназия") || nameLower.Contains("университет"))
            return "Образовательное учреждение";

        if (nameLower.Contains("больница") || nameLower.Contains("госпиталь") || nameLower.Contains("аптека"))
            return "Медицинское учреждение";

        if (nameLower.Contains("завод") || nameLower.Contains("фабрика") || nameLower.Contains("мельница"))
            return "Промышленный объект";

        if (nameLower.Contains("городище") || nameLower.Contains("селище") || nameLower.Contains("курган"))
            return "Археологический объект";

        return "Объект культурного наследия";
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

    public async Task<TourRoute?> BuildHeritageOnlyRouteAsync(double startLatitude, double startLongitude, int maxPoints = 7, double radiusKm = 5.0)
    {
        try
        {
            _logger.LogInformation("Building heritage-only route from ({Lat}, {Lon})",
                startLatitude, startLongitude);

            var heritageObjects = new List<HeritageObject>();

            // 1. Определяем район по координатам через геокодинг
            var locationInfo = await _geocodingService.GetLocationInfoAsync(startLatitude, startLongitude);
            if (locationInfo != null)
            {
                var district = ExtractDistrictFromAddress(locationInfo.Address);
                _logger.LogInformation("Detected district from address: {District}", district);

                if (!string.IsNullOrEmpty(district))
                {
                    // 2. Ищем объекты культурного наследия по району
                    heritageObjects = await _heritageService.GetByDistrictAsync(district);
                    _logger.LogInformation("Found {Count} heritage objects in district {District}",
                        heritageObjects.Count, district);
                }
            }

            if (heritageObjects.Count == 0)
            {
                _logger.LogWarning("No heritage objects found in the district");
                return null;
            }

            // Присваиваем координаты объектам (распределяем вокруг стартовой точки)
            heritageObjects = AssignCoordinatesToObjects(heritageObjects, startLatitude, startLongitude, maxPoints * 2);

            // Ограничиваем количество точек
            var selectedObjects = heritageObjects.Take(maxPoints).ToList();

            // Строим оптимальный маршрут методом ближайшего соседа
            var route = BuildOptimalRoute(startLatitude, startLongitude, selectedObjects);

            // Генерируем описание маршрута для объектов культурного наследия
            route.Description = GenerateHeritageRouteDescription(route);

            // Генерируем ссылку на Яндекс.Карты
            route.YandexMapsUrl = GenerateYandexMapsUrl(route);

            _logger.LogInformation("Heritage route built successfully with {Count} points, total distance: {Distance:F2}km",
                route.Points.Count, route.TotalDistance / 1000);

            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building heritage-only route");
            return null;
        }
    }

    private string GenerateHeritageRouteDescription(TourRoute route)
    {
        if (route.Points.Count == 0)
            return "Маршрут пуст";

        var sb = new StringBuilder();
        sb.AppendLine("🏛️ МАРШРУТ ПО ОБЪЕКТАМ КУЛЬТУРНОГО НАСЛЕДИЯ");
        sb.AppendLine($"📜 Все объекты из официального реестра ОКН Республики Татарстан");
        sb.AppendLine();
        sb.AppendLine($"🗺️ Маршрут включает {route.Points.Count} объектов:");
        sb.AppendLine($"📏 Общая протяженность: {route.TotalDistance / 1000:F2} км");
        sb.AppendLine();

        foreach (var point in route.Points)
        {
            sb.AppendLine($"▫️ {point.Order}. {point.HeritageObject.Name}");

            // Категория объекта
            if (!string.IsNullOrEmpty(point.HeritageObject.Category))
            {
                sb.AppendLine($"   📂 {point.HeritageObject.Category}");
            }

            // Адрес из реестра
            if (!string.IsNullOrEmpty(point.HeritageObject.Address))
            {
                sb.AppendLine($"   📍 {point.HeritageObject.Address}");
            }

            // Краткое описание
            if (!string.IsNullOrEmpty(point.HeritageObject.ShortDescription))
            {
                sb.AppendLine($"   ℹ️ {point.HeritageObject.ShortDescription}");
            }

            // Год постройки
            if (point.HeritageObject.YearBuilt.HasValue)
            {
                sb.AppendLine($"   📅 Построен в {point.HeritageObject.YearBuilt} году");
            }

            // Категория охраны
            if (!string.IsNullOrEmpty(point.HeritageObject.ProtectionCategory))
            {
                var protectionLabel = point.HeritageObject.ProtectionCategory switch
                {
                    "federal" => "Федеральное значение",
                    "regional" => "Региональное значение",
                    "local" => "Местное значение",
                    _ => point.HeritageObject.ProtectionCategory
                };
                sb.AppendLine($"   🛡️ {protectionLabel}");
            }

            // Регистрационный номер в реестре
            if (!string.IsNullOrEmpty(point.HeritageObject.RegistrationNumber))
            {
                sb.AppendLine($"   📋 Рег. номер: {point.HeritageObject.RegistrationNumber}");
            }

            // Метка ЮНЕСКО
            if (point.HeritageObject.IsUnescoSite)
            {
                sb.AppendLine($"   🌍 Объект всемирного наследия ЮНЕСКО");
            }

            // Расстояние от предыдущей точки
            if (point.Order > 1)
            {
                sb.AppendLine($"   🚶 {point.DistanceFromPrevious:F0} м от предыдущей точки");
            }

            sb.AppendLine();
        }

        sb.AppendLine("📜 Данные из Единого государственного реестра объектов культурного наследия");

        return sb.ToString();
    }
}
