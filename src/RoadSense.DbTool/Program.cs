using Microsoft.EntityFrameworkCore;
using RoadSense.Infrastructure.Sql;

// Създава/обновява базата RoadSense (EF миграции) и зарежда mock данните.
// Използване: dotnet run --project src/RoadSense.DbTool [connectionString]
var cs = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("ROADSENSE_CONNECTION") ?? DbDefaults.ConnectionString;
await using var db = new RoadSenseDbContext(DbDefaults.Options(cs));

Console.WriteLine("Прилагане на миграции...");
await db.Database.MigrateAsync();

Console.WriteLine("Зареждане на данни от MockData...");
await MockDataSeeder.SeedAsync(db, Path.Combine(AppContext.BaseDirectory, "MockData"));

Console.WriteLine("Зареждане на публични данни (МВР, Open-Meteo, АПИ)...");
await PublicDataSeeder.SeedAsync(db, Path.Combine(AppContext.BaseDirectory, "PublicData"));

Console.WriteLine("Готово.");
