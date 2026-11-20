using System.Collections;
using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#pragma warning disable EF1002 // имената на таблиците идват от EF модела, не от потребителски вход

namespace RoadSense.Infrastructure.Sql;

/// <summary>
/// CSV данни за всички таблици в базата: по един файл на ентитет, наречен като таблицата (напр. Cities.csv).
/// Първият ред е заглавие с имената на свойствата на ентитета; празна стойност означава NULL.
/// Работи през EF метаданните, затова нова таблица влиза автоматично, щом има файл с нейното име.
/// </summary>
public static class CsvData
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Резултат от зареждането на една таблица.</summary>
    public sealed record TableResult(string Table, int Inserted, int Skipped, bool FileFound);

    // ---------- метаданни ----------

    /// <summary>Ентитетите в ред на зависимост: първо тези, към които сочат външни ключове.</summary>
    public static List<IEntityType> OrderedEntityTypes(RoadSenseDbContext db)
    {
        var result = new List<IEntityType>();
        var seen = new HashSet<IEntityType>();
        void Visit(IEntityType t)
        {
            if (!seen.Add(t)) return;
            foreach (var fk in t.GetForeignKeys())
                if (fk.PrincipalEntityType != t) Visit(fk.PrincipalEntityType);
            result.Add(t);
        }
        foreach (var t in db.Model.GetEntityTypes().OrderBy(t => t.GetTableName(), StringComparer.Ordinal)) Visit(t);
        return result;
    }

    private static List<IProperty> Columns(IEntityType t) =>
        t.GetProperties().Where(p => p.PropertyInfo is not null).ToList();

    private static string FileName(IEntityType t) => t.GetTableName() + ".csv";

    private static string KeyOf(object entity, IReadOnlyList<IProperty> key) =>
        string.Join("|", key.Select(p => Format(p.PropertyInfo!.GetValue(entity))));

    private static IEnumerable<object> SetOf(RoadSenseDbContext db, IEntityType t)
    {
        var set = typeof(DbContext).GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(t.ClrType).Invoke(db, null)!;
        var asNoTracking = typeof(EntityFrameworkQueryableExtensions).GetMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking))!
            .MakeGenericMethod(t.ClrType).Invoke(null, [set])!;
        return ((IEnumerable)asNoTracking).Cast<object>();
    }

    // ---------- износ (база → CSV) ----------

    /// <summary>Записва всяка таблица в свой CSV файл в папката. Връща броя редове по таблици.</summary>
    public static Dictionary<string, int> Export(RoadSenseDbContext db, string dir)
    {
        Directory.CreateDirectory(dir);
        var counts = new Dictionary<string, int>();
        foreach (var t in OrderedEntityTypes(db))
        {
            var cols = Columns(t);
            var key = t.FindPrimaryKey()!.Properties;
            var rows = SetOf(db, t).ToList();
            rows = rows.OrderBy(r => KeyOf(r, key), new NaturalComparer()).ToList();

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", cols.Select(c => Quote(c.Name))));
            foreach (var r in rows)
                sb.AppendLine(string.Join(",", cols.Select(c => Quote(Format(c.PropertyInfo!.GetValue(r))))));
            File.WriteAllText(Path.Combine(dir, FileName(t)), sb.ToString(), new UTF8Encoding(false));
            counts[t.GetTableName()!] = rows.Count;
        }
        return counts;
    }

    // ---------- внос (CSV → база) ----------

    /// <summary>
    /// Зарежда таблицата на ентитета от CSV файла. Запис, чийто първичен ключ вече е в базата, се пропуска.
    /// </summary>
    public static async Task<TableResult> ImportTableAsync(RoadSenseDbContext db, IEntityType t, string file, CancellationToken ct = default)
    {
        db.ChangeTracker.AutoDetectChangesEnabled = false;
        var table = t.GetTableName()!;
        var cols = Columns(t);
        var key = t.FindPrimaryKey()!.Properties;

        // ключовете, които вече са в базата
        var existing = SetOf(db, t).Select(e => KeyOf(e, key)).ToHashSet();

        var records = ParseCsv(File.ReadAllText(file, Encoding.UTF8));
        if (records.Count == 0) return new(table, 0, 0, true);
        var header = records[0].Select(h => h.Trim()).ToList();
        var map = cols.Select(c => (Col: c, Index: header.IndexOf(c.Name))).Where(x => x.Index >= 0).ToList();
        var missingKey = key.Where(k => map.All(m => m.Col != k)).Select(k => k.Name).ToList();
        if (missingKey.Count > 0)
            throw new InvalidDataException($"{Path.GetFileName(file)}: липсва колона за първичния ключ ({string.Join(", ", missingKey)}).");

        var toAdd = new List<object>();
        var skipped = 0;
        for (var i = 1; i < records.Count; i++)
        {
            var row = records[i];
            var entity = Activator.CreateInstance(t.ClrType)!;
            foreach (var (col, idx) in map)
            {
                var raw = idx < row.Count ? row[idx] : "";
                try { col.PropertyInfo!.SetValue(entity, Parse(col, raw)); }
                catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or ArgumentException)
                {
                    throw new InvalidDataException($"{Path.GetFileName(file)}, ред {i + 1}, колона {col.Name}: невалидна стойност „{raw}“.", ex);
                }
            }
            if (existing.Add(KeyOf(entity, key))) toAdd.Add(entity); else skipped++;
        }
        if (toAdd.Count == 0) return new(table, 0, skipped, true);

        // автоматично генериран цял ключ (IDENTITY) — записваме го със стойността от файла
        var identityKey = key.Count == 1 && key[0].ClrType == typeof(int) && key[0].ValueGenerated == ValueGenerated.OnAdd;
        var schema = t.GetSchema() ?? "dbo";

        foreach (var chunk in toAdd.Chunk(2000))
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            if (identityKey) await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{schema}].[{table}] ON", ct);
            foreach (var e in chunk) db.Add(e);
            await db.SaveChangesAsync(ct);
            if (identityKey) await db.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT [{schema}].[{table}] OFF", ct);
            await tx.CommitAsync(ct);
            db.ChangeTracker.Clear();
        }
        return new(table, toAdd.Count, skipped, true);
    }

    // ---------- стойности ----------

    private static object? Parse(IProperty p, string raw)
    {
        var type = Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType;
        if (raw.Length == 0)
        {
            if (type == typeof(string)) return p.IsNullable ? null : "";
            if (p.IsNullable) return null;
            throw new FormatException("Празна стойност за задължително поле.");
        }
        if (type == typeof(string)) return raw;
        if (type == typeof(int)) return int.Parse(raw, Inv);
        if (type == typeof(long)) return long.Parse(raw, Inv);
        if (type == typeof(double)) return double.Parse(raw, Inv);
        if (type == typeof(decimal)) return decimal.Parse(raw, Inv);
        if (type == typeof(bool)) return bool.Parse(raw);
        if (type == typeof(DateOnly)) return DateOnly.ParseExact(raw, "yyyy-MM-dd", Inv);
        if (type == typeof(DateTime)) return DateTime.Parse(raw, Inv, DateTimeStyles.RoundtripKind);
        if (type.IsEnum) return Enum.Parse(type, raw, ignoreCase: true);
        throw new NotSupportedException($"Тип {type.Name} не се поддържа в CSV.");
    }

    private static string Format(object? v) => v switch
    {
        null => "",
        string s => s,
        bool b => b ? "true" : "false",
        double d => d.ToString("R", Inv),
        float f => f.ToString("R", Inv),
        DateOnly d => d.ToString("yyyy-MM-dd", Inv),
        DateTime d => d.ToString("o", Inv),
        IFormattable f => f.ToString(null, Inv),
        _ => v.ToString() ?? ""
    };

    private static string Quote(string s) =>
        s.IndexOfAny([',', '"', '\r', '\n']) >= 0 || s.Length != s.Trim().Length ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;

    /// <summary>CSV по RFC 4180: полета в кавички, удвоени кавички, нови редове вътре в кавичките.</summary>
    public static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var sb = new StringBuilder();
        var quoted = false;
        var any = false;
        if (text.Length > 0 && text[0] == '﻿') text = text[1..];

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                    else quoted = false;
                }
                else sb.Append(c);
                continue;
            }
            switch (c)
            {
                case '"': quoted = true; any = true; break;
                case ',': row.Add(sb.ToString()); sb.Clear(); any = true; break;
                case '\r': break;
                case '\n':
                    if (any || sb.Length > 0) { row.Add(sb.ToString()); rows.Add(row); }
                    row = []; sb.Clear(); any = false;
                    break;
                default: sb.Append(c); any = true; break;
            }
        }
        if (any || sb.Length > 0) { row.Add(sb.ToString()); rows.Add(row); }
        return rows;
    }

    /// <summary>Подрежда ключове като „2“ преди „10“, а текстовете по азбучен ред.</summary>
    private sealed class NaturalComparer : IComparer<string>
    {
        public int Compare(string? x, string? y)
        {
            var a = (x ?? "").Split('|'); var b = (y ?? "").Split('|');
            for (var i = 0; i < Math.Min(a.Length, b.Length); i++)
            {
                var c = double.TryParse(a[i], NumberStyles.Float, Inv, out var da) && double.TryParse(b[i], NumberStyles.Float, Inv, out var db)
                    ? da.CompareTo(db)
                    : string.CompareOrdinal(a[i], b[i]);
                if (c != 0) return c;
            }
            return a.Length.CompareTo(b.Length);
        }
    }
}
