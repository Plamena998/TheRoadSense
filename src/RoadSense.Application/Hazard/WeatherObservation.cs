namespace RoadSense.Application.Hazard;

/// <summary>
/// Моментно време за точка от пътя. Полетата съвпадат с почасовите данни на Open-Meteo
/// (dataSource/weather), така че една и съща структура се ползва и за проверка върху историята, и на живо от API.
/// </summary>
public sealed record WeatherObservation(
    double TemperatureC,
    double HumidityPercent,
    double PrecipitationMmPerHour,
    double SnowfallCmPerHour,
    double SnowDepthM,
    int WeatherCode,
    double WindKmh,
    double GustKmh,
    bool IsDay,
    double? VisibilityM = null,
    double? SurfaceTempC = null
);