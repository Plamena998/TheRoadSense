using RoadSense.Application;

namespace RoadSense.Application.Services;

/// <summary>Източникът на местоположението, което виждат потребителите.</summary>
public enum LocationSource { Gps, Network, Default }

public sealed record LocationResult(WeatherLocation Location, LocationSource Source, string? Network);

/// <summary>
/// Определя района на потребителя:
/// 1) по GPS координати от браузъра (ако е разрешено);
/// 2) иначе по IP адреса на мрежата (Wi-Fi / мобилни данни) — по-неточно, ±5–25 км;
/// 3) иначе района по подразбиране.
/// И в трите случая се връща най-близкото населено място и пътят до него.
/// </summary>
public sealed class LocationService(IPlaceRepository places, IIpGeolocator geo, IWeatherProvider weather)
{
    public LocationResult FromCoordinates(double lat, double lon, double? accuracyM)
    {
        var p = Nearest(lat, lon);
        var accKm = Math.Max(1, (int)Math.Ceiling((accuracyM ?? 100) / 1000));
        return new LocationResult(new WeatherLocation(p.Name + " и околността", lat, lon, accKm, p.NearRoad), LocationSource.Gps, null);
    }

    public async Task<LocationResult> FromNetworkAsync(string? ip, CancellationToken ct = default)
    {
        IpGeoResult? r = null;
        try { r = await geo.LocateAsync(ip, ct); }
        catch (Exception) when (!ct.IsCancellationRequested) { /* външната услуга не отговаря — падаме към района по подразбиране */ }

        if (r is null) return new LocationResult(weather.GetLocation(), LocationSource.Default, null);
        var p = Nearest(r.Latitude, r.Longitude);
        return new LocationResult(new WeatherLocation(p.Name + " и околността", r.Latitude, r.Longitude, r.AccuracyKm, p.NearRoad),
            LocationSource.Network, r.Network);
    }

    private Place Nearest(double lat, double lon) =>
        places.GetPlaces().MinBy(p => DistanceKm(lat, lon, p.Latitude, p.Longitude))
        ?? new Place("Неизвестно място", lat, lon, "—");

    public static double DistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        double rad(double d) => d * Math.PI / 180;
        var dLat = rad(lat2 - lat1); var dLon = rad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(rad(lat1)) * Math.Cos(rad(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }
}
