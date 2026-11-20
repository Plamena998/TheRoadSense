using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;
using RoadSense.Infrastructure.Sql;

// Инструмент за базата RoadSense.
//   dotnet run --project src/RoadSense.DbTool                   — миграции (ако има чакащи) + зареждане от CSV на празните таблици
//   dotnet run --project src/RoadSense.DbTool -- export [папка] — записва всяка таблица в CSV файл (по подразбиране dataSource/seed)
// Низ за връзка: ROADSENSE_CONNECTION или DbDefaults.ConnectionString. Папка с CSV за зареждане: ROADSENSE_SEED_PATH.
var cs = Environment.GetEnvironmentVariable("ROADSENSE_CONNECTION") ?? DbDefaults.ConnectionString;
await using var db = new RoadSenseDbContext(DbDefaults.Options(cs));

if (args.Length > 0 && args[0].Equals("export", StringComparison.OrdinalIgnoreCase))
{
    var outDir = args.Length > 1 ? args[1] : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../dataSource/seed"));
    Console.WriteLine($"Износ към {outDir}");
    foreach (var (table, count) in CsvData.Export(db, outDir)) Console.WriteLine($"  {table}.csv — {count} реда");
    return;
}

using var loggerFactory = LoggerFactory.Create(b => b.AddSimpleConsole(o => o.SingleLine = true).SetMinimumLevel(LogLevel.Debug));
await DatabaseInitializer.InitializeAsync(db, Environment.GetEnvironmentVariable("ROADSENSE_SEED_PATH"), loggerFactory.CreateLogger("DbTool"));
Console.WriteLine("Готово.");
