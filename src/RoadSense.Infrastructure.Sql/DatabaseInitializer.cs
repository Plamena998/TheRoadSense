using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace RoadSense.Infrastructure.Sql;

/// <summary>
/// Подготвя базата при стартиране, без да се решава от настройки:
///  • ако има неприложени EF миграции — прилага ги (създава базата, ако я няма);
///  • всяка таблица, която е празна, се зарежда от своя CSV файл (един метод на таблица, в ред на зависимост).
/// Безопасно е да се пуска при всяко стартиране: при актуална база и пълни таблици не прави нищо.
/// </summary>
public static class DatabaseInitializer
{
    /// <param name="seedPath">Папка с CSV файловете (по един на таблица). Празно = „SeedData“ до изпълнимия файл.</param>
    public static async Task InitializeAsync(RoadSenseDbContext db, string? seedPath = null, ILogger? log = null, CancellationToken ct = default)
    {
        await MigrateIfPendingAsync(db, log, ct);
        var dir = ResolveSeedPath(seedPath);
        if (!Directory.Exists(dir))
        {
            log?.LogWarning("Няма папка с CSV данни ({Dir}) – не зареждам данни.", dir);
            return;
        }
        log?.LogInformation("CSV данни от: {Dir}", dir);

        // Редът е важен: първо таблиците, към които сочат външни ключове.
        await SeedCitiesAsync(db, dir, log, ct);
        await SeedRoadSegmentsAsync(db, dir, log, ct);
        await SeedSpeedLimitsAsync(db, dir, log, ct);
        await SeedWeatherLocationsAsync(db, dir, log, ct);
        await SeedWeatherHourProfilesAsync(db, dir, log, ct);
        await SeedRoadsAsync(db, dir, log, ct);
        await SeedBlackSpotsAsync(db, dir, log, ct);
        await SeedRoadRegionWeightsAsync(db, dir, log, ct);
        await SeedRoadCausesAsync(db, dir, log, ct);
        await SeedRankingRowsAsync(db, dir, log, ct);
        await SeedCalendarEventsAsync(db, dir, log, ct);
        await SeedMonthWeightsAsync(db, dir, log, ct);
        await SeedHourWeightsAsync(db, dir, log, ct);
        await SeedPlacesAsync(db, dir, log, ct);
        await SeedTickerItemsAsync(db, dir, log, ct);
        await SeedHomeStatsAsync(db, dir, log, ct);
        await SeedContentSettingsAsync(db, dir, log, ct);
        await SeedFirstAidStepsAsync(db, dir, log, ct);
        await SeedFirstAidStepItemsAsync(db, dir, log, ct);
        await SeedFirstAidTopicsAsync(db, dir, log, ct);
        await SeedFirstAidTopicItemsAsync(db, dir, log, ct);
        await SeedFirstAidKitItemsAsync(db, dir, log, ct);
        await SeedOblastsAsync(db, dir, log, ct);
        await SeedOblastPeriodStatsAsync(db, dir, log, ct);
        await SeedOblastMonthlyPtpAsync(db, dir, log, ct);
        await SeedHourWeekdayStatsAsync(db, dir, log, ct);
        await SeedOblastRisksAsync(db, dir, log, ct);
        await SeedWeatherDailyAsync(db, dir, log, ct);
        await SeedBridgesAsync(db, dir, log, ct);
        await SeedTunnelsAsync(db, dir, log, ct);
        await SeedSpeedReductionRulesAsync(db, dir, log, ct);
        await SeedDataSourcesAsync(db, dir, log, ct);

    }

    private static string ResolveSeedPath(string? seedPath) =>
        string.IsNullOrWhiteSpace(seedPath) ? Path.Combine(AppContext.BaseDirectory, "SeedData") : Path.GetFullPath(seedPath);

    private static async Task MigrateIfPendingAsync(RoadSenseDbContext db, ILogger? log, CancellationToken ct)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(ct)).ToList();
        if (pending.Count == 0)
        {
            log?.LogInformation("Схемата на базата е актуална.");
            return;
        }
        log?.LogInformation("Прилагам {Count} миграции: {Names}", pending.Count, string.Join(", ", pending));
        await db.Database.MigrateAsync(ct);
    }

    private static Task SeedCitiesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.Cities, Path.Combine(dir, "Cities.csv"), log, ct);

    private static Task SeedRoadSegmentsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.RoadSegments, Path.Combine(dir, "RoadSegments.csv"), log, ct);

    private static Task SeedSpeedLimitsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.SpeedLimits, Path.Combine(dir, "SpeedLimits.csv"), log, ct);

    private static Task SeedWeatherLocationsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.WeatherLocations, Path.Combine(dir, "WeatherLocations.csv"), log, ct);

    private static Task SeedWeatherHourProfilesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.WeatherHourProfiles, Path.Combine(dir, "WeatherHourProfiles.csv"), log, ct);

    private static Task SeedRoadsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.Roads, Path.Combine(dir, "Roads.csv"), log, ct);

    private static Task SeedBlackSpotsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.BlackSpots, Path.Combine(dir, "BlackSpots.csv"), log, ct);

    private static Task SeedRoadRegionWeightsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.RoadRegionWeights, Path.Combine(dir, "RoadRegionWeights.csv"), log, ct);

    private static Task SeedRoadCausesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.RoadCauses, Path.Combine(dir, "RoadCauses.csv"), log, ct);

    private static Task SeedRankingRowsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.RankingRows, Path.Combine(dir, "RankingRows.csv"), log, ct);

    private static Task SeedCalendarEventsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.CalendarEvents, Path.Combine(dir, "CalendarEvents.csv"), log, ct);

    private static Task SeedMonthWeightsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.MonthWeights, Path.Combine(dir, "MonthWeights.csv"), log, ct);

    private static Task SeedHourWeightsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.HourWeights, Path.Combine(dir, "HourWeights.csv"), log, ct);

    private static Task SeedPlacesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.Places, Path.Combine(dir, "Places.csv"), log, ct);

    private static Task SeedTickerItemsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.TickerItems, Path.Combine(dir, "TickerItems.csv"), log, ct);

    private static Task SeedHomeStatsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.HomeStats, Path.Combine(dir, "HomeStats.csv"), log, ct);

    private static Task SeedContentSettingsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.ContentSettings, Path.Combine(dir, "ContentSettings.csv"), log, ct);

    private static Task SeedFirstAidStepsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.FirstAidSteps, Path.Combine(dir, "FirstAidSteps.csv"), log, ct);

    private static Task SeedFirstAidStepItemsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.FirstAidStepItems, Path.Combine(dir, "FirstAidStepItems.csv"), log, ct);

    private static Task SeedFirstAidTopicsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.FirstAidTopics, Path.Combine(dir, "FirstAidTopics.csv"), log, ct);

    private static Task SeedFirstAidTopicItemsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.FirstAidTopicItems, Path.Combine(dir, "FirstAidTopicItems.csv"), log, ct);

    private static Task SeedFirstAidKitItemsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.FirstAidKitItems, Path.Combine(dir, "FirstAidKitItems.csv"), log, ct);

    private static Task SeedOblastsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.Oblasts, Path.Combine(dir, "Oblasts.csv"), log, ct);

    private static Task SeedOblastPeriodStatsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.OblastPeriodStats, Path.Combine(dir, "OblastPeriodStats.csv"), log, ct);

    private static Task SeedOblastMonthlyPtpAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.OblastMonthlyPtp, Path.Combine(dir, "OblastMonthlyPtp.csv"), log, ct);

    private static Task SeedHourWeekdayStatsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.HourWeekdayStats, Path.Combine(dir, "HourWeekdayStats.csv"), log, ct);

    private static Task SeedOblastRisksAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.OblastRisks, Path.Combine(dir, "OblastRisks.csv"), log, ct);

    private static Task SeedWeatherDailyAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.WeatherDaily, Path.Combine(dir, "WeatherDaily.csv"), log, ct);

    private static Task SeedBridgesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.Bridges, Path.Combine(dir, "Bridges.csv"), log, ct);

    private static Task SeedTunnelsAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.Tunnels, Path.Combine(dir, "Tunnels.csv"), log, ct);

    private static Task SeedSpeedReductionRulesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.SpeedReductionRules, Path.Combine(dir, "SpeedReductionRules.csv"), log, ct);

    private static Task SeedDataSourcesAsync(RoadSenseDbContext db, string dir, ILogger? log, CancellationToken ct) =>
        SeedIfEmptyAsync(db, db.DataSources, Path.Combine(dir, "DataSources.csv"), log, ct);

    /// <summary>Ако таблицата е празна и файлът съществува — зарежда я от него. Иначе я пропуска.</summary>
    private static async Task SeedIfEmptyAsync<TEntity>(RoadSenseDbContext db, DbSet<TEntity> table, string file, ILogger? log, CancellationToken ct)
        where TEntity : class
    {
        var name = Path.GetFileNameWithoutExtension(file);
        if (!File.Exists(file))
        {
            log?.LogWarning("{Table}: няма CSV файл ({File}) – пропускам.", name, file);
            return;
        }
        if (await table.AnyAsync(ct))
        {
            log?.LogDebug("{Table}: има данни – пропускам.", name);
            return;
        }
        var result = await CsvData.ImportTableAsync(db, db.Model.FindEntityType(typeof(TEntity))!, file, ct);
        log?.LogInformation("{Table}: заредени {Inserted} реда.", name, result.Inserted);
    }
}
