using RoadSense.Domain;

namespace RoadSense.Infrastructure.Sql;

// EF таблици за данните на приложението (началните стойности са в dataSource/seed/*.csv).

// ===== Пътна мрежа =====
public class CityEntity
{
    public int SortOrder { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public string? Note { get; set; }
    public bool Selectable { get; set; }
}

public class RoadSegmentEntity
{
    public int SortOrder { get; set; }
    public string Id { get; set; } = "";
    public string FromCityId { get; set; } = "";
    public string ToCityId { get; set; } = "";
    public string Road { get; set; } = "";
    public double Km { get; set; }
    public double FogExposure { get; set; }
    public double History { get; set; }
    public string? Incident { get; set; }
    public string? Repair { get; set; }
    public CityEntity FromCity { get; set; } = null!;
    public CityEntity ToCity { get; set; } = null!;
}

public class SpeedLimitEntity
{
    public RoadType RoadType { get; set; }
    public VehicleType Vehicle { get; set; }
    public int LimitKmh { get; set; }
}

// ===== Време =====
public class WeatherLocationEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int AccuracyKm { get; set; }
    public string NearRoad { get; set; } = "";
    public double TemperatureMean { get; set; }
    public double TemperatureAmplitude { get; set; }
    public int WarmestHour { get; set; }
    public int Sunrise { get; set; }
    public int Sunset { get; set; }
    public List<WeatherHourProfileEntity> HourProfiles { get; set; } = new();
}

/// <summary>Профил по час (0–23): индекс на мъглата и валежи.</summary>
public class WeatherHourProfileEntity
{
    public int LocationId { get; set; }
    public int Hour { get; set; }
    public double FogIndex { get; set; }
    public double PrecipitationMm { get; set; }
}

// ===== Статистика =====
public class RoadEntity
{
    public int SortOrder { get; set; }
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string FullName { get; set; } = "";
    public int LengthKm { get; set; }
    public int CrashesPerYear { get; set; }
    public int DeathsPerYear { get; set; }
    public int InjuredPerYear { get; set; }
    public List<BlackSpotEntity> BlackSpots { get; set; } = new();
    public List<RoadRegionWeightEntity> RegionWeights { get; set; } = new();
    public List<RoadCauseEntity> Causes { get; set; } = new();
    public List<CalendarEventEntity> CalendarEvents { get; set; } = new();
}

public class BlackSpotEntity
{
    public int Id { get; set; }
    public string RoadId { get; set; } = "";
    public int Km { get; set; }
    public string Description { get; set; } = "";
}

public class RoadRegionWeightEntity
{
    public int SortOrder { get; set; }
    public string RoadId { get; set; } = "";
    public string Region { get; set; } = "";
    public double Weight { get; set; }
}

public class RoadCauseEntity
{
    public int Id { get; set; }
    public string RoadId { get; set; } = "";
    public string Label { get; set; } = "";
    public int Percent { get; set; }
}

public class RankingRowEntity
{
    public int Id { get; set; }
    public string Road { get; set; } = "";
    public string Section { get; set; } = "";
    public int LengthKm { get; set; }
    public int Crashes { get; set; }
    public int Deaths { get; set; }
}

public class CalendarEventEntity
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }
    public int Deaths { get; set; }
    public string Description { get; set; } = "";
    public string Source { get; set; } = "";
    public string? RoadId { get; set; }
    public RoadEntity? Road { get; set; }
}

public class MonthWeightEntity
{
    public int Month { get; set; } // 1–12
    public double Weight { get; set; }
}

public class HourWeightEntity
{
    public int Hour { get; set; } // 0–23
    public double Weight { get; set; }
}

// ===== Места =====
public class PlaceEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string NearRoad { get; set; } = "";
}

// ===== Съдържание =====
public class TickerItemEntity
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Level { get; set; } = "";
    public string Road { get; set; } = "";
    public string Text { get; set; } = "";
}

public class HomeStatEntity
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Value { get; set; } = "";
    public string Unit { get; set; } = "";
    public string Description { get; set; } = "";
    public string Source { get; set; } = "";
}

/// <summary>Ключ–стойност за единични текстове (напр. StatsSource на началната страница).</summary>
public class ContentSettingEntity
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}

public class FirstAidStepEntity
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = "";
    public string Lead { get; set; } = "";
    public List<FirstAidStepItemEntity> Items { get; set; } = new();
}

public class FirstAidStepItemEntity
{
    public int Id { get; set; }
    public int StepId { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; } = "";
}

public class FirstAidTopicEntity
{
    public string Id { get; set; } = "";
    public int SortOrder { get; set; }
    public string Tag { get; set; } = "";
    public string Title { get; set; } = "";
    public List<FirstAidTopicItemEntity> Items { get; set; } = new();
}

public class FirstAidTopicItemEntity
{
    public int Id { get; set; }
    public string TopicId { get; set; } = "";
    public int SortOrder { get; set; }
    public string Text { get; set; } = "";
}

public class FirstAidKitItemEntity
{
    public int Id { get; set; }
    public int SortOrder { get; set; }
    public string Text { get; set; } = "";
}

// ===== Абонати =====
public class SubscriberEntity
{
    public int Id { get; set; }
    /// <summary>Нормализиран (малки букви), уникален.</summary>
    public string Email { get; set; } = "";
    public DateTime SubscribedUtc { get; set; }
}
