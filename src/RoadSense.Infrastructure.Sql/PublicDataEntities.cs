namespace RoadSense.Infrastructure.Sql;

// Таблици за публичните данни от dataSource/processed (МВР, Open-Meteo, АПИ ГИС).

public class OblastEntity
{
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>Тежки ПТП по област за полугодие („2026-H1“, „2025-H1“) — МВР.</summary>
public class OblastPeriodStatEntity
{
    public string Oblast { get; set; } = "";
    public string Period { get; set; } = "";
    public int Ptp { get; set; }
    public int Killed { get; set; }
    public int Injured { get; set; }
}

public class OblastMonthlyPtpEntity
{
    public string Oblast { get; set; } = "";
    public int Year { get; set; }
    public int Month { get; set; }
    public int Ptp { get; set; }
}

/// <summary>Тежки ПТП по ден от седмицата (1=понеделник) и час (0–23) за страната — МВР.</summary>
public class HourWeekdayStatEntity
{
    public int Weekday { get; set; }
    public int Hour { get; set; }
    public int Ptp { get; set; }
    public int Killed { get; set; }
    public int Injured { get; set; }
}

/// <summary>Сравнение 2026 спрямо 2025 и показатели за риск по област.</summary>
public class OblastRiskEntity
{
    public string Oblast { get; set; } = "";
    public int Ptp2026 { get; set; }
    public int Killed2026 { get; set; }
    public int Injured2026 { get; set; }
    public int Ptp2025 { get; set; }
    public int Killed2025 { get; set; }
    public int Injured2025 { get; set; }
    public double PtpChangePct { get; set; }
    public double KilledPer100Ptp { get; set; }
    public double InjuredPer100Ptp { get; set; }
    public double SharePctOfCountry { get; set; }
}

/// <summary>Дневно време по област (Open-Meteo архив).</summary>
public class WeatherDailyEntity
{
    public string Oblast { get; set; } = "";
    public DateOnly Date { get; set; }
    public double? TempMin { get; set; }
    public double? TempMax { get; set; }
    public double? TempMean { get; set; }
    public double? PrecipitationMm { get; set; }
    public double? SnowfallCm { get; set; }
    public double? HumidityMean { get; set; }
    public double? CloudMean { get; set; }
    public double? WindMaxKmh { get; set; }
    public double? GustMaxKmh { get; set; }
    public int? WeatherCodeMax { get; set; }
}

public class BridgeEntity
{
    public int Id { get; set; }
    public string RoadRef { get; set; } = "";
    public double Km { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class TunnelEntity
{
    public int Id { get; set; }
    public string RoadRef { get; set; } = "";
    public double Km { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

/// <summary>Намаление на препоръчителната скорост според метеорологично условие.</summary>
public class SpeedReductionRuleEntity
{
    public string Condition { get; set; } = "";
    public int ReductionPercent { get; set; }
    public int? MaxSpeedKmh { get; set; }
    public string Advice { get; set; } = "";
}

public class DataSourceEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Period { get; set; } = "";
    public string Url { get; set; } = "";
}
