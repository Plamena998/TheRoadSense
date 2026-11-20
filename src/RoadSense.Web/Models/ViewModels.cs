using RoadSense.Application;
using RoadSense.Domain;

namespace RoadSense.Web.Models;

public sealed record HomeViewModel(HomeContent Content, IReadOnlyList<TickerItem> Ticker);

public sealed record RouteViewModel(IReadOnlyList<City> Cities, string From, string To, int Hour, string Vehicle);

public sealed record StatsViewModel(IReadOnlyList<RoadStats> Roads, IReadOnlyList<RankingDto> Ranking, int Year, int Month);

/// <summary>Активната страница в менюто (ViewData["Page"]).</summary>
public static class Pages
{
    public const string Home = "home", Route = "route", Weather = "weather", Stats = "stats", FirstAid = "firstaid";
}
