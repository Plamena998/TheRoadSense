using Microsoft.AspNetCore.Mvc;
using RoadSense.Application;
using RoadSense.Application.Services;
using RoadSense.Web.Models;

namespace RoadSense.Web.Controllers;

// MVC страниците. Статичното съдържание се рендерира на сървъра (Razor),
// а интерактивните части (маршрут, графики, календар) се зареждат от /api/... с JavaScript.

public class HomeController(IContentRepository content) : Controller
{
    public IActionResult Index() => View(new HomeViewModel(content.GetHome(), content.GetTicker()));

    [Route("/Home/Error")]
    public IActionResult Error() => View();
}

public class RouteController(RouteRiskService routes) : Controller
{
    public IActionResult Index(string from = "SOF", string to = "BGS", int hour = 6, string vehicle = "Car")
    {
        var cities = routes.GetNetwork().Cities.Where(c => c.Selectable).ToList();
        return View(new RouteViewModel(cities, from, to, hour, vehicle));
    }
}

public class WeatherController : Controller
{
    public IActionResult Index() => View();
}

public class StatsController(StatsService stats, TimeProvider clock) : Controller
{
    public IActionResult Index()
    {
        var now = clock.GetLocalNow();
        return View(new StatsViewModel(stats.GetRoads(), stats.GetRanking(), now.Year, now.Month));
    }
}

public class FirstAidController(IContentRepository content) : Controller
{
    public IActionResult Index() => View(content.GetFirstAid());
}
