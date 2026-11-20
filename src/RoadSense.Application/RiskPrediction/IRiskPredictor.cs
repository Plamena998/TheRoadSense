namespace RoadSense.Application.RiskPrediction;

/// <summary>
/// Предвижда оценка на риск по времето с обучен ML модел. Реализира се в Infrastructure (ML.NET).
/// </summary>
public interface IRiskPredictor
{
    RiskEstimate Predict(WeatherRiskFeatures features);

    /// <summary>Метрики на модела от последното обучение.</summary>
    RiskModelInfo ModelInfo { get; }
}
