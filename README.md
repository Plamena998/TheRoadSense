# RoadSense — фронтенд с mockup данни

ASP.NET Core MVC (.NET 10) приложение с петте страници от concept-art-а. Данните идват от JSON файлове (mockup), а не от базата, така че фронтендът може да се развива и тества веднага. Когато базата е готова, се сменя само източникът на данни — изгледите, CSS, JS и API-то остават същите.

## Стартиране

Нужно е .NET 10 SDK (без NuGet пакети). Отваря се с Visual Studio 2026 или `dotnet run`.

```bash
cd src/RoadSense.Web
dotnet run
```

Отвори http://localhost:5113. Във Visual Studio / Rider: стартирай проекта `RoadSense.Web`.

В режим Development часът е фиксиран на **19.11.2026, 07:00** (`DemoNow` в `appsettings.Development.json`), за да се вижда сутрешната мъгла на презентацията. Изтрий стойността, за да се ползва реалният час.

## Структура

```
RoadSense.sln
src/
  RoadSense.Domain/                 модели (City, RoadSegment, WeatherHour, RoadStats, …) и enum-и
  RoadSense.Application/
    Abstractions.cs                 интерфейсите за данни (IRoadNetworkRepository, IWeatherProvider, IStatsRepository, …)
    Dtos.cs                         обектите, които API-то връща
    Services/RouteRiskService.cs    маршрут (Дейкстра) + оценка на риска 0–100 по участъци и часове
    Services/SpeedAdvisor.cs        препоръчителна скорост: физика + множители, настройки от appsettings
    Services/StatsService.cs        статистика по път, класация, черен календар; WeatherService
  RoadSense.Infrastructure.Mock/
    MockData/*.json                 ← mockup данните — редактирай тук
    MockDataStore.cs                чете JSON-а и реализира всички интерфейси
  RoadSense.Web/
    Program.cs                      DI: "DataSource": "Mock" → MockDataStore
    Controllers/PagesControllers.cs MVC страниците
    Controllers/Api/ApiControllers.cs  JSON API за фронтенда
    Views/Shared/_Layout.cshtml     навигация, шрифтове, общ footer
    Views/{Home,Route,Weather,Stats,FirstAid}/Index.cshtml
    wwwroot/css/site.css            дизайн токени и общи компоненти (бутони, чипове, карти, падащи менюта, скрол барове…)
    wwwroot/css/<страница>.css      стиловете на всяка страница (+ weather-widget.css)
    wwwroot/js/core.js              RS.api, RS.dropdown, RS.reveal, цветове на риска, форматиране
    wwwroot/js/<страница>.js        логиката на всяка страница
    wwwroot/lib/lenis               плавен скрол на началната страница (MIT)
```

## Как се свързват данните

| Страница | Сървър (Razor) | JavaScript → API |
|---|---|---|
| Начало | числата в историята, лентата на живо | — |
| Маршрут | списък с градовете | `GET /api/network`, `GET /api/route?from=SOF&to=BGS&hour=23&vehicle=Truck` |
| Времето около мен | местоположение | `GET /api/weather/today`, `GET /api/location/gps?lat=42.69&lon=23.32&accuracy=40`, `GET /api/location/network`, `GET /api/speed?hour=7&vehicle=Car&road=Motorway&surface=auto` |
| Статистика | пътищата, класацията (таблицата) | `GET /api/stats/road/A1?period=all`, `GET /api/stats/calendar?year=2026&month=11` |
| Първа помощ | всички стъпки и раздели от `content-firstaid.json` | — |

Допълнително: `GET /api/stats/roads`, `GET /api/stats/ranking`, `GET /api/ticker`.

## Mockup данните (`src/RoadSense.Infrastructure.Mock/MockData`)

| Файл | Съдържание |
|---|---|
| `cities.json` | възлите на пътната мрежа (координати върху схемата 800×440); `selectable` — дали са в падащото меню |
| `segments.json` | участъците: път, км, излагане на мъгла, история на ПТП, текущ инцидент/ремонт |
| `speed-limits.json` | ограничения по тип път и превозно средство (таблицата, която ще е в базата) |
| `weather.json` | местоположение и почасов профил (мъгла, температура, валежи, изгрев/залез) |
| `places.json` | населени места с координати — най-близкото до потребителя се показва като „район“ |
| `ip-location.json` | какво връща „местоположението по мрежата“ в mock режим (сменете координатите, за да симулирате друг град) |
| `stats-roads.json` | пътищата за статистиката: черни точки, области, причини |
| `stats-weights.json` | разпределение на загиналите по месеци и часове |
| `ranking.json` | класацията на опасните пътища |
| `calendar.json` | черният календар — реални събития от 2026 г. с източник |
| `content-home.json` | лентата на живо и числата в историята на началната страница |
| `content-firstaid.json` | стъпките, разделите и списъкът за колата |

Файловете се копират при build и се четат наново, ако се променят (без рестарт при `dotnet watch`).

## Преминаване към базата

1. Създай `RoadSense.Infrastructure.Sql` (EF Core) с класове, които реализират интерфейсите от `RoadSense.Application/Abstractions.cs`.
2. В `Program.cs` добави клон за `"DataSource": "Sql"`, който регистрира тези класове вместо `MockDataStore`.
3. В `appsettings.json` смени `"DataSource": "Sql"`.

## Местоположение

Страницата „Времето около мен“ пита браузъра за GPS. Ако потребителят откаже, GPS не отговори до 8 s или браузърът не го поддържа, районът се определя по IP адреса на мрежата (Wi-Fi / мобилни данни, ±5–25 км) чрез `GET /api/location/network`. Ако и това не стане, се показва районът по подразбиране от `weather.json`. Бутонът „Използвай GPS“ опитва отново.

В `appsettings.json`: `"IpGeolocation": "Mock"` връща отговора от `ip-location.json`; `"IpWhoIs"` ползва реалната услуга ipwho.is (без ключ). На localhost реалната услуга не може да определи място и се показва районът по подразбиране — проверява се на публичен сървър. Зад reverse proxy адресът се взима от `X-Forwarded-For`.

Фронтендът не се променя. Ограниченията на скоростта по тип път вече се четат през `ISpeedLimitRepository` — точно както е договорено за базата.

## SpeedAdvisor

Препоръката е по-ниската от: (1) скоростта, при която спирачният път `v·t + v²/(2μg)` е по-къс от видимостта, и (2) ограничението × множителите (лек/силен дъжд, сняг, лед, видимост под 200/50 м, пориви). Закръгля се надолу до 10 км/ч и не надвишава ограничението. Всички коефициенти са в `appsettings.json` → `SpeedAdvisor` и са илюстративни — да се сверят с реални препоръки.

## Бележки

- Данните са демо, освен: числата за първото полугодие на 2026 г. (ДАБДП) и трите събития в черния календар.
- Шрифтовете (Unbounded, Onest, JetBrains Mono) се зареждат от Google Fonts.
- Анимациите следват принципите от `emil-design-eng` и се изключват при `prefers-reduced-motion`.
