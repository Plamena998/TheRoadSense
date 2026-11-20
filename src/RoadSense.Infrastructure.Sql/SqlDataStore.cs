using Microsoft.EntityFrameworkCore;
using RoadSense.Application;
using RoadSense.Domain;

namespace RoadSense.Infrastructure.Sql;

/// <summary>
/// Реализация на всички интерфейси за данни върху SQL Server (EF Core). Заменя MockDataStore.
/// Резултатите се държат в памет кратко време (справочните данни рядко се променят),
/// така че сингълтон услугите не удрят базата при всяка заявка.
/// </summary>
public sealed class SqlDataStore(IDbContextFactory<RoadSenseDbContext> factory) : IRoadNetworkRepository, ISpeedLimitRepository, IWeatherProvider,
    IStatsRepository, IContentRepository, IPlaceRepository
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
    private readonly Dictionary<string, (DateTime at, object value)> _cache = new();
    private readonly object _lock = new();

    private T Cached<T>(string key, Func<RoadSenseDbContext, T> load) where T : notnull
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var c) && DateTime.UtcNow - c.at < Ttl) return (T)c.value;
            using var db = factory.CreateDbContext();
            db.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            var v = load(db);
            _cache[key] = (DateTime.UtcNow, v);
            return v;
        }
    }

    // ---- пътна мрежа ----
    public IReadOnlyList<City> GetCities() => Cached<IReadOnlyList<City>>("cities", db =>
        db.Cities.OrderBy(c => c.SortOrder).AsEnumerable()
            .Select(c => new City(c.Id, c.Name, c.X, c.Y, c.Note, c.Selectable)).ToList());

    public IReadOnlyList<RoadSegment> GetSegments() => Cached<IReadOnlyList<RoadSegment>>("segments", db =>
        db.RoadSegments.OrderBy(s => s.SortOrder).AsEnumerable()
            .Select(s => new RoadSegment(s.Id, s.FromCityId, s.ToCityId, s.Road, s.Km, s.FogExposure, s.History, s.Incident, s.Repair)).ToList());

    // ---- ограничения ----
    public int GetLimit(RoadType roadType, VehicleType vehicle) =>
        Cached<Dictionary<(RoadType, VehicleType), int>>("limits", db =>
            db.SpeedLimits.ToDictionary(l => (l.RoadType, l.Vehicle), l => l.LimitKmh))[(roadType, vehicle)];

    // ---- време ----
    private sealed record WeatherProfile(WeatherLocation Location, double[] Fog, double[] Precip, double Mean, double Amplitude, int Warmest, int Sunrise, int Sunset);

    private WeatherProfile Profile() => Cached("weather", db =>
    {
        var w = db.WeatherLocations.Include(x => x.HourProfiles).OrderBy(x => x.Id).First();
        var fog = new double[24];
        var rain = new double[24];
        foreach (var h in w.HourProfiles) { fog[h.Hour] = h.FogIndex; rain[h.Hour] = h.PrecipitationMm; }
        return new WeatherProfile(new WeatherLocation(w.Name, w.Latitude, w.Longitude, w.AccuracyKm, w.NearRoad),
            fog, rain, w.TemperatureMean, w.TemperatureAmplitude, w.WarmestHour, w.Sunrise, w.Sunset);
    });

    public WeatherLocation GetLocation() => Profile().Location;
    public double GetFogIndex(int hourOfDay) => Profile().Fog[((hourOfDay % 24) + 24) % 24];

    public IReadOnlyList<WeatherHour> GetDay(DateOnly date)
    {
        var w = Profile();
        return Enumerable.Range(0, 24).Select(h =>
        {
            var fog = w.Fog[h];
            var vis = (int)Math.Max(40, Math.Round(10000 * Math.Exp(-5.3 * fog)));
            var temp = Math.Round(w.Mean + w.Amplitude * Math.Cos((h - w.Warmest) / 24.0 * 2 * Math.PI), 1);
            var hum = (int)Math.Min(100, Math.Round(66 + 33 * fog));
            var dew = Math.Round(temp - (0.3 + 6 * (1 - fog)), 1);
            var wind = (int)Math.Round(6 + 10 * Math.Max(0, Math.Sin((h - 9) / 24.0 * 2 * Math.PI)));
            var night = h < w.Sunrise || h >= w.Sunset;
            var rain = w.Precip[h];
            var state = vis < 1000 ? WeatherState.Fog : rain > 0 ? WeatherState.Rain : fog > 0.15 ? WeatherState.Partly : WeatherState.Clear;
            return new WeatherHour(h, temp, dew, hum, vis, rain, wind, wind + 9, state, night);
        }).ToList();
    }

    // ---- статистика ----
    public IReadOnlyList<RoadStats> GetRoads() => Cached<IReadOnlyList<RoadStats>>("roads", db =>
        db.Roads.Include(r => r.BlackSpots).Include(r => r.RegionWeights).Include(r => r.Causes)
            .AsSplitQuery().OrderBy(r => r.SortOrder).AsEnumerable()
            .Select(r => new RoadStats(r.Id, r.Label, r.FullName, r.LengthKm, r.CrashesPerYear, r.DeathsPerYear, r.InjuredPerYear,
                r.BlackSpots.OrderBy(b => b.Id).Select(b => new BlackSpot(b.Km, b.Description)).ToList(),
                r.RegionWeights.OrderBy(x => x.SortOrder).ToDictionary(x => x.Region, x => x.Weight),
                r.Causes.OrderBy(c => c.Id).Select(c => new Cause(c.Label, c.Percent)).ToList())).ToList());

    public IReadOnlyList<RankingRow> GetRanking() => Cached<IReadOnlyList<RankingRow>>("ranking", db =>
        db.RankingRows.OrderBy(r => r.Id).AsEnumerable()
            .Select(r => new RankingRow(r.Road, r.Section, r.LengthKm, r.Crashes, r.Deaths)).ToList());

    public IReadOnlyList<CalendarEvent> GetCalendarEvents() => Cached<IReadOnlyList<CalendarEvent>>("calendar", db =>
        db.CalendarEvents.OrderBy(c => c.Date).AsEnumerable()
            .Select(c => new CalendarEvent(c.Date, c.Deaths, c.Description, c.Source, c.RoadId)).ToList());

    public IReadOnlyList<double> GetMonthWeights() => Cached<IReadOnlyList<double>>("monthW", db =>
        db.MonthWeights.OrderBy(m => m.Month).Select(m => m.Weight).ToList());

    public IReadOnlyList<double> GetHourWeights() => Cached<IReadOnlyList<double>>("hourW", db =>
        db.HourWeights.OrderBy(h => h.Hour).Select(h => h.Weight).ToList());

    // ---- места ----
    public IReadOnlyList<Place> GetPlaces() => Cached<IReadOnlyList<Place>>("places", db =>
        db.Places.OrderBy(p => p.Id).AsEnumerable().Select(p => new Place(p.Name, p.Latitude, p.Longitude, p.NearRoad)).ToList());

    // ---- съдържание ----
    public IReadOnlyList<TickerItem> GetTicker() => Cached<IReadOnlyList<TickerItem>>("ticker", db =>
        db.TickerItems.OrderBy(t => t.SortOrder).AsEnumerable().Select(t => new TickerItem(t.Level, t.Road, t.Text)).ToList());

    public HomeContent GetHome() => Cached("home", db =>
        new HomeContent(
            db.HomeStats.OrderBy(s => s.SortOrder).AsEnumerable().Select(s => new HomeStat(s.Value, s.Unit, s.Description, s.Source)).ToList(),
            db.ContentSettings.Where(s => s.Key == "StatsSource").Select(s => s.Value).FirstOrDefault() ?? ""));

    public FirstAidContent GetFirstAid() => Cached("firstaid", db =>
        new FirstAidContent(
            db.FirstAidSteps.Include(s => s.Items).AsSplitQuery().OrderBy(s => s.SortOrder).AsEnumerable()
                .Select(s => new FirstAidStep(s.Title, s.Lead, s.Items.OrderBy(i => i.SortOrder).Select(i => i.Text).ToList())).ToList(),
            db.FirstAidTopics.Include(t => t.Items).AsSplitQuery().OrderBy(t => t.SortOrder).AsEnumerable()
                .Select(t => new FirstAidTopic(t.Id, t.Tag, t.Title, t.Items.OrderBy(i => i.SortOrder).Select(i => i.Text).ToList())).ToList(),
            db.FirstAidKitItems.OrderBy(k => k.SortOrder).Select(k => k.Text).ToList()));
}
