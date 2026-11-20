using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using RoadSense.Application.RiskPrediction;

namespace RoadSense.Web.Infrastructure.MachineLearning;

/// <summary>
/// Обучава регресионния ML.NET модел за относителен риск и го записва като .zip (+ .json с метриките).
/// Данните са малко (28 области × 6 месеца), затова моделът е прост: Поасонова регресия (log-линейна, подходяща
/// за брой ПТП спрямо очаквания) с L2 регуляризация. Качеството се мери с кръстосана проверка, а не с един тестов дял.
/// Сравнени бяха и FastTree и SDCA – при тези данни никой не е по-добър от базовата линия, Поасон е най-близо.
/// </summary>
public static class RiskModelTrainer
{
    private const int Folds = 5;
    private const int Seed = 42;

    public static (ITransformer Model, RiskModelInfo Info, DataViewSchema Schema) Train(
        MLContext ml, IReadOnlyList<RiskModelInput> rows)
    {
        if (rows.Count < Folds * 4)
            throw new InvalidOperationException($"Твърде малко редове за обучение: {rows.Count}.");

        var data = ml.Data.LoadFromEnumerable(rows);

        var pipeline = ml.Transforms.Concatenate("Features", RiskModelInput.FeatureColumns)
            .Append(ml.Transforms.NormalizeMeanVariance("Features"))
            .Append(ml.Regression.Trainers.LbfgsPoissonRegression(new LbfgsPoissonRegressionTrainer.Options
            {
                LabelColumnName = nameof(RiskModelInput.Label),
                FeatureColumnName = "Features",
                ExampleWeightColumnName = nameof(RiskModelInput.Weight),
                L2Regularization = 1f
            }));

        var cv = ml.Regression.CrossValidate(data, pipeline, numberOfFolds: Folds,
            labelColumnName: nameof(RiskModelInput.Label), seed: Seed);

        // Базова линия: винаги „обичаен риск“ = средния етикет. Моделът има смисъл само ако е по-добър от нея.
        var mean = rows.Average(r => r.Label);
        var baselineMae = rows.Average(r => Math.Abs(r.Label - mean));

        var model = pipeline.Fit(data);

        var mae = cv.Average(f => f.Metrics.MeanAbsoluteError);
        var r2 = cv.Average(f => f.Metrics.RSquared);

        var info = new RiskModelInfo(
            TrainedUtc: DateTime.UtcNow,
            TrainingRows: rows.Count,
            Folds: Folds,
            RSquared: Math.Round(r2, 3),
            MeanAbsoluteError: Math.Round(mae, 3),
            BaselineMeanAbsoluteError: Math.Round(baselineMae, 3),
            IsReliable: r2 > 0 && mae < baselineMae,
            Trainer: "LbfgsPoissonRegression (ML.NET)",
            LabelDescription: "Тежки ПТП наблюдавани / очаквани за областта и месеца (1.0 = обичайно)");

        return (model, info, data.Schema);
    }

    public static RiskModelInfo TrainAndSave(MLContext ml, string processedDir, string weatherDir, string modelPath)
    {
        var rows = RiskTrainingDataBuilder.Build(processedDir, weatherDir);
        var (model, info, schema) = Train(ml, rows);

        Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
        ml.Model.Save(model, schema, modelPath);
        File.WriteAllText(InfoPath(modelPath), JsonSerializer.Serialize(info, JsonOptions));
        return info;
    }

    public static string InfoPath(string modelPath) => Path.ChangeExtension(modelPath, ".json");

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
