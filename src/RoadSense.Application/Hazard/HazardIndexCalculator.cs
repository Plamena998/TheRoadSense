using RoadSense.Domain;

namespace RoadSense.Application.Hazard;

/// <summary>Објекти на пътя, които усилват определени опасности.</summary>
[Flags]
public enum RoadFeatures
{
    None = 0,
    Bridge = 1,
    Tunnel = 2
}

/// <summary>Придонес на едно условие в оценката (0–100 поени).</summary>
public sealed record HazardFactor(WeatherCondition Condition, int Points);

/// <summary>Оценка на метеорологична опасност за точка от пътя.</summary>
public sealed record HazardResult(int Score, RiskLevel Level, IReadOnlyList<HazardFactor> Factors);

/// <summary>
/// Индекс на метеорологична опасност 0–100 за участък от пътя.
/// Не е вероятност за ПТП, а мярка колко опасно е времето на този участък в този час.
/// Теглата идват от HazardWeights, така че оценката и препоръчаната скорост не си противоречат.
/// Условията се комбинират с "шумно ИЛИ" (1 − Π(1 − wᵢ)): едно силно условие дава висока оценка,
/// а няколко слаби се натрупват, без да се усредняват.
/// Мост и тунел усилват поледицата и вятъра (мостът замръзва пръв, при портала на тунела има рязка смяна на условията).
/// Множителите са експертна оценка, не обучени стойности.
/// </summary>
public static class HazardIndexCalculator
{
    /// <summary>Добавка към оценката нощем при лоши условия (дял от 0 до 1).</summary>
    private const double NightBonus = 0.05;

    /// <summary>Таван на тежестта на едно условие, за да не стигне оценката 100 само от него.</summary>
    private const double MaxWeight = 0.95;

    private const double BridgeMultiplier = 1.30;
    private const double TunnelMultiplier = 1.15;

    public static HazardResult Calculate(WeatherObservation weather, RoadFeatures features = RoadFeatures.None)
    {
        var conditions = WeatherClassifier.Classify(weather);
        if (conditions.Count == 0)
            return new HazardResult(0, RiskLevel.Low, []);

        var factors = conditions
            .Select(c => new HazardFactor(c, (int)Math.Round(Weight(c, features) * 100)))
            .OrderByDescending(f => f.Points)
            .ToList();

        var safeProbability = conditions.Aggregate(1.0, (acc, c) => acc * (1 - Weight(c, features)));
        var combined = 1 - safeProbability;

        if (!weather.IsDay)
            combined = Math.Min(1.0, combined + NightBonus);

        var score = Math.Clamp((int)Math.Round(combined * 100), 0, 100);
        return new HazardResult(score, LevelFor(score), factors);
    }

    /// <summary>Оценка за цял маршрут: най-лошият участък, не средно. Един мъглив участък е по-важен от десет чисти.</summary>
    public static int RouteScore(IEnumerable<int> segmentScores)
        => segmentScores.DefaultIfEmpty(0).Max();

    /// <summary>Нивата са общите за проекта (<see cref="Risk.LevelOf"/>), за да съвпадат с маршрута.</summary>
    public static RiskLevel LevelFor(int score) => Risk.LevelOf(score);

    private static double Weight(WeatherCondition condition, RoadFeatures features)
    {
        var rule = HazardWeights.For(condition);
        var weight = rule.ReductionPercent / (double)HazardWeights.MaxTotalReductionPercent;

        if (IsAmplifiedByStructures(condition))
        {
            if (features.HasFlag(RoadFeatures.Bridge)) weight *= BridgeMultiplier;
            if (features.HasFlag(RoadFeatures.Tunnel)) weight *= TunnelMultiplier;
        }

        return Math.Min(weight, MaxWeight);
    }

    private static bool IsAmplifiedByStructures(WeatherCondition c)
        => c is WeatherCondition.Ice
            or WeatherCondition.FreezingRain
            or WeatherCondition.StrongWind
            or WeatherCondition.Storm;
}