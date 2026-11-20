using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using RoadSense.Application;

namespace RoadSense.Web.Infrastructure;

/// <summary>
/// Реално местоположение по IP чрез безплатната услуга ipwho.is (без ключ).
/// Включва се с "IpGeolocation": "IpWhoIs" в appsettings.json.
/// За локален адрес (localhost, 192.168.x.x) няма как да се определи място — връща null
/// и приложението показва района по подразбиране.
/// </summary>
public sealed class IpWhoIsGeolocator(HttpClient http) : IIpGeolocator
{
    public async Task<IpGeoResult?> LocateAsync(string? ipAddress, CancellationToken ct = default)
    {
        if (!IPAddress.TryParse(ipAddress, out var ip) || IsPrivate(ip)) return null;

        using var doc = JsonDocument.Parse(await http.GetStringAsync("https://ipwho.is/" + ip, ct));
        var root = doc.RootElement;
        if (!root.TryGetProperty("success", out var ok) || !ok.GetBoolean()) return null;

        var isp = root.TryGetProperty("connection", out var c) && c.TryGetProperty("isp", out var i) ? i.GetString() : null;
        return new IpGeoResult(root.GetProperty("latitude").GetDouble(), root.GetProperty("longitude").GetDouble(),
            root.TryGetProperty("city", out var city) ? city.GetString() : null, isp, AccuracyKm: 15);
    }

    private static bool IsPrivate(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (IPAddress.IsLoopback(ip)) return true;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6) return ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal;
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
    }
}
