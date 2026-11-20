using System.Text.Json;
using Microsoft.ML;
using RoadSense.Application.RiskPrediction;

namespace RoadSense.Web.Infrastructure.MachineLearning;

/// <summary>
/// Предвижда оценка на риск с ML.NET модела. Регистрира се като singleton.
/// При първо ползване зарежда модела от диска; ако файлът липсва, обучава го от dataSource и го записва.
/// За ново обучение (напр. след нови данни от МВР) изтрийте файла с модела и рестартирайте.
/// </summary>
/// <param name="DataSourcePath">Папката dataSource (с processed/ и weather/).</param>
/// <param name="ModelPathOverride">Файл на модела; по подразбиране dataSource/models/risk-model.zip.</param>
public sealed record RiskModelOptions(string DataSourcePath, string? ModelPathOverride = null)
{
    public string ProcessedDataPath => Path.Combine(DataSourcePath, "processed");
    public string WeatherDataPath => Path.Combine(DataSourcePath, "weather");
    public string ModelPath => ModelPathOverride ?? Path.Combine(DataSourcePath, "models", "risk-model.zip");
}

public sealed class MlNetRiskPredictor : IRiskPredictor
{
    private readonly MLContext _ml = new(seed: 42);
    private readonly Lazy<(PredictionEngine<RiskModelInput, RiskModelOutput> Engine, RiskModelInfo Info)> _model;

    // PredictionEngine не е безопасен за няколко нишки; предвиждането е микросекунди, затова стига обикновен lock.
    private readonly Lock _gate = new();

    public MlNetRiskPredictor(RiskModelOptions options, ILogger<MlNetRiskPredictor> logger)
    {
        _model = new Lazy<(PredictionEngine<RiskModelInput, RiskModelOutput>, RiskModelInfo)>(
            () => Load(options, logger), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public RiskModelInfo ModelInfo => _model.Value.Info;

    public RiskEstimate Predict(WeatherRiskFeatures features)
    {
        var engine = _model.Value.Engine;
        float relative;
        lock (_gate)
            relative = engine.Predict(RiskModelInput.From(features)).RelativeRisk;
        return RiskScale.FromRelativeRisk(relative);
    }

    private (PredictionEngine<RiskModelInput, RiskModelOutput>, RiskModelInfo) Load(
        RiskModelOptions options, ILogger logger)
    {
        var modelPath = options.ModelPath;
        RiskModelInfo info;

        if (!File.Exists(modelPath))
        {
            logger.LogInformation("Няма ML модел за риск в {Path} – обучавам от dataSource…", modelPath);
            info = RiskModelTrainer.TrainAndSave(_ml, options.ProcessedDataPath, options.WeatherDataPath, modelPath);
            logger.LogInformation(
                "Моделът е обучен върху {Rows} реда. Кръстосана проверка: R²={R2}, MAE={Mae} (базова линия {Base}).",
                info.TrainingRows, info.RSquared, info.MeanAbsoluteError, info.BaselineMeanAbsoluteError);
            if (!info.IsReliable)
                logger.LogWarning("ML моделът за риск не е по-добър от базовата линия – /api/risk ще ползва правилата за оценката.");
        }
        else
        {
            var infoPath = RiskModelTrainer.InfoPath(modelPath);
            info = File.Exists(infoPath)
                ? JsonSerializer.Deserialize<RiskModelInfo>(File.ReadAllText(infoPath))!
                : new RiskModelInfo(File.GetLastWriteTimeUtc(modelPath), 0, 0, double.NaN, double.NaN, double.NaN, false, "unknown", "");
        }

        var model = _ml.Model.Load(modelPath, out _);
        return (_ml.Model.CreatePredictionEngine<RiskModelInput, RiskModelOutput>(model), info);
    }
}
