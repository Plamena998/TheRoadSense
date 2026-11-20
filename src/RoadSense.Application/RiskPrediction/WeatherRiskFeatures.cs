using RoadSense.Application.Hazard;

namespace RoadSense.Application.RiskPrediction;

/// <summary>
/// Признаци за ML модела за риск: обобщение на времето за период (обикновено 24 ч. напред).
/// Всички дялове са частта от часовете (0–1), в които <see cref="WeatherClassifier"/> открива условието.
/// Моделът е обучен върху месечни обобщения по области, затова признаците са дялове, а не моментни стойности:
/// така един и същ вид данни се подава и при обучение (месец), и при предвиждане (прозорец от часове).
/// </summary>
public sealed record WeatherRiskFeatures(
    float RainShare,
    float HeavyRainShare,
    float SnowShare,
    float FogShare,
    float IceShare,
    float WindShare,
    float MeanTemperatureC,
    float PrecipitationMmPerDay)
{
    /// <summary>Пориви над този праг (км/ч) се броят като ветровит час.</summary>
    public const double GustyThresholdKmh = 50;

    public static WeatherRiskFeatures FromHours(IReadOnlyCollection<WeatherObservation> hours)
    {
        if (hours.Count == 0)
            throw new ArgumentException("Нужен е поне един час с данни.", nameof(hours));

        int rain = 0, heavyRain = 0, snow = 0, fog = 0, ice = 0, wind = 0;
        double tempSum = 0, precipSum = 0;

        foreach (var h in hours)
        {
            var c = WeatherClassifier.Classify(h);
            if (c.Any(x => x is WeatherCondition.LightRain or WeatherCondition.Rain or WeatherCondition.HeavyRain)) rain++;
            if (c.Any(x => x is WeatherCondition.Rain or WeatherCondition.HeavyRain)) heavyRain++;
            if (c.Any(x => x is WeatherCondition.LightSnow or WeatherCondition.Snow or WeatherCondition.HeavySnow)) snow++;
            if (c.Any(x => x is WeatherCondition.Fog or WeatherCondition.DenseFog)) fog++;
            if (c.Any(x => x is WeatherCondition.Ice or WeatherCondition.FreezingRain)) ice++;
            if (h.GustKmh >= GustyThresholdKmh || c.Any(x => x is WeatherCondition.StrongWind or WeatherCondition.Storm)) wind++;
            tempSum += h.TemperatureC;
            precipSum += h.PrecipitationMmPerHour;
        }

        float n = hours.Count;
        return new WeatherRiskFeatures(
            RainShare: rain / n,
            HeavyRainShare: heavyRain / n,
            SnowShare: snow / n,
            FogShare: fog / n,
            IceShare: ice / n,
            WindShare: wind / n,
            MeanTemperatureC: (float)(tempSum / n),
            PrecipitationMmPerDay: (float)(precipSum / n * 24));
    }
}
