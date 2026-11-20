using System.Text.Json;
using System.Text.Json.Serialization;
using RoadSense.Application;
using RoadSense.Domain;

namespace RoadSense.Infrastructure.Mock;

/// <summary>
/// Чете mockup данните от JSON файловете в папка MockData (копират се до изхода при build).
/// Реализира всички интерфейси за данни — по-късно се заменя с реализация върху SQL базата.
/// Редактирайте JSON файловете, за да промените какво показва фронтендът; рестарт не е нужен,
/// файловете се четат наново при промяна.
/// </summary>
public sealed class MockDataStore : IRoadNetworkRepository, ISpeedLimitRepository, IWeatherProvider, IStatsRepository, IContentRepository,
    IPlaceRepository, IIpGeolocator
{
    private readonly string _dir;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
    private readonly Dictionary<string, (DateTime stamp, object value)> _cache = new();
    private readonly object _lock = new();

    public MockDataStore(string? directory = null)
    {
        _dir = directory ?? Path.Combine(AppContext.BaseDirectory, "MockData");
        if (!Directory.Exists(_dir)) throw new DirectoryNotFoundException($"Липсва папка с mock данни: {_dir}");
    }

    private T Load<T>(string file)
    {
        var path = Path.Combine(_dir, file);
        var stamp = File.GetLastWriteTimeUtc(path);
        lock (_lock)
        {
            if (_cache.TryGetValue(file, out var c) && c.stamp == stamp) return (T)c.value;
            var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json) ?? throw new InvalidDataException(file);
            _cache[file] = (stamp, value!);
            return value;
        }
    }

    // ---- пътна мрежа ----
    public IReadOnlyList<City> GetCities() => Load<List<City>>("cities.json");
    public IReadOnlyList<RoadSegment> GetSegments() => Load<List<RoadSegment>>("segments.json");

    // ---- ограничения ----
    public int GetLimit(RoadType roadType, VehicleType vehicle) =>
        Load<List<SpeedLimit>>("speed-limits.json").First(l => l.RoadType == roadType && l.Vehicle == vehicle).LimitKmh;

    // ---- време ----
    private sealed record WeatherFile(WeatherLocation Location, double[] FogIndex, double TemperatureMean, double TemperatureAmplitude,
        int WarmestHour, double[] Precipitation, int Sunrise, int Sunset);

    public WeatherLocation GetLocation() => Load<WeatherFile>("weather.json").Location;
    public double GetFogIndex(int hourOfDay) => Load<WeatherFile>("weather.json").FogIndex[((hourOfDay % 24) + 24) % 24];

    public IReadOnlyList<WeatherHour> GetDay(DateOnly date)
    {
        var w = Load<WeatherFile>("weather.json");
        return Enumerable.Range(0, 24).Select(h =>
        {
            var fog = w.FogIndex[h];
            var vis = (int)Math.Max(40, Math.Round(10000 * Math.Exp(-5.3 * fog)));
            var temp = Math.Round(w.TemperatureMean + w.TemperatureAmplitude * Math.Cos((h - w.WarmestHour) / 24.0 * 2 * Math.PI), 1);
            var hum = (int)Math.Min(100, Math.Round(66 + 33 * fog));
            var dew = Math.Round(temp - (0.3 + 6 * (1 - fog)), 1);
            var wind = (int)Math.Round(6 + 10 * Math.Max(0, Math.Sin((h - 9) / 24.0 * 2 * Math.PI)));
            var night = h < w.Sunrise || h >= w.Sunset;
            var rain = w.Precipitation[h];
            var state = vis < 1000 ? WeatherState.Fog : rain > 0 ? WeatherState.Rain : fog > 0.15 ? WeatherState.Partly : WeatherState.Clear;
            return new WeatherHour(h, temp, dew, hum, vis, rain, wind, wind + 9, state, night);
        }).ToList();
    }

    // ---- статистика ----
    private sealed record Weights(double[] Months, double[] Hours);
    public IReadOnlyList<RoadStats> GetRoads() => Load<List<RoadStats>>("stats-roads.json");
    public IReadOnlyList<RankingRow> GetRanking() => Load<List<RankingRow>>("ranking.json");
    public IReadOnlyList<CalendarEvent> GetCalendarEvents() => Load<List<CalendarEvent>>("calendar.json");
    public IReadOnlyList<double> GetMonthWeights() => Load<Weights>("stats-weights.json").Months;
    public IReadOnlyList<double> GetHourWeights() => Load<Weights>("stats-weights.json").Hours;

    // ---- съдържание ----
    private sealed record HomeFile(List<TickerItem> Ticker, HomeContent Home);
    public IReadOnlyList<Place> GetPlaces() => Load<List<Place>>("places.json");

    // ---- местоположение по мрежата (mock: винаги отговорът от ip-location.json) ----
    private sealed record IpFile(IpGeoResult Default);
    public Task<IpGeoResult?> LocateAsync(string? ipAddress, CancellationToken ct = default) =>
        Task.FromResult<IpGeoResult?>(Load<IpFile>("ip-location.json").Default);

    public IReadOnlyList<TickerItem> GetTicker() => Load<HomeFile>("content-home.json").Ticker;
    public HomeContent GetHome() => Load<HomeFile>("content-home.json").Home;
    public FirstAidContent GetFirstAid() => Load<FirstAidContent>("content-firstaid.json");
}
