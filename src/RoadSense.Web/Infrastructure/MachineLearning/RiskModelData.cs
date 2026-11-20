using Microsoft.ML.Data;
using RoadSense.Application.RiskPrediction;

namespace RoadSense.Web.Infrastructure.MachineLearning;

/// <summary>Ред за ML.NET: признаците от <see cref="WeatherRiskFeatures"/> + етикет и тегло (само при обучение).</summary>
public sealed class RiskModelInput
{
    public float RainShare { get; set; }
    public float HeavyRainShare { get; set; }
    public float SnowShare { get; set; }
    public float FogShare { get; set; }
    public float IceShare { get; set; }
    public float WindShare { get; set; }
    public float MeanTemperatureC { get; set; }
    public float PrecipitationMmPerDay { get; set; }

    /// <summary>Относителен риск: наблюдавани / очаквани тежки ПТП за областта и месеца.</summary>
    public float Label { get; set; }

    /// <summary>Очакваният брой ПТП. Области с повече ПТП имат по-надежден етикет и по-голямо тегло.</summary>
    public float Weight { get; set; }

    /// <summary>Само за справка/диагностика – не влиза в модела.</summary>
    [NoColumn] public string Oblast { get; set; } = "";
    [NoColumn] public int Month { get; set; }

    public static readonly string[] FeatureColumns =
    [
        nameof(RainShare), nameof(HeavyRainShare), nameof(SnowShare), nameof(FogShare),
        nameof(IceShare), nameof(WindShare), nameof(MeanTemperatureC), nameof(PrecipitationMmPerDay)
    ];

    public static RiskModelInput From(WeatherRiskFeatures f) => new()
    {
        RainShare = f.RainShare,
        HeavyRainShare = f.HeavyRainShare,
        SnowShare = f.SnowShare,
        FogShare = f.FogShare,
        IceShare = f.IceShare,
        WindShare = f.WindShare,
        MeanTemperatureC = f.MeanTemperatureC,
        PrecipitationMmPerDay = f.PrecipitationMmPerDay
    };
}

public sealed class RiskModelOutput
{
    [ColumnName("Score")] public float RelativeRisk { get; set; }
}
