namespace RoadSense.Web.Infrastructure;

/// <summary>Фиксиран час за демо (напр. да показва сутрешната мъгла на презентацията).</summary>
public sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.CreateCustomTimeZone("demo", now.Offset, "demo", "demo");
}
