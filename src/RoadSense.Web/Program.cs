using System.Text.Json;
using System.Text.Json.Serialization;
using RoadSense.Application;
using RoadSense.Application.Services;
using Microsoft.EntityFrameworkCore;
using RoadSense.Infrastructure.Mock;
using RoadSense.Infrastructure.Sql;
using RoadSense.Application.RiskPrediction;
using RoadSense.Web.Infrastructure;
using RoadSense.Web.Infrastructure.MachineLearning;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));

// ---- Час: реален или фиксиран демо час (appsettings: "DemoNow") ----
var demoNow = builder.Configuration["DemoNow"];
builder.Services.AddSingleton<TimeProvider>(string.IsNullOrWhiteSpace(demoNow)
    ? TimeProvider.System
    : new FixedTimeProvider(DateTimeOffset.Parse(demoNow)));

// ---- Източник на данни ----
// "Sql" — SQL Server през EF Core (ConnectionStrings:RoadSense), "Mock" — JSON файловете в MockData.
var dataSource = builder.Configuration["DataSource"] ?? "Sql";
// MockDataStore остава за IP местоположението в режим "Mock" на IpGeolocation (ip-location.json).
builder.Services.AddSingleton<MockDataStore>();
if (dataSource.Equals("Sql", StringComparison.OrdinalIgnoreCase))
{
    var cs = builder.Configuration.GetConnectionString("RoadSense") ?? DbDefaults.ConnectionString;
    builder.Services.AddDbContextFactory<RoadSenseDbContext>(o => o.UseSqlServer(cs));
    builder.Services.AddSingleton<SqlDataStore>();
    builder.Services.AddSingleton<IRoadNetworkRepository>(sp => sp.GetRequiredService<SqlDataStore>());
    builder.Services.AddSingleton<ISpeedLimitRepository>(sp => sp.GetRequiredService<SqlDataStore>());
    builder.Services.AddSingleton<IWeatherProvider>(sp => sp.GetRequiredService<SqlDataStore>());
    builder.Services.AddSingleton<IStatsRepository>(sp => sp.GetRequiredService<SqlDataStore>());
    builder.Services.AddSingleton<IContentRepository>(sp => sp.GetRequiredService<SqlDataStore>());
    builder.Services.AddSingleton<IPlaceRepository>(sp => sp.GetRequiredService<SqlDataStore>());
}
else if (dataSource.Equals("Mock", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IRoadNetworkRepository>(sp => sp.GetRequiredService<MockDataStore>());
    builder.Services.AddSingleton<ISpeedLimitRepository>(sp => sp.GetRequiredService<MockDataStore>());
    builder.Services.AddSingleton<IWeatherProvider>(sp => sp.GetRequiredService<MockDataStore>());
    builder.Services.AddSingleton<IStatsRepository>(sp => sp.GetRequiredService<MockDataStore>());
    builder.Services.AddSingleton<IContentRepository>(sp => sp.GetRequiredService<MockDataStore>());
    builder.Services.AddSingleton<IPlaceRepository>(sp => sp.GetRequiredService<MockDataStore>());
}
else
{
    throw new NotSupportedException($"Източникът на данни \"{dataSource}\" още не е реализиран. Използвайте \"Sql\" или \"Mock\".");
}

// ---- Местоположение по мрежата (Wi-Fi / мобилни данни), когато няма GPS ----
// "Mock" — отговорът от MockData/ip-location.json; "IpWhoIs" — реалната услуга ipwho.is.
if ((builder.Configuration["IpGeolocation"] ?? "Mock").Equals("IpWhoIs", StringComparison.OrdinalIgnoreCase))
    builder.Services.AddHttpClient<IIpGeolocator, IpWhoIsGeolocator>(c => c.Timeout = TimeSpan.FromSeconds(4));
else
    builder.Services.AddSingleton<IIpGeolocator>(sp => sp.GetRequiredService<MockDataStore>());

// ---- Услуги ----
builder.Services.AddSingleton(builder.Configuration.GetSection("SpeedAdvisor").Get<SpeedAdvisorOptions>() ?? new SpeedAdvisorOptions());
builder.Services.AddSingleton<RouteRiskService>();
builder.Services.AddSingleton<SpeedAdvisor>();
builder.Services.AddSingleton<StatsService>();
builder.Services.AddSingleton<WeatherService>();
builder.Services.AddSingleton<LocationService>();

// ---- Прогноза (видимост, валеж, пориви) от Open-Meteo ----
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("openmeteo", c => { c.Timeout = TimeSpan.FromSeconds(10); c.DefaultRequestHeaders.UserAgent.ParseAdd("RoadSense/1.0"); });
builder.Services.AddSingleton<OpenMeteoForecast>();

// ---- ML.NET модел за оценка на риска по времето ----
// Обучава се от dataSource (месечни ПТП по области + почасово време) при първо ползване, ако файлът на модела липсва.
builder.Services.AddSingleton(new RiskModelOptions(
    Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, builder.Configuration["RiskModel:DataSourcePath"] ?? "../../dataSource")),
    builder.Configuration["RiskModel:ModelPath"] is { Length: > 0 } modelPath ? modelPath : null));
builder.Services.AddSingleton<IRiskPredictor, MlNetRiskPredictor>();

// ---- Живи метеоданни от bgtoll.bg (JSON, обновява се на 15 минути) ----
builder.Services.AddHttpClient("bgtoll", c => c.Timeout = TimeSpan.FromSeconds(20));
builder.Services.AddSingleton<BgTollWeatherFeed>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BgTollWeatherFeed>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
