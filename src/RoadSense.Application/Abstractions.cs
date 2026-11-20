using RoadSense.Domain;

namespace RoadSense.Application;

// Интерфейсите, които всеки източник на данни трябва да реализира.
// Реализира ги RoadSense.Infrastructure.Sql (EF Core и SQL Server); услугите и фронтендът зависят само от тези интерфейси.

public interface IRoadNetworkRepository
{
    IReadOnlyList<City> GetCities();
    IReadOnlyList<RoadSegment> GetSegments();
}

public interface ISpeedLimitRepository
{
    /// <summary>Ограничението по закон за дадения тип път и превозно средство.</summary>
    int GetLimit(RoadType roadType, VehicleType vehicle);
}

public interface IWeatherProvider
{
    WeatherLocation GetLocation();
    /// <summary>Почасова прогноза за деня (0–23 ч).</summary>
    IReadOnlyList<WeatherHour> GetDay(DateOnly date);
    /// <summary>Индекс на мъглата 0–1 за часа от денонощието (за оценка на маршрут).</summary>
    double GetFogIndex(int hourOfDay);
}

public interface IStatsRepository
{
    IReadOnlyList<RoadStats> GetRoads();
    IReadOnlyList<RankingRow> GetRanking();
    IReadOnlyList<CalendarEvent> GetCalendarEvents();
    /// <summary>Относително разпределение на загиналите по месеци (12) и по часове (24).</summary>
    IReadOnlyList<double> GetMonthWeights();
    IReadOnlyList<double> GetHourWeights();
}

public interface IContentRepository
{
    IReadOnlyList<TickerItem> GetTicker();
    HomeContent GetHome();
    FirstAidContent GetFirstAid();
}

/// <summary>Населени места с координати — за „най-близкото място“ до потребителя.</summary>
public interface IPlaceRepository
{
    IReadOnlyList<Place> GetPlaces();
}

/// <summary>
/// Приблизително местоположение по IP адреса на мрежата (Wi-Fi / мобилни данни),
/// когато потребителят не е разрешил GPS. Връща null, ако адресът не може да се определи.
/// </summary>
public interface IIpGeolocator
{
    Task<IpGeoResult?> LocateAsync(string? ipAddress, CancellationToken ct = default);
}

public sealed record Place(string Name, double Latitude, double Longitude, string NearRoad);
public sealed record IpGeoResult(double Latitude, double Longitude, string? City, string? Network, int AccuracyKm);

public sealed record WeatherLocation(string Name, double Latitude, double Longitude, int AccuracyKm, string NearRoad);

public sealed record HomeStat(string Value, string Unit, string Description, string Source);
public sealed record HomeContent(IReadOnlyList<HomeStat> Stats, string StatsSource);

public sealed record FirstAidStep(string Title, string Lead, IReadOnlyList<string> Items);
public sealed record FirstAidTopic(string Id, string Tag, string Title, IReadOnlyList<string> Items);
public sealed record FirstAidContent(IReadOnlyList<FirstAidStep> Steps, IReadOnlyList<FirstAidTopic> Topics, IReadOnlyList<string> Kit);

/// <summary>Абонати за предупреждения по имейл.</summary>
public interface ISubscriberRepository
{
    /// <summary>Записва имейла. Връща false, ако вече е абониран (регистърът на буквите не се отчита).</summary>
    Task<bool> AddAsync(string email, CancellationToken ct = default);
}
