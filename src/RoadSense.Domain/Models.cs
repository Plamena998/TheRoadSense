namespace RoadSense.Domain;

// ===== Справочни типове =====

public enum VehicleType { Car, Motorcycle, Van, Truck, Bus }

/// <summary>Тип път — по него се взима ограничението на скоростта от базата.</summary>
public enum RoadType { Motorway, OutsideTown, Town }

public enum SurfaceType { Dry, Wet, Snow, Ice }

public enum RiskLevel { Low, Moderate, High, VeryHigh }

public enum WeatherState { Clear, Partly, Cloudy, Rain, Storm, Snow, Fog }

// ===== Пътна мрежа =====

/// <summary>Населено място / възел в пътната мрежа. X/Y са координати върху схемата 800×440.</summary>
public sealed record City(string Id, string Name, double X, double Y, string? Note = null, bool Selectable = true);

/// <summary>Участък от пътя между два възела.</summary>
public sealed record RoadSegment(
    string Id,
    string FromCityId,
    string ToCityId,
    string Road,        // „А1“, „I-1“ …
    double Km,
    double FogExposure, // 0–1: колко често този участък е в мъгла (низини, реки)
    double History,     // 0–1: нормализирана история на тежките ПТП
    string? Incident,   // текущ инцидент (АПИ / МВР)
    string? Repair)     // текущ ремонт (АПИ)
{
    public bool IsMotorway => Road.StartsWith('А') || Road.StartsWith('A');
    public double AvgSpeedKmh => IsMotorway ? 110 : 75;
}

/// <summary>Ограничение на скоростта по тип път и тип превозно средство (таблица в базата).</summary>
public sealed record SpeedLimit(RoadType RoadType, VehicleType Vehicle, int LimitKmh);

// ===== Време =====

public sealed record WeatherHour(
    int Hour,
    double TemperatureC,
    double DewPointC,
    int HumidityPct,
    int VisibilityM,
    double PrecipitationMm,
    int WindKmh,
    int WindGustKmh,
    WeatherState State,
    bool IsNight);

// ===== Статистика =====

public sealed record BlackSpot(int Km, string Description);

public sealed record RoadStats(
    string Id,
    string Label,
    string FullName,
    int LengthKm,
    int CrashesPerYear,
    int DeathsPerYear,
    int InjuredPerYear,
    IReadOnlyList<BlackSpot> BlackSpots,
    IReadOnlyDictionary<string, double> RegionWeights, // по кои области минава пътят и с каква тежест
    IReadOnlyList<Cause> Causes);

public sealed record RankingRow(string Road, string Section, int LengthKm, int Crashes, int Deaths);

public sealed record CalendarEvent(DateOnly Date, int Deaths, string Description, string Source, string? RoadId = null);

public sealed record Cause(string Label, int Percent);

public sealed record TickerItem(string Level, string Road, string Text);
