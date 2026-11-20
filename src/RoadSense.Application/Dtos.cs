using RoadSense.Domain;

namespace RoadSense.Application;

// Обектите, които API-то връща към фронтенда (JSON, camelCase).

public static class Risk
{
    public static RiskLevel LevelOf(double score) =>
        score < 25 ? RiskLevel.Low : score < 50 ? RiskLevel.Moderate : score < 75 ? RiskLevel.High : RiskLevel.VeryHigh;

    public static string Label(RiskLevel level) => level switch
    {
        RiskLevel.Low => "Нисък",
        RiskLevel.Moderate => "Умерен",
        RiskLevel.High => "Висок",
        _ => "Много висок"
    };
}

// ----- Маршрут -----

public sealed record RouteFactor(string Key, string Label, int Value);

public sealed record RouteLeg(
    string SegmentId, string Road, string From, string To, double Km,
    double StartHour, bool NextDay, int Score, RiskLevel Level, int VisibilityM,
    string Note, string? Incident, string? Repair,
    IReadOnlyList<string> Conditions, int HazardScore);

public sealed record DepartureOption(int Hour, int Score, RiskLevel Level);

public sealed record RouteResult(
    string FromId, string ToId, string FromName, string ToName,
    double TotalKm, double Hours, int DepartureHour, double ArrivalHour, bool ArrivesNextDay,
    int Score, RiskLevel Level, string LevelLabel, string Verdict,
    IReadOnlyList<RouteFactor> Factors, IReadOnlyList<RouteLeg> Legs,
    IReadOnlyList<DepartureOption> Departures, int BestHour, int BestScore,
    IReadOnlyList<string> SegmentIds, string RiskSource, bool ModelReliable);

public sealed record RoadNetworkDto(IReadOnlyList<City> Cities, IReadOnlyList<RoadSegment> Segments);

// ----- Време и скорост -----

public sealed record WeatherDay(WeatherLocation Location, DateOnly Date, int CurrentHour,
    double MinTemperatureC, double MaxTemperatureC, IReadOnlyList<WeatherHour> Hours, IReadOnlyList<string> Warnings);

public sealed record AppliedFactor(string Label, double Multiplier);

public sealed record SpeedRecommendation(
    int LimitKmh, int RecommendedKmh, SurfaceType Surface, bool SurfaceAuto,
    int VisibilityM, int StoppingAtLimitM, int StoppingAtRecommendedM,
    int PhysicsKmh, double CombinedMultiplier, IReadOnlyList<AppliedFactor> Factors,
    string Title, string Explanation);

// ----- Статистика -----

public sealed record NamedValue(string Name, double Value);

public sealed record KmBin(int FromKm, int ToKm, int Crashes, bool IsBlackSpot, double Relative);

public sealed record RoadStatsDto(
    string RoadId, string FullName, int LengthKm, string Period, string PeriodLabel,
    int Deaths, int Crashes,
    IReadOnlyList<NamedValue> Regions, IReadOnlyList<NamedValue> Months, IReadOnlyList<NamedValue> Hours,
    int TopMonthIndex, int TopHour, string? TopMonthNote,
    IReadOnlyList<KmBin> Bins, IReadOnlyList<BlackSpot> BlackSpots, IReadOnlyList<Cause> Causes);

public sealed record RankingDto(int Rank, string Road, string Section, int LengthKm, int Crashes, int Deaths, double DeathsPer100Km, string Grade);

public sealed record CalendarDay(int Day, int Weekday, bool IsToday, CalendarEvent? Event);

public sealed record CalendarMonth(int Year, int Month, string Name, int LeadingBlanks, IReadOnlyList<CalendarDay> Days,
    bool HasEvents, IReadOnlyList<int> MonthsWithEvents, bool CanGoNext);
