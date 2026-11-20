using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using RoadSense.Domain;

namespace RoadSense.Web.Infrastructure;

/// <summary>Прогноза за деня в точка: текущо състояние + 24 часа. Източник: Open-Meteo.</summary>
public sealed record ForecastDto(int CurrentHour, double MinTemperatureC, double MaxTemperatureC, WeatherHour Current,
    IReadOnlyList<WeatherHour> Hours, IReadOnlyList<string> Warnings, DateTimeOffset FetchedAtUtc, string Source);

/// <summary>
/// Взима от Open-Meteo (без ключ) видимост, валеж, пориви и WMO код за координатите на потребителя.
/// Отговорът се кешира 15 минути по клетка ~11 км; при грешка се връща последният запазен отговор.
/// </summary>
public sealed class OpenMeteoForecast(IHttpClientFactory httpFactory, IMemoryCache cache, ILogger<OpenMeteoForecast> log)
{
    private static readonly TimeSpan Fresh = TimeSpan.FromMinutes(15);
    private const string Fields = "temperature_2m,dew_point_2m,relative_humidity_2m,visibility,precipitation,weather_code,wind_speed_10m,wind_gusts_10m,is_day";

    public async Task<ForecastDto?> GetAsync(double lat, double lon, CancellationToken ct = default)
    {
        var key = $"om:{Math.Round(lat, 1).ToString(CultureInfo.InvariantCulture)}:{Math.Round(lon, 1).ToString(CultureInfo.InvariantCulture)}";
        if (cache.TryGetValue(key, out ForecastDto? hit) && hit is not null && DateTimeOffset.UtcNow - hit.FetchedAtUtc < Fresh) return hit;
        try
        {
            var url = "https://api.open-meteo.com/v1/forecast?latitude=" + lat.ToString("0.####", CultureInfo.InvariantCulture) +
                      "&longitude=" + lon.ToString("0.####", CultureInfo.InvariantCulture) +
                      "&current=" + Fields + "&hourly=" + Fields + "&forecast_days=1&timezone=auto&wind_speed_unit=kmh";
            await using var stream = await httpFactory.CreateClient("openmeteo").GetStreamAsync(url, ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var dto = Parse(doc.RootElement);
            cache.Set(key, dto, TimeSpan.FromHours(6)); // пазим дълго, за да има резерва при недостъпност
            return dto;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Open-Meteo не отговори; връща се последният запазен отговор (ако има).");
            return hit;
        }
    }

    private static ForecastDto Parse(JsonElement root)
    {
        var h = root.GetProperty("hourly");
        var times = h.GetProperty("time").EnumerateArray().Select(t => t.GetString()!).ToList();
        var hours = new List<WeatherHour>();
        for (var i = 0; i < times.Count; i++)
            hours.Add(Hour(int.Parse(times[i].AsSpan(11, 2)), k => h.GetProperty(k)[i]));
        var c = root.GetProperty("current");
        var curHour = int.Parse(c.GetProperty("time").GetString()!.AsSpan(11, 2));
        var current = Hour(curHour, k => c.GetProperty(k));

        var warnings = new List<string>();
        var fog = hours.Where(x => x.VisibilityM < 200).Select(x => x.Hour).ToList();
        if (fog.Count > 0) warnings.Add($"Гъста мъгла между {fog.Min():00}:00 и {fog.Max() + 1:00}:00 — видимост под 200 м.");
        if (hours.Any(x => x.TemperatureC <= 0.5 && (x.HumidityPct >= 95 || x.PrecipitationMm > 0)))
            warnings.Add("Възможна поледица по мостове и надлези.");
        return new ForecastDto(curHour, hours.Min(x => x.TemperatureC), hours.Max(x => x.TemperatureC), current, hours, warnings, DateTimeOffset.UtcNow, "Open-Meteo");
    }

    private static WeatherHour Hour(int hour, Func<string, JsonElement> get)
    {
        double D(string k) { var v = get(k); return v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0; }
        var vis = (int)Math.Round(D("visibility") is var v0 && v0 > 0 ? v0 : 10000);
        var state = State((int)D("weather_code"), vis);
        return new WeatherHour(hour, D("temperature_2m"), D("dew_point_2m"), (int)Math.Round(D("relative_humidity_2m")), vis,
            Math.Round(D("precipitation"), 1), (int)Math.Round(D("wind_speed_10m")), (int)Math.Round(D("wind_gusts_10m")), state, D("is_day") < 0.5);
    }

    // WMO код → състояние за widget-а; видимост под 200 м е мъгла независимо от кода.
    private static WeatherState State(int code, int visibilityM) => visibilityM < 200 ? WeatherState.Fog : code switch
    {
        0 => WeatherState.Clear,
        1 or 2 => WeatherState.Partly,
        3 => WeatherState.Cloudy,
        45 or 48 => WeatherState.Fog,
        >= 95 => WeatherState.Storm,
        >= 71 and <= 77 or 85 or 86 => WeatherState.Snow,
        >= 51 and <= 67 or >= 80 and <= 82 => WeatherState.Rain,
        _ => WeatherState.Cloudy
    };
}
