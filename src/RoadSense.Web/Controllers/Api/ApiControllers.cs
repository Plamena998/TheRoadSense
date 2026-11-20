using Microsoft.AspNetCore.Mvc;
using RoadSense.Application;
using RoadSense.Application.Services;
using RoadSense.Domain;
using RoadSense.Web.Infrastructure;

namespace RoadSense.Web.Controllers.Api;

// JSON API, което ползва фронтендът. Едно и също е за mock и за реалните данни.

[ApiController]
[Route("api")]
public class RoadApiController(RouteRiskService routes) : ControllerBase
{
    /// <summary>Възлите и участъците на пътната мрежа (за картата).</summary>
    [HttpGet("network")]
    public RoadNetworkDto Network() => routes.GetNetwork();

    /// <summary>Оценка на риска за маршрут. Пример: /api/route?from=SOF&amp;to=BGS&amp;hour=23&amp;vehicle=truck</summary>
    [HttpGet("route")]
    public ActionResult<RouteResult> Route(string from = "SOF", string to = "BGS", int hour = 6, VehicleType vehicle = VehicleType.Car)
    {
        try { return routes.Evaluate(from, to, hour, vehicle); }
        catch (ArgumentException ex) { return Problem(ex.Message, statusCode: 400); }
    }
}

[ApiController]
[Route("api/location")]
public class LocationApiController(LocationService locations) : ControllerBase
{
    /// <summary>Най-близкото място по GPS координати от браузъра. Пример: /api/location/gps?lat=42.69&amp;lon=23.32&amp;accuracy=40</summary>
    [HttpGet("gps")]
    public ActionResult<LocationResult> Gps(double lat, double lon, double? accuracy)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180) return Problem("Невалидни координати.", statusCode: 400);
        return locations.FromCoordinates(lat, lon, accuracy);
    }

    /// <summary>Приблизително място по IP адреса на мрежата (Wi-Fi / мобилни данни), когато няма GPS.</summary>
    [HttpGet("network")]
    public Task<LocationResult> Network(CancellationToken ct)
    {
        // Зад reverse proxy реалният адрес е първият в X-Forwarded-For.
        var forwarded = Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
        var ip = string.IsNullOrEmpty(forwarded) ? HttpContext.Connection.RemoteIpAddress?.ToString() : forwarded;
        return locations.FromNetworkAsync(ip, ct);
    }
}

[ApiController]
[Route("api")]
public class WeatherApiController(WeatherService weather, IWeatherProvider provider, SpeedAdvisor advisor, TimeProvider clock) : ControllerBase
{
    /// <summary>Почасова прогноза за деня в района.</summary>
    [HttpGet("weather/today")]
    public WeatherDay Today() => weather.GetToday();

    /// <summary>Последно измерване от най-близката метеостанция на bgtoll.bg (обновява се на 15 мин). 204 — още няма данни.</summary>
    [HttpGet("weather/live")]
    public ActionResult<LiveWeather> Live([FromServices] RoadSense.Web.Infrastructure.BgTollWeatherFeed feed, double lat, double lon)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180) return Problem("Невалидни координати.", statusCode: 400);
        var live = feed.Nearest(lat, lon, clock.GetLocalNow().Hour);
        return live is null ? NoContent() : live;
    }

    /// <summary>Почасова прогноза (Open-Meteo) за точката: видимост, валеж, пориви. 204 — източникът не е достъпен.</summary>
    [HttpGet("weather/forecast")]
    public async Task<ActionResult<ForecastDto>> Forecast([FromServices] OpenMeteoForecast forecast, double lat, double lon, CancellationToken ct)
    {
        if (lat is < -90 or > 90 || lon is < -180 or > 180) return Problem("Невалидни координати.", statusCode: 400);
        var f = await forecast.GetAsync(lat, lon, ct);
        return f is null ? NoContent() : f;
    }

    /// <summary>
    /// Препоръчителна скорост за точката и часа (по подразбиране — текущия). Основа е прогнозата от Open-Meteo;
    /// ако е недостъпна — измерванията от bgtoll. Настилка „auto“ се уточнява с температурата на пътя от най-близката станция.
    /// Пример: /api/speed?lat=42.69&amp;lon=23.32&amp;hour=7&amp;vehicle=car&amp;road=motorway&amp;surface=auto
    /// </summary>
    [HttpGet("speed")]
    public async Task<ActionResult<SpeedRecommendation>> Speed([FromServices] OpenMeteoForecast forecast, [FromServices] BgTollWeatherFeed feed,
        double lat, double lon, int? hour, VehicleType vehicle = VehicleType.Car, RoadType road = RoadType.Motorway, string surface = "auto", CancellationToken ct = default)
    {
        var f = await forecast.GetAsync(lat, lon, ct);
        var live = feed.Nearest(lat, lon, clock.GetLocalNow().Hour);
        WeatherHour w;
        if (f is not null) w = hour is null || hour == f.CurrentHour ? f.Current : f.Hours[Math.Clamp(hour.Value, 0, f.Hours.Count - 1)];
        else if (live is not null) w = live.ToWeatherHour();
        else return Problem("Няма метеоданни за района.", statusCode: 404);

        SurfaceType? s = Enum.TryParse<SurfaceType>(surface, true, out var parsed) ? parsed : null;
        var isNow = f is null || hour is null || hour == f.CurrentHour;
        if (s is null && isNow && live?.Station is { SurfaceC: <= 0.5, HumidityPct: >= 90 } && live.DistanceKm <= 50) s = SurfaceType.Ice;
        return advisor.Recommend(w, vehicle, road, s);
    }
}

[ApiController]
[Route("api/stats")]
public class StatsApiController(StatsService stats) : ControllerBase
{
    [HttpGet("roads")]
    public IEnumerable<object> Roads() => stats.GetRoads().Select(r => new { r.Id, r.Label, r.FullName });

    /// <summary>Загинали по области, месеци и часове + разпределение по км. period: 2025 | 2026 | all</summary>
    [HttpGet("road/{id}")]
    public RoadStatsDto Road(string id, string period = "all") => stats.GetRoad(id, period);

    [HttpGet("ranking")]
    public IReadOnlyList<RankingDto> Ranking() => stats.GetRanking();

    [HttpGet("calendar")]
    public CalendarMonth Calendar(int year, int month) => stats.GetCalendar(year, month);
}

[ApiController]
[Route("api")]
public class ContentApiController(IContentRepository content) : ControllerBase
{
    [HttpGet("ticker")]
    public IReadOnlyList<TickerItem> Ticker() => content.GetTicker();
}
