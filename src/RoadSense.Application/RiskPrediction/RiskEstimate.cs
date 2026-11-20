using RoadSense.Application.Hazard;
using RoadSense.Domain;

namespace RoadSense.Application.RiskPrediction;

/// <summary>
/// Оценка на риск от ML модела.
/// RelativeRisk: колко пъти повече тежки ПТП се очакват при това време спрямо нормалното за мястото и сезона
/// (1.0 = обичайно, 1.2 = с 20% повече).
/// Score: 0–100, изведен от RelativeRisk (вж. <see cref="RiskScale"/>), за да се показва като останалите оценки.
/// </summary>
public sealed record RiskEstimate(double RelativeRisk, int Score, RiskLevel Level);

/// <summary>Преобразува относителния риск от модела в оценка 0–100 и ниво.</summary>
public static class RiskScale
{
    /// <summary>Точки за всеки 1% над обичайния риск: +50% ⇒ 100 точки.</summary>
    private const double PointsPerUnit = 200;

    public static RiskEstimate FromRelativeRisk(double relativeRisk)
    {
        var rr = Math.Max(0, relativeRisk);
        var score = Math.Clamp((int)Math.Round((rr - 1.0) * PointsPerUnit), 0, 100);
        return new RiskEstimate(Math.Round(rr, 3), score, HazardIndexCalculator.LevelFor(score));
    }
}

/// <summary>
/// Качество на обучения модел (кръстосана проверка), за да е видно колко може да се вярва на оценката.
/// IsReliable е true само ако моделът е по-добър от базовата линия „винаги обичаен риск“ (R² &gt; 0 и по-малка MAE).
/// </summary>
public sealed record RiskModelInfo(
    DateTime TrainedUtc,
    int TrainingRows,
    int Folds,
    double RSquared,
    double MeanAbsoluteError,
    double BaselineMeanAbsoluteError,
    bool IsReliable,
    string Trainer,
    string LabelDescription);
