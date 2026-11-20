using RoadSense.Domain;

namespace RoadSense.Application.Services;

/// <summary>
/// Намира най-бързия маршрут по пътната мрежа (Дейкстра по време) и оценява риска 0–100
/// за всеки участък в часа, в който шофьорът ще мине през него.
/// Тежести: време и видимост 45%, история на ПТП 25%, инциденти и ремонти 20%, трафик 10%.
/// </summary>
public sealed class RouteRiskService(IRoadNetworkRepository network, IWeatherProvider weather)
{
    private static readonly IReadOnlyDictionary<VehicleType, int> VehicleBonus = new Dictionary<VehicleType, int>
    {
        [VehicleType.Car] = 0, [VehicleType.Van] = 2, [VehicleType.Motorcycle] = 8, [VehicleType.Truck] = 6, [VehicleType.Bus] = 6
    };

    public RoadNetworkDto GetNetwork() => new(network.GetCities(), network.GetSegments());

    public RouteResult Evaluate(string fromId, string toId, int departureHour, VehicleType vehicle)
    {
        var cities = network.GetCities().ToDictionary(c => c.Id);
        if (!cities.ContainsKey(fromId)) throw new ArgumentException($"Непознат град: {fromId}", nameof(fromId));
        if (!cities.ContainsKey(toId)) throw new ArgumentException($"Непознат град: {toId}", nameof(toId));
        departureHour = Math.Clamp(departureHour, 0, 23);

        var path = fromId == toId ? [] : ShortestPath(fromId, toId);
        var eval = EvaluatePath(path, departureHour, vehicle);

        var departures = Enumerable.Range(0, 24)
            .Select(h => { var s = EvaluatePath(path, h, vehicle).Score; return new DepartureOption(h, s, Risk.LevelOf(s)); })
            .ToList();
        var best = departures.OrderBy(d => d.Score).ThenBy(d => Math.Abs(d.Hour - departureHour)).First();

        var legs = eval.Legs.Select(l =>
        {
            var notes = new List<string>
            {
                l.Risk.Weather >= 74 ? $"мъгла, видимост ~{l.Risk.Visibility} м" : l.Risk.Weather >= 50 ? "намалена видимост" : "добра видимост"
            };
            if (l.Segment.Incident is { } inc) notes.Add(inc);
            if (l.Segment.Repair is { } rep) notes.Add(rep);
            if (l.Segment.History >= 0.8) notes.Add("черна точка");
            return new RouteLeg(l.Segment.Id, l.Segment.Road, cities[l.From].Name, cities[l.To].Name, l.Segment.Km,
                l.Start, l.Start >= 24, l.Risk.Total, Risk.LevelOf(l.Risk.Total), l.Risk.Visibility,
                string.Join(" · ", notes), l.Segment.Incident, l.Segment.Repair);
        }).ToList();

        var verdict = "Избери две различни точки.";
        if (eval.Worst is { } w)
        {
            var mid = w.Start + w.Segment.Km / w.Segment.AvgSpeedKmh / 2;
            var cause = w.Risk.Weather >= 74 ? $"гъста мъгла (видимост около {w.Risk.Visibility} м)"
                : w.Risk.Weather >= 50 ? "намалена видимост"
                : w.Segment.Incident is not null ? "инцидент на пътя"
                : w.Segment.Repair is not null ? "ремонт" : "историята на катастрофите в участъка";
            verdict = $"Най-рисково е при {cities[w.From].Name} – {cities[w.To].Name} около {Clock(mid)}{(mid >= 24 ? " (утре)" : "")}: {cause}.";
        }

        var arrival = departureHour + eval.Hours;
        return new RouteResult(
            fromId, toId, cities[fromId].Name, cities[toId].Name,
            Math.Round(eval.Km), Math.Round(eval.Hours, 2), departureHour, Math.Round(arrival, 2), arrival >= 24,
            eval.Score, Risk.LevelOf(eval.Score), Risk.Label(Risk.LevelOf(eval.Score)), verdict,
            [
                new("weather", "Време и видимост", (int)Math.Round(eval.Factors.Weather)),
                new("history", "История на ПТП", (int)Math.Round(eval.Factors.History)),
                new("incidents", "Инциденти, ремонти", (int)Math.Round(eval.Factors.Incidents)),
                new("traffic", "Трафик", (int)Math.Round(eval.Factors.Traffic))
            ],
            legs, departures, best.Hour, best.Score, path.Select(p => p.Segment.Id).ToList());
    }

    public static string Clock(double hours)
    {
        var x = ((hours % 24) + 24) % 24;
        var h = (int)Math.Floor(x);
        var m = (int)(Math.Round((x - h) * 60 / 5) * 5);
        if (m == 60) { h = (h + 1) % 24; m = 0; }
        return $"{h:00}:{m:00}";
    }

    // ---------- вътрешни ----------

    private sealed record Step(RoadSegment Segment, string From, string To);
    private sealed record SegmentRisk(int Total, double Weather, double History, double Incidents, double Traffic, int Visibility);
    private sealed record LegEval(RoadSegment Segment, string From, string To, double Start, SegmentRisk Risk);
    private sealed record Factors(double Weather, double History, double Incidents, double Traffic);
    private sealed record PathEval(List<LegEval> Legs, int Score, LegEval? Worst, double Km, double Hours, Factors Factors);

    private List<Step> ShortestPath(string from, string to)
    {
        var segments = network.GetSegments();
        var dist = network.GetCities().ToDictionary(c => c.Id, _ => double.PositiveInfinity);
        var prev = new Dictionary<string, Step>();
        var open = new HashSet<string>(dist.Keys);
        dist[from] = 0;
        while (open.Count > 0)
        {
            var u = open.MinBy(k => dist[k])!;
            open.Remove(u);
            if (u == to || double.IsInfinity(dist[u])) break;
            foreach (var e in segments)
            {
                var other = e.FromCityId == u ? e.ToCityId : e.ToCityId == u ? e.FromCityId : null;
                if (other is null || !open.Contains(other)) continue;
                var d = dist[u] + e.Km / e.AvgSpeedKmh;
                if (d < dist[other]) { dist[other] = d; prev[other] = new Step(e, u, other); }
            }
        }
        var path = new List<Step>();
        for (var c = to; prev.TryGetValue(c, out var s); c = s.From) path.Insert(0, s);
        return path;
    }

    private PathEval EvaluatePath(List<Step> path, int departure, VehicleType vehicle)
    {
        double t = departure, wsum = 0, km = 0, fw = 0, fh = 0, fi = 0, ft = 0;
        var legs = new List<LegEval>();
        LegEval? worst = null;
        foreach (var step in path)
        {
            var dur = step.Segment.Km / step.Segment.AvgSpeedKmh;
            var r = SegmentRiskAt(step.Segment, t + dur / 2, vehicle);
            var leg = new LegEval(step.Segment, step.From, step.To, t, r);
            legs.Add(leg);
            t += dur;
            km += step.Segment.Km; wsum += r.Total * step.Segment.Km;
            fw += r.Weather * step.Segment.Km; fh += r.History * step.Segment.Km; fi += r.Incidents * step.Segment.Km; ft += r.Traffic * step.Segment.Km;
            if (worst is null || r.Total > worst.Risk.Total) worst = leg;
        }
        var avg = km > 0 ? wsum / km : 0;
        var score = (int)Math.Round(0.6 * (worst?.Risk.Total ?? 0) + 0.4 * avg);
        var f = km > 0 ? new Factors(fw / km, fh / km, fi / km, ft / km) : new Factors(0, 0, 0, 0);
        return new PathEval(legs, score, worst, km, t - departure, f);
    }

    private SegmentRisk SegmentRiskAt(RoadSegment e, double hour, VehicleType vehicle)
    {
        var hh = (((int)Math.Floor(hour)) % 24 + 24) % 24;
        var fog = weather.GetFogIndex(hh);
        var vis = (int)Math.Max(35, Math.Round(10000 * Math.Exp(-5.4 * e.FogExposure * fog)));
        double w = vis < 50 ? 96 : vis < 100 ? 88 : vis < 200 ? 74 : vis < 500 ? 50 : vis < 1000 ? 30 : 10;
        var hist = e.History * 100;
        double inc = e.Incident is not null ? 70 : e.Repair is not null ? 45 : 5;
        double traffic = (hh is >= 7 and <= 9) || (hh is >= 17 and <= 19) ? 55 : 20;
        var total = (int)Math.Min(100, Math.Round(0.45 * (w + VehicleBonus[vehicle]) + 0.25 * hist + 0.2 * inc + 0.1 * traffic));
        return new SegmentRisk(total, w, hist, inc, traffic, vis);
    }
}
