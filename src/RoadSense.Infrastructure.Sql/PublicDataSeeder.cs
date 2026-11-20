using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace RoadSense.Infrastructure.Sql;

/// <summary>Зарежда публичните данни (МВР, Open-Meteo, АПИ) от CSV файловете в dataSource/processed.</summary>
public static class PublicDataSeeder
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static List<string[]> ReadCsv(string dir, string file)
    {
        var lines = File.ReadAllLines(Path.Combine(dir, file), Encoding.UTF8);
        // част от записите имат кавички с нов ред вътре — слепваме редовете, докато кавичките се затворят
        var records = new List<string>();
        var cur = new StringBuilder();
        foreach (var l in lines.Skip(1))
        {
            if (cur.Length == 0 && string.IsNullOrWhiteSpace(l)) continue;
            cur.Append(cur.Length == 0 ? "" : " ").Append(l);
            if (cur.ToString().Count(c => c == '"') % 2 == 0) { records.Add(cur.ToString()); cur.Clear(); }
        }
        return records.Select(r => Split(r).Select(f => f.Trim()).ToArray()).ToList();
    }

    private static string[] Split(string line)
    {
        var res = new List<string>();
        var sb = new StringBuilder();
        var q = false;
        foreach (var c in line.TrimStart('﻿'))
        {
            if (c == '"') q = !q;
            else if (c == ',' && !q) { res.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        res.Add(sb.ToString());
        return res.ToArray();
    }

    private static double D(string s) => double.Parse(s, Inv);
    private static double? DN(string s) => string.IsNullOrWhiteSpace(s) ? null : D(s);
    private static int I(string s) => (int)Math.Round(D(s));

    public static async Task SeedAsync(RoadSenseDbContext db, string dir, CancellationToken ct = default)
    {
        if (!Directory.Exists(dir)) throw new DirectoryNotFoundException($"Липсва папка с данни: {dir}");
        db.ChangeTracker.AutoDetectChangesEnabled = false;

        if (!await db.Oblasts.AnyAsync(ct))
        {
            db.Oblasts.AddRange(ReadCsv(dir, "oblasts.csv").Select(r => new OblastEntity { Name = r[0], Latitude = D(r[1]), Longitude = D(r[2]) }));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.OblastPeriodStats.AnyAsync(ct))
            db.OblastPeriodStats.AddRange(ReadCsv(dir, "oblast_period_stats.csv").Select(r =>
                new OblastPeriodStatEntity { Oblast = r[0], Period = r[1], Ptp = I(r[2]), Killed = I(r[3]), Injured = I(r[4]) }));

        if (!await db.OblastMonthlyPtp.AnyAsync(ct))
            db.OblastMonthlyPtp.AddRange(ReadCsv(dir, "oblast_monthly_ptp.csv").Select(r =>
                new OblastMonthlyPtpEntity { Oblast = r[0], Year = I(r[1]), Month = I(r[2]), Ptp = I(r[3]) }));

        if (!await db.HourWeekdayStats.AnyAsync(ct))
            db.HourWeekdayStats.AddRange(ReadCsv(dir, "hour_weekday_stats.csv").Select(r =>
                new HourWeekdayStatEntity { Weekday = I(r[0]), Hour = I(r[1]), Ptp = I(r[2]), Killed = I(r[3]), Injured = I(r[4]) }));

        if (!await db.OblastRisks.AnyAsync(ct))
            db.OblastRisks.AddRange(ReadCsv(dir, "oblasti_risk.csv").Select(r => new OblastRiskEntity
            {
                Oblast = r[0], Ptp2026 = I(r[1]), Killed2026 = I(r[2]), Injured2026 = I(r[3]),
                Ptp2025 = I(r[4]), Killed2025 = I(r[5]), Injured2025 = I(r[6]),
                PtpChangePct = D(r[7]), KilledPer100Ptp = D(r[8]), InjuredPer100Ptp = D(r[9]), SharePctOfCountry = D(r[10])
            }));

        if (!await db.Bridges.AnyAsync(ct))
            db.Bridges.AddRange(ReadCsv(dir, "bridges.csv").Select(r =>
                new BridgeEntity { RoadRef = r[0], Km = D(r[1]), Latitude = D(r[2]), Longitude = D(r[3]) }));

        if (!await db.Tunnels.AnyAsync(ct))
            db.Tunnels.AddRange(ReadCsv(dir, "tunnels.csv").Select(r =>
                new TunnelEntity { RoadRef = r[0], Km = D(r[1]), Latitude = D(r[2]), Longitude = D(r[3]) }));

        if (!await db.SpeedReductionRules.AnyAsync(ct))
            // съветът може да съдържа запетаи без кавички — всичко след 3-тата колона е текст
            db.SpeedReductionRules.AddRange(ReadCsv(dir, "speed_reduction_rules.csv").Select(r => new SpeedReductionRuleEntity
            {
                Condition = r[0], ReductionPercent = I(r[1]),
                MaxSpeedKmh = string.IsNullOrWhiteSpace(r[2]) ? null : I(r[2]),
                Advice = string.Join(",", r.Skip(3))
            }));

        if (!await db.DataSources.AnyAsync(ct))
            db.DataSources.AddRange(ReadCsv(dir, "data_sources.csv").Select(r =>
                new DataSourceEntity { Name = r[0], Period = r[1], Url = string.Join(",", r.Skip(2)) }));

        await db.SaveChangesAsync(ct);

        if (!await db.WeatherDaily.AnyAsync(ct))
        {
            var rows = ReadCsv(dir, "weather_daily.csv");
            foreach (var chunk in rows.Chunk(2000))
            {
                db.WeatherDaily.AddRange(chunk.Select(r => new WeatherDailyEntity
                {
                    Oblast = r[0], Date = DateOnly.ParseExact(r[1], "yyyy-MM-dd", Inv),
                    TempMin = DN(r[2]), TempMax = DN(r[3]), TempMean = DN(r[4]), PrecipitationMm = DN(r[5]),
                    SnowfallCm = DN(r[6]), HumidityMean = DN(r[7]), CloudMean = DN(r[8]),
                    WindMaxKmh = DN(r[9]), GustMaxKmh = DN(r[10]),
                    WeatherCodeMax = string.IsNullOrWhiteSpace(r[11]) ? null : I(r[11])
                }));
                await db.SaveChangesAsync(ct);
                db.ChangeTracker.Clear();
            }
        }
    }
}
