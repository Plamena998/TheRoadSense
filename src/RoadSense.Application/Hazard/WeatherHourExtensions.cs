using RoadSense.Domain;

namespace RoadSense.Application.Hazard;

/// <summary>Връзка между <see cref="WeatherHour"/> (типа на сайта) и <see cref="WeatherObservation"/> (входа на класификатора).</summary>
public static class WeatherHourExtensions
{
    /// <summary>
    /// WeatherHour няма снеговалеж в см и WMO код, затова състоянието се превежда в приблизителен WMO код:
    /// сняг → 73 (умерен сняг), буря → 95. Видимостта се подава директно, така мъглата се определя по нея.
    /// </summary>
    /// <param name="visibilityOverrideM">Видимост за конкретния участък (напр. по излагането му на мъгла), ако е различна от общата.</param>
    public static WeatherObservation ToObservation(this WeatherHour w, int? visibilityOverrideM = null) => new(
        TemperatureC: w.TemperatureC,
        HumidityPercent: w.HumidityPct,
        PrecipitationMmPerHour: w.State == WeatherState.Snow ? 0 : w.PrecipitationMm,
        SnowfallCmPerHour: w.State == WeatherState.Snow ? Math.Max(0.1, w.PrecipitationMm) : 0,
        SnowDepthM: 0,
        WeatherCode: w.State switch
        {
            WeatherState.Snow => 73,
            WeatherState.Storm => 95,
            WeatherState.Fog => 45,
            WeatherState.Cloudy => 3,
            WeatherState.Partly => 2,
            _ => 0
        },
        WindKmh: w.WindKmh,
        GustKmh: w.WindGustKmh,
        IsDay: !w.IsNight,
        VisibilityM: visibilityOverrideM ?? w.VisibilityM);
}

public static class WeatherConditionNames
{
    public static string Bg(this WeatherCondition c) => c switch
    {
        WeatherCondition.LightRain => "слаб дъжд",
        WeatherCondition.Rain => "дъжд",
        WeatherCondition.HeavyRain => "пороен дъжд",
        WeatherCondition.LightSnow => "слаб сняг",
        WeatherCondition.Snow => "сняг",
        WeatherCondition.HeavySnow => "силен снеговалеж",
        WeatherCondition.Fog => "мъгла",
        WeatherCondition.DenseFog => "гъста мъгла",
        WeatherCondition.Ice => "поледица",
        WeatherCondition.FreezingRain => "заледяващ дъжд",
        WeatherCondition.StrongWind => "силен вятър",
        WeatherCondition.Storm => "буря",
        _ => c.ToString()
    };
}
