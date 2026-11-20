using System.Globalization;
using System.Text.Json;

namespace RoadSense.Web.Infrastructure;

/// <summary>Последно измерване от метеостанция на bgtoll.bg (отворени данни, обновява се на 15 минути).</summary>
public sealed record StationObservation(int StationId, string Name, string? NameEn, double Latitude, double Longitude,
    double? AirC, double? SurfaceC, double? HumidityPct, double? PressureHpa, double? WindSpeed, double? WindDirDeg, DateTimeOffset? MeasuredAtUtc);

/// <summary>State/IsNight са оценка: станциите не мерят валежи и видимост, затова състоянието се извежда от влажността и температурата.</summary>
public sealed record LiveWeather(StationObservation Station, double DistanceKm, DateTimeOffset FetchedAtUtc, RoadSense.Domain.WeatherState State, bool IsNight)
{
    /// <summary>Входът за SpeedAdvisor. Видимост и валеж не се мерят: приема се 10 км видимост и без валеж.</summary>
    public RoadSense.Domain.WeatherHour ToWeatherHour()
    {
        var s = Station;
        var wind = (int)Math.Round(s.WindSpeed ?? 0);
        return new RoadSense.Domain.WeatherHour(0, s.AirC ?? 10, (s.AirC ?? 10) - (100 - (s.HumidityPct ?? 50)) / 5, (int)Math.Round(s.HumidityPct ?? 50),
            State == RoadSense.Domain.WeatherState.Fog ? 150 : 10000, 0, wind, wind, State, IsNight);
    }
}

/// <summary>
/// Тегли на всеки 15 минути JSON файла с метеоданните (BgToll:WeatherUrl) и пази последния снимък в паметта.
/// Времето на измерване във файла е UTC.
/// </summary>
public sealed class BgTollWeatherFeed(IHttpClientFactory httpFactory, IConfiguration config, ILogger<BgTollWeatherFeed> log) : BackgroundService
{
    private readonly HttpClient http = httpFactory.CreateClient("bgtoll");
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private volatile Snapshot? _snap;
    private sealed record Snapshot(IReadOnlyList<StationObservation> Stations, DateTimeOffset FetchedAtUtc);

    public bool IsConfigured => !string.IsNullOrWhiteSpace(config["BgToll:WeatherUrl"]);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!IsConfigured) { log.LogWarning("BgToll:WeatherUrl не е зададен — живите метеоданни са изключени."); return; }
        using var timer = new PeriodicTimer(Interval);
        do { await RefreshAsync(ct); }
        while (await timer.WaitForNextTickAsync(ct));
    }

    private async Task RefreshAsync(CancellationToken ct)
    {
        try
        {
            var url = config["BgToll:WeatherUrl"]!;
            // Локален файл (за разработка/тест) или http(s) адрес на JSON файла.
            var path = Path.IsPathRooted(url) ? url : Path.Combine(AppContext.BaseDirectory, url);
            await using Stream stream = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? await http.GetStreamAsync(url, ct)
                : File.OpenRead(File.Exists(path) ? path : Path.Combine(Directory.GetCurrentDirectory(), url));
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var list = Parse(doc.RootElement);
            if (list.Count > 0) _snap = new Snapshot(list, DateTimeOffset.UtcNow);
            log.LogInformation("bgtoll метеоданни: {Count} станции.", list.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Неуспешно изтегляне на метеоданните от bgtoll.bg; пази се предишният снимък.");
        }
    }

    /// <summary>Най-близката станция до координатите или null, ако още няма данни.</summary>
    public LiveWeather? Nearest(double lat, double lon, int localHour)
    {
        var s = _snap;
        if (s is null) return null;
        // Измервания, по-стари от 2 часа, не са „текущи“ (напр. спрял източник или архивен файл) — не ги показваме.
        var cutoff = DateTimeOffset.UtcNow.AddHours(-2);
        var best = s.Stations.Where(x => x.AirC is not null && (x.MeasuredAtUtc is null || x.MeasuredAtUtc >= cutoff)).MinBy(x => Km(lat, lon, x.Latitude, x.Longitude));
        if (best is null) return null;
        var h = best.HumidityPct ?? 0;
        var state = h >= 98 ? RoadSense.Domain.WeatherState.Fog
            : best.AirC <= 0 && h >= 90 ? RoadSense.Domain.WeatherState.Snow
            : h >= 90 ? RoadSense.Domain.WeatherState.Cloudy
            : h >= 65 ? RoadSense.Domain.WeatherState.Partly
            : RoadSense.Domain.WeatherState.Clear;
        return new LiveWeather(best, Math.Round(Km(lat, lon, best.Latitude, best.Longitude), 1), s.FetchedAtUtc, state, localHour is >= 20 or < 6);
    }

    // Приема масив от станции или обект, който съдържа такъв масив.
    private static List<StationObservation> Parse(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
            root = root.EnumerateObject().Select(p => p.Value).FirstOrDefault(v => v.ValueKind == JsonValueKind.Array);
        var result = new List<StationObservation>();
        if (root.ValueKind != JsonValueKind.Array) return result;
        foreach (var e in root.EnumerateArray())
        {
            if (e.ValueKind != JsonValueKind.Object) continue;
            var lat = Num(e, "lat"); var lon = Num(e, "lon");
            if (lat is null || lon is null) continue;
            DateTimeOffset? at = e.TryGetProperty("time", out var t) && t.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(t.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt)
                ? new DateTimeOffset(dt, TimeSpan.Zero) : null;
            result.Add(new StationObservation((int)(Num(e, "station_id") ?? 0), e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                e.TryGetProperty("nameENG", out var ne) ? ne.GetString() : null, lat.Value, lon.Value,
                Num(e, "air"), Num(e, "surface"), Num(e, "humidity"), Num(e, "pressure"), Num(e, "wind"), Num(e, "wind_dir"), at));
        }
        return result;
    }

    private static double? Num(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && double.IsFinite(d) ? d : null;

    private static double Km(double la1, double lo1, double la2, double lo2)
    {
        const double R = 6371, Rad = Math.PI / 180;
        var dLa = (la2 - la1) * Rad; var dLo = (lo2 - lo1) * Rad;
        var a = Math.Sin(dLa / 2) * Math.Sin(dLa / 2) + Math.Cos(la1 * Rad) * Math.Cos(la2 * Rad) * Math.Sin(dLo / 2) * Math.Sin(dLo / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }
}
