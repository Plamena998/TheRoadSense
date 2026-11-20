using RoadSense.Domain;

namespace RoadSense.Application.Services;

/// <summary>Настройки на SpeedAdvisor — четат се от appsettings.json (секция "SpeedAdvisor").</summary>
public sealed class SpeedAdvisorOptions
{
    public double ReactionTimeS { get; set; } = 1.5;
    /// <summary>Каква част от видимостта може да заема спирачният път (запас за безопасност).</summary>
    public double VisibilityShare { get; set; } = 0.9;
    public Dictionary<SurfaceType, double> Friction { get; set; } = new()
    {
        [SurfaceType.Dry] = 0.7, [SurfaceType.Wet] = 0.45, [SurfaceType.Snow] = 0.2, [SurfaceType.Ice] = 0.08
    };
    public double HeavyVehicleFriction { get; set; } = 0.8;
    public double MotorcycleWetFriction { get; set; } = 0.85;
    // множители (илюстративни — да се сверят с препоръки за безопасност)
    public double LightRain { get; set; } = 0.9;
    public double HeavyRain { get; set; } = 0.8;
    public double HeavyRainMm { get; set; } = 2.5;
    public double SnowMultiplier { get; set; } = 0.7;
    public double IceMultiplier { get; set; } = 0.5;
    public double Visibility200 { get; set; } = 0.6;
    public double Visibility50 { get; set; } = 0.4;
    public double Gusts { get; set; } = 0.85;
    public int GustsKmh { get; set; } = 60;
    public int MinimumKmh { get; set; } = 20;
}

/// <summary>
/// Препоръчителна скорост = по-ниската от:
/// 1) физика — най-високата скорост, при която спирачният път v·t + v²/(2μg) е по-къс от видимостта;
/// 2) ограничението (от базата по тип път и превозно средство) × множителите за валеж, сняг, лед, мъгла и пориви.
/// Закръгля се надолу до 10 км/ч и никога не надвишава ограничението.
/// </summary>
public sealed class SpeedAdvisor(ISpeedLimitRepository limits, SpeedAdvisorOptions o)
{
    public static SurfaceType SurfaceFrom(WeatherHour w)
    {
        var wet = w.HumidityPct >= 95 || w.PrecipitationMm > 0;
        if (w.TemperatureC <= 0.5 && wet) return SurfaceType.Ice;
        if (w.State == WeatherState.Snow) return SurfaceType.Snow;
        return wet ? SurfaceType.Wet : SurfaceType.Dry;
    }

    public double StoppingDistance(double kmh, double mu) =>
        kmh / 3.6 * o.ReactionTimeS + Math.Pow(kmh / 3.6, 2) / (2 * mu * 9.81);

    public SpeedRecommendation Recommend(WeatherHour w, VehicleType vehicle, RoadType road, SurfaceType? surfaceOverride = null)
    {
        var limit = limits.GetLimit(road, vehicle);
        var surface = surfaceOverride ?? SurfaceFrom(w);
        var heavy = vehicle is VehicleType.Truck or VehicleType.Bus;

        var mu = o.Friction[surface];
        if (heavy) mu *= o.HeavyVehicleFriction;
        if (vehicle == VehicleType.Motorcycle && surface != SurfaceType.Dry) mu *= o.MotorcycleWetFriction;

        // 1) физика
        var physics = limit;
        while (physics > o.MinimumKmh && StoppingDistance(physics, mu) > w.VisibilityM * o.VisibilityShare) physics -= 5;

        // 2) множители
        var factors = new List<AppliedFactor>();
        if (surface == SurfaceType.Ice) factors.Add(new("Риск от поледица", o.IceMultiplier));
        else if (surface == SurfaceType.Snow) factors.Add(new("Сняг", o.SnowMultiplier));
        if (w.PrecipitationMm >= o.HeavyRainMm) factors.Add(new("Силен дъжд", o.HeavyRain));
        else if (w.PrecipitationMm > 0) factors.Add(new("Лек дъжд", o.LightRain));
        if (w.VisibilityM < 50) factors.Add(new("Видимост под 50 м", o.Visibility50));
        else if (w.VisibilityM < 200) factors.Add(new("Видимост под 200 м", o.Visibility200));
        if (w.WindGustKmh >= o.GustsKmh) factors.Add(new($"Пориви над {o.GustsKmh} км/ч", o.Gusts));
        var multiplier = factors.Aggregate(1.0, (a, f) => a * f.Multiplier);

        var v = Math.Min(limit, Math.Min(physics, limit * multiplier));
        var recommended = Math.Max(o.MinimumKmh, (int)Math.Floor(v / 10) * 10);

        var reason = w.VisibilityM < 300 ? $"Видимостта е само {w.VisibilityM} м."
            : surface != SurfaceType.Dry ? $"Настилката е {SurfaceName(surface).ToLowerInvariant()}." : "Условията са добри.";
        var title = recommended < limit ? $"С {limit - recommended} км/ч под ограничението" : "Може да караш с разрешената скорост";
        var text = reason + (recommended < limit ? $" При {limit} км/ч няма да успееш да спреш, преди да видиш препятствие." : "");

        return new SpeedRecommendation(limit, recommended, surface, surfaceOverride is null, w.VisibilityM,
            (int)Math.Round(StoppingDistance(limit, mu)), (int)Math.Round(StoppingDistance(recommended, mu)),
            physics, Math.Round(multiplier, 3), factors, title, text);
    }

    public static string SurfaceName(SurfaceType s) => s switch
    {
        SurfaceType.Dry => "Суха", SurfaceType.Wet => "Мокра", SurfaceType.Snow => "Сняг", _ => "Заледена"
    };
}
