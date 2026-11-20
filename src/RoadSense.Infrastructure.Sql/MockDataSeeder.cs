using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RoadSense.Domain;

namespace RoadSense.Infrastructure.Sql;

/// <summary>Зарежда началните данни в базата от mock JSON файловете (само в празни таблици).</summary>
public static class MockDataSeeder
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private record CityDto(string Id, string Name, double X, double Y, string? Note, bool Selectable);
    private record SegmentDto(string Id, string FromCityId, string ToCityId, string Road, double Km, double FogExposure, double History, string? Incident, string? Repair);
    private record LocationDto(string Name, double Latitude, double Longitude, int AccuracyKm, string NearRoad);
    private record WeatherDto(LocationDto Location, double[] FogIndex, double TemperatureMean, double TemperatureAmplitude, int WarmestHour, double[] Precipitation, int Sunrise, int Sunset);
    private record BlackSpotDto(int Km, string Description);
    private record CauseDto(string Label, int Percent);
    private record RoadDto(string Id, string Label, string FullName, int LengthKm, int CrashesPerYear, int DeathsPerYear, int InjuredPerYear,
        List<BlackSpotDto> BlackSpots, Dictionary<string, double> RegionWeights, List<CauseDto> Causes);
    private record RankingDto(string Road, string Section, int LengthKm, int Crashes, int Deaths);
    private record CalendarDto(DateOnly Date, int Deaths, string Description, string Source, string? RoadId);
    private record WeightsDto(double[] Months, double[] Hours);
    private record PlaceDto(string Name, double Latitude, double Longitude, string NearRoad);
    private record TickerDto(string Level, string Road, string Text);
    private record HomeStatDto(string Value, string Unit, string Description, string Source);
    private record HomeDto(string StatsSource, List<HomeStatDto> Stats);
    private record HomeFileDto(List<TickerDto> Ticker, HomeDto Home);
    private record StepDto(string Title, string Lead, List<string> Items);
    private record TopicDto(string Id, string Tag, string Title, List<string> Items);
    private record FirstAidDto(List<StepDto> Steps, List<TopicDto> Topics, List<string> Kit);

    private static T Read<T>(string dir, string file) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(Path.Combine(dir, file)), Json) ?? throw new InvalidDataException(file);

    public static async Task SeedAsync(RoadSenseDbContext db, string dir, CancellationToken ct = default)
    {
        if (!Directory.Exists(dir)) throw new DirectoryNotFoundException($"Липсва папка с mock данни: {dir}");

        if (!await db.Cities.AnyAsync(ct))
        {
            db.Cities.AddRange(Read<List<CityDto>>(dir, "cities.json")
                .Select((c, i) => new CityEntity { SortOrder = i, Id = c.Id, Name = c.Name, X = c.X, Y = c.Y, Note = c.Note, Selectable = c.Selectable }));
            await db.SaveChangesAsync(ct);
            db.RoadSegments.AddRange(Read<List<SegmentDto>>(dir, "segments.json").Select((s, i) => new RoadSegmentEntity
            {
                SortOrder = i, Id = s.Id, FromCityId = s.FromCityId, ToCityId = s.ToCityId, Road = s.Road, Km = s.Km,
                FogExposure = s.FogExposure, History = s.History, Incident = s.Incident, Repair = s.Repair
            }));
        }

        if (!await db.SpeedLimits.AnyAsync(ct))
            db.SpeedLimits.AddRange(Read<List<SpeedLimit>>(dir, "speed-limits.json")
                .Select(l => new SpeedLimitEntity { RoadType = l.RoadType, Vehicle = l.Vehicle, LimitKmh = l.LimitKmh }));

        if (!await db.WeatherLocations.AnyAsync(ct))
        {
            var w = Read<WeatherDto>(dir, "weather.json");
            db.WeatherLocations.Add(new WeatherLocationEntity
            {
                Name = w.Location.Name, Latitude = w.Location.Latitude, Longitude = w.Location.Longitude,
                AccuracyKm = w.Location.AccuracyKm, NearRoad = w.Location.NearRoad,
                TemperatureMean = w.TemperatureMean, TemperatureAmplitude = w.TemperatureAmplitude,
                WarmestHour = w.WarmestHour, Sunrise = w.Sunrise, Sunset = w.Sunset,
                HourProfiles = Enumerable.Range(0, 24).Select(h => new WeatherHourProfileEntity
                    { Hour = h, FogIndex = w.FogIndex[h], PrecipitationMm = w.Precipitation[h] }).ToList()
            });
        }

        if (!await db.Roads.AnyAsync(ct))
        {
            db.Roads.AddRange(Read<List<RoadDto>>(dir, "stats-roads.json").Select((r, ri) => new RoadEntity
            {
                SortOrder = ri, Id = r.Id, Label = r.Label, FullName = r.FullName, LengthKm = r.LengthKm,
                CrashesPerYear = r.CrashesPerYear, DeathsPerYear = r.DeathsPerYear, InjuredPerYear = r.InjuredPerYear,
                BlackSpots = r.BlackSpots.Select(s => new BlackSpotEntity { Km = s.Km, Description = s.Description }).ToList(),
                RegionWeights = r.RegionWeights.Select((p, pi) => new RoadRegionWeightEntity { SortOrder = pi, Region = p.Key, Weight = p.Value }).ToList(),
                Causes = r.Causes.Select(c => new RoadCauseEntity { Label = c.Label, Percent = c.Percent }).ToList()
            }));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.RankingRows.AnyAsync(ct))
            db.RankingRows.AddRange(Read<List<RankingDto>>(dir, "ranking.json").Select(r => new RankingRowEntity
                { Road = r.Road, Section = r.Section, LengthKm = r.LengthKm, Crashes = r.Crashes, Deaths = r.Deaths }));

        if (!await db.CalendarEvents.AnyAsync(ct))
            db.CalendarEvents.AddRange(Read<List<CalendarDto>>(dir, "calendar.json").Select(c => new CalendarEventEntity
                { Date = c.Date, Deaths = c.Deaths, Description = c.Description, Source = c.Source, RoadId = c.RoadId }));

        if (!await db.MonthWeights.AnyAsync(ct))
        {
            var wt = Read<WeightsDto>(dir, "stats-weights.json");
            db.MonthWeights.AddRange(wt.Months.Select((v, i) => new MonthWeightEntity { Month = i + 1, Weight = v }));
            db.HourWeights.AddRange(wt.Hours.Select((v, i) => new HourWeightEntity { Hour = i, Weight = v }));
        }

        if (!await db.Places.AnyAsync(ct))
            db.Places.AddRange(Read<List<PlaceDto>>(dir, "places.json").Select(p => new PlaceEntity
                { Name = p.Name, Latitude = p.Latitude, Longitude = p.Longitude, NearRoad = p.NearRoad }));

        if (!await db.TickerItems.AnyAsync(ct))
        {
            var home = Read<HomeFileDto>(dir, "content-home.json");
            db.TickerItems.AddRange(home.Ticker.Select((t, i) => new TickerItemEntity { SortOrder = i, Level = t.Level, Road = t.Road, Text = t.Text }));
            db.HomeStats.AddRange(home.Home.Stats.Select((s, i) => new HomeStatEntity
                { SortOrder = i, Value = s.Value, Unit = s.Unit, Description = s.Description, Source = s.Source }));
            db.ContentSettings.Add(new ContentSettingEntity { Key = "StatsSource", Value = home.Home.StatsSource });
        }

        if (!await db.FirstAidSteps.AnyAsync(ct))
        {
            var fa = Read<FirstAidDto>(dir, "content-firstaid.json");
            db.FirstAidSteps.AddRange(fa.Steps.Select((s, i) => new FirstAidStepEntity
            {
                SortOrder = i, Title = s.Title, Lead = s.Lead,
                Items = s.Items.Select((t, j) => new FirstAidStepItemEntity { SortOrder = j, Text = t }).ToList()
            }));
            db.FirstAidTopics.AddRange(fa.Topics.Select((t, i) => new FirstAidTopicEntity
            {
                Id = t.Id, SortOrder = i, Tag = t.Tag, Title = t.Title,
                Items = t.Items.Select((x, j) => new FirstAidTopicItemEntity { SortOrder = j, Text = x }).ToList()
            }));
            db.FirstAidKitItems.AddRange(fa.Kit.Select((k, i) => new FirstAidKitItemEntity { SortOrder = i, Text = k }));
        }

        await db.SaveChangesAsync(ct);
    }
}
