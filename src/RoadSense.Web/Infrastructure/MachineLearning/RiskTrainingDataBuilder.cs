using System.Globalization;
using System.Text;
using RoadSense.Application.RiskPrediction;
using RoadSense.Application.Hazard;

namespace RoadSense.Web.Infrastructure.MachineLearning;

/// <summary>
/// Сглобява данните за обучение от CSV файловете в dataSource:
///  • processed/oblast_monthly_ptp.csv – тежки ПТП по област и месец (МВР, 2026-01…06);
///  • weather/&lt;ОБЛАСТ&gt;.csv – почасово време от Open-Meteo за същата област.
///
/// Един ред = една област × един месец. Признаците са <see cref="WeatherRiskFeatures"/> за часовете на месеца.
///
/// Етикетът е относителен риск = наблюдавани / очаквани ПТП, където очакваните са
/// (общо за областта × общо за месеца в страната) / общо. Така се махат двата големи ефекта, които не са време:
/// размерът на областта (София има повече ПТП) и сезонният трафик (през юни се кара много повече, отколкото през януари).
/// Остава отклонението „този месец тук беше по-лош от очакваното“, което моделът се опитва да обясни с времето.
/// </summary>
public static class RiskTrainingDataBuilder
{
    public static List<RiskModelInput> Build(string processedDir, string weatherDir)
    {
        var ptp = File.ReadLines(Path.Combine(processedDir, "oblast_monthly_ptp.csv"), Encoding.UTF8)
            .Skip(1)
            .Select(l => l.Split(','))
            .Where(c => c.Length >= 4)
            .Select(c => new
            {
                Oblast = c[0].Trim().Trim('"', '﻿').ToUpperInvariant(),
                Year = int.Parse(c[1], CultureInfo.InvariantCulture),
                Month = int.Parse(c[2], CultureInfo.InvariantCulture),
                Count = int.Parse(c[3], CultureInfo.InvariantCulture)
            })
            .ToList();

        if (ptp.Count == 0)
            throw new InvalidOperationException("oblast_monthly_ptp.csv е празен.");

        double grand = ptp.Sum(p => p.Count);
        var byOblast = ptp.GroupBy(p => p.Oblast).ToDictionary(g => g.Key, g => (double)g.Sum(p => p.Count));
        var byMonth = ptp.GroupBy(p => (p.Year, p.Month)).ToDictionary(g => g.Key, g => (double)g.Sum(p => p.Count));

        var rows = new List<RiskModelInput>();
        foreach (var oblastGroup in ptp.GroupBy(p => p.Oblast))
        {
            var file = Path.Combine(weatherDir, oblastGroup.Key + ".csv");
            if (!File.Exists(file)) continue;

            var hoursByMonth = ReadHourly(file)
                .GroupBy(h => (h.Time.Year, h.Time.Month))
                .ToDictionary(g => g.Key, g => g.Select(x => x.Observation).ToList());

            foreach (var p in oblastGroup)
            {
                if (!hoursByMonth.TryGetValue((p.Year, p.Month), out var hours) || hours.Count < 24 * 20)
                    continue;

                var expected = byOblast[p.Oblast] * byMonth[(p.Year, p.Month)] / grand;
                if (expected <= 0) continue;

                var row = RiskModelInput.From(WeatherRiskFeatures.FromHours(hours));
                row.Label = (float)(p.Count / expected);
                row.Weight = (float)expected;
                row.Oblast = p.Oblast;
                row.Month = p.Month;
                rows.Add(row);
            }
        }

        return rows;
    }

    /// <summary>
    /// Чете почасов файл от Open-Meteo: първите редове са метаданни (координати), после празен ред и таблицата.
    /// </summary>
    public static IEnumerable<(DateTime Time, WeatherObservation Observation)> ReadHourly(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8);
        string? line;
        string[]? header = null;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("time,", StringComparison.Ordinal)) { header = line.Split(','); break; }
        }
        if (header is null) yield break;

        int Col(string prefix) => Array.FindIndex(header, h => h.StartsWith(prefix, StringComparison.Ordinal));
        int iTime = Col("time"), iTemp = Col("temperature_2m"), iHum = Col("relative_humidity_2m"),
            iPrec = Col("precipitation"), iSnow = Col("snowfall"), iDepth = Col("snow_depth"),
            iCode = Col("weather_code"), iWind = Col("wind_speed_10m"), iGust = Col("wind_gusts_10m"), iDay = Col("is_day");

        static double D(string[] c, int i) =>
            i >= 0 && i < c.Length && double.TryParse(c[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var c = line.Split(',');
            if (!DateTime.TryParseExact(c[iTime], "yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t))
                continue;

            yield return (t, new WeatherObservation(
                TemperatureC: D(c, iTemp),
                HumidityPercent: D(c, iHum),
                PrecipitationMmPerHour: D(c, iPrec),
                SnowfallCmPerHour: D(c, iSnow),
                SnowDepthM: D(c, iDepth),
                WeatherCode: (int)D(c, iCode),
                WindKmh: D(c, iWind),
                GustKmh: D(c, iGust),
                IsDay: D(c, iDay) >= 1));
        }
    }
}
