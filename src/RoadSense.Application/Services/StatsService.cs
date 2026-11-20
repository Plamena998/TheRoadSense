using System.Globalization;
using RoadSense.Domain;

namespace RoadSense.Application.Services;

public sealed class StatsService(IStatsRepository repo, TimeProvider clock)
{
    public static readonly string[] MonthNames = ["януари", "февруари", "март", "април", "май", "юни", "юли", "август", "септември", "октомври", "ноември", "декември"];
    private static readonly string[] MonthTitles = ["Януари", "Февруари", "Март", "Април", "Май", "Юни", "Юли", "Август", "Септември", "Октомври", "Ноември", "Декември"];

    public IReadOnlyList<RoadStats> GetRoads() => repo.GetRoads();

    /// <param name="period">"2025", "2026" или "all" (2025–2026)</param>
    public RoadStatsDto GetRoad(string roadId, string period)
    {
        var r = repo.GetRoads().FirstOrDefault(x => x.Id == roadId) ?? repo.GetRoads()[0];
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        var (factor, label, years) = period switch
        {
            "2025" => (1.0, "за 2025 г.", new[] { 2025 }),
            "2026" => (YearShare(today), $"за 2026 г. (до {today.Day} {MonthNames[today.Month - 1]})", new[] { 2026 }),
            _ => (1 + YearShare(today), "за 2025–2026 г.", new[] { 2025, 2026 })
        };
        period = period is "2025" or "2026" ? period : "all";

        var crashes = (int)Math.Round(r.CrashesPerYear * factor);
        var deathsBase = r.DeathsPerYear * factor;

        // месеци
        var mw = repo.GetMonthWeights();
        var months = mw.Select((w, i) => deathsBase * w / mw.Sum() * Noise(i + r.LengthKm)).ToArray();
        if (period == "2026") { for (var m = today.Month; m < 12; m++) months[m] = 0; months[today.Month - 1] *= today.Day / 30.0; }
        string? note = null;
        foreach (var ev in repo.GetCalendarEvents().Where(e => e.RoadId == r.Id && years.Contains(e.Date.Year)))
        {
            months[ev.Date.Month - 1] += ev.Deaths;
            note = "вкл. реални ПТП от " + ev.Date.Year;
        }
        var monthVals = months.Select(v => Math.Round(v)).ToArray();
        var deaths = (int)monthVals.Sum();
        var topMonth = Array.IndexOf(monthVals, monthVals.Max());

        // области
        var rw = r.RegionWeights;
        var regions = rw.Select((kv, i) => new NamedValue(kv.Key, Math.Round(deaths * kv.Value / rw.Values.Sum() * Noise(i + 40))))
                        .OrderByDescending(x => x.Value).ToList();
        // часове
        var hw = repo.GetHourWeights();
        var hours = hw.Select((w, i) => deaths * w / hw.Sum() * Noise(i + 80)).ToArray();
        var topHour = Array.IndexOf(hours, hours.Max());

        // разпределение по километри (18 интервала) с черните точки
        const int nb = 18;
        var binLen = (double)r.LengthKm / nb;
        var raw = Enumerable.Range(0, nb).Select(i =>
        {
            var v = 0.25 + 0.5 * Noise(i * 3 + r.LengthKm) - 0.4;
            foreach (var s in r.BlackSpots)
            {
                var d = Math.Abs((i + 0.5) * binLen - s.Km);
                if (d < binLen * 1.2) v += 1.2 - d / binLen * 0.6;
            }
            return Math.Max(0.1, v);
        }).ToArray();
        var bins = raw.Select((v, i) => new KmBin((int)Math.Round(i * binLen), (int)Math.Round((i + 1) * binLen),
            (int)Math.Round(v / raw.Sum() * crashes), r.BlackSpots.Any(s => Math.Abs((i + 0.5) * binLen - s.Km) < binLen * 0.75),
            Math.Round(v / raw.Max(), 3))).ToList();

        return new RoadStatsDto(r.Id, r.FullName, r.LengthKm, period, label, deaths, crashes, regions,
            monthVals.Select((v, i) => new NamedValue(MonthNames[i], v)).ToList(),
            hours.Select((v, i) => new NamedValue($"{i:00}:00", Math.Round(v, 1))).ToList(),
            topMonth, topHour, topMonth >= 0 && note is not null ? note : null,
            bins, r.BlackSpots, r.Causes);
    }

    public IReadOnlyList<RankingDto> GetRanking()
    {
        return repo.GetRanking()
            .Select(x => (x, per: Math.Round(x.Deaths / (double)x.LengthKm * 100, 1)))
            .OrderByDescending(t => t.per)
            .Select((t, i) => new RankingDto(i + 1, t.x.Road, t.x.Section, t.x.LengthKm, t.x.Crashes, t.x.Deaths, t.per, Grade(t.per / 5)))
            .ToList();
    }

    /// <summary>Оценка А–Е по загинали на 100 км на година.</summary>
    public static string Grade(double perYear) =>
        perYear >= 9 ? "Е" : perYear >= 6.5 ? "Д" : perYear >= 4.5 ? "Г" : perYear >= 3 ? "В" : perYear >= 1.5 ? "Б" : "А";

    public CalendarMonth GetCalendar(int year, int month)
    {
        month = Math.Clamp(month, 1, 12);
        var events = repo.GetCalendarEvents();
        var today = DateOnly.FromDateTime(clock.GetLocalNow().DateTime);
        // не показваме месеци, които още не са дошли
        if (year > today.Year || (year == today.Year && month > today.Month)) { year = today.Year; month = today.Month; }
        var first = new DateOnly(year, month, 1);
        var lead = ((int)first.DayOfWeek + 6) % 7; // понеделник = 0
        var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).Select(d =>
        {
            var date = new DateOnly(year, month, d);
            return new CalendarDay(d, ((int)date.DayOfWeek + 6) % 7, date == today, events.FirstOrDefault(e => e.Date == date));
        }).ToList();
        var withEvents = events.Where(e => e.Date.Year == year).Select(e => e.Date.Month).Distinct().Order().ToList();
        return new CalendarMonth(year, month, $"{MonthTitles[month - 1]} {year}", lead, days, days.Any(d => d.Event is not null), withEvents,
            CanGoNext: year < today.Year || (year == today.Year && month < today.Month));
    }

    private static double YearShare(DateOnly today) => today.Year > 2026 ? 1 : Math.Round(today.DayOfYear / 365.0, 3);

    // детерминиран „шум“ за демо данните (±20%)
    private static double Noise(int k)
    {
        var x = Math.Sin(k * 12.9898) * 43758.5453;
        return 0.8 + 0.4 * (x - Math.Floor(x));
    }
}

public sealed class WeatherService(IWeatherProvider provider, TimeProvider clock)
{
    public WeatherDay GetToday()
    {
        var now = clock.GetLocalNow();
        var date = DateOnly.FromDateTime(now.DateTime);
        var hours = provider.GetDay(date);
        var warnings = new List<string>();
        var fogHours = hours.Where(h => h.VisibilityM < 200).Select(h => h.Hour).ToList();
        if (fogHours.Count > 0)
            warnings.Add($"Гъста мъгла между {fogHours.Min():00}:00 и {fogHours.Max() + 1:00}:00 — видимост под 200 м.");
        if (hours.Any(h => h.TemperatureC <= 0.5 && (h.HumidityPct >= 95 || h.PrecipitationMm > 0)))
            warnings.Add("Възможна поледица по мостове и надлези.");
        return new WeatherDay(provider.GetLocation(), date, now.Hour,
            hours.Min(h => h.TemperatureC), hours.Max(h => h.TemperatureC), hours, warnings);
    }
}
