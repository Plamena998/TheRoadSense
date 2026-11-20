# RoadSense

ASP.NET Core MVC (.NET 10) приложение с петте страници от concept-art-а. Данните идват от SQL Server през EF Core. При стартиране приложението само прилага чакащите миграции и зарежда празните таблици от CSV файловете в `dataSource/seed`.

## Стартиране

Нужни са .NET 10 SDK и достъпен SQL Server (низът за връзка е в `ConnectionStrings:RoadSense`). Отваря се с Visual Studio 2026 или `dotnet run`.

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
  RoadSense.Infrastructure.Sql/
    RoadSenseDbContext.cs, Entities.cs   EF модел и таблици; Migrations/ — EF миграции
    SqlDataStore.cs                 реализира всички интерфейси за данни върху базата
    DatabaseInitializer.cs          миграции + зареждане на празните таблици от CSV (по един метод на таблица)
    CsvData.cs                      четене и износ на CSV през EF метаданните
  RoadSense.DbTool/                 миграции и зареждане от командния ред; „export“ записва таблиците в CSV
  RoadSense.Web/
    Program.cs                      DI: SqlDataStore, миграции и зареждане при старт
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
| Първа помощ | всички стъпки и раздели от базата | — |

Допълнително: `GET /api/stats/roads`, `GET /api/stats/ranking`, `GET /api/ticker`.

## Данни и база

Началните данни са CSV файлове в `dataSource/seed` — по един на таблица, с име като таблицата (`Cities.csv`, `RoadSegments.csv`, …). Първият ред е заглавие с имената на колоните, празна стойност е NULL.

При стартиране на `RoadSense.Web` (или `dotnet run --project src/RoadSense.DbTool`):
1. ако има неприложени EF миграции, се прилагат (базата се създава, ако липсва);
2. всяка таблица, която е празна, се зарежда от своя CSV файл. Таблици с данни не се пипат.

Файловете се копират в `SeedData/` до изпълнимия файл при build. Друга папка се задава с `Database:SeedPath`.
Текущото съдържание на базата се записва обратно в CSV с `dotnet run --project src/RoadSense.DbTool -- export`.

## Местоположение

Страницата „Времето около мен“ пита браузъра за GPS. Ако потребителят откаже, GPS не отговори до 8 s или браузърът не го поддържа, районът се определя по IP адреса на мрежата (Wi-Fi / мобилни данни, ±5–25 км) чрез `GET /api/location/network`. Ако и това не стане, се показва районът по подразбиране (първото място от таблицата `WeatherLocations`). Бутонът „Използвай GPS“ опитва отново.

Местоположението по IP идва от услугата ipwho.is (без ключ). На localhost реалната услуга не може да определи място и се показва районът по подразбиране — проверява се на публичен сървър. Зад reverse proxy адресът се взима от `X-Forwarded-For`.

## SpeedAdvisor

Препоръката е по-ниската от: (1) скоростта, при която спирачният път `v·t + v²/(2μg)` е по-къс от видимостта, и (2) ограничението × множителите (лек/силен дъжд, сняг, лед, видимост под 200/50 м, пориви). Закръгля се надолу до 10 км/ч и не надвишава ограничението. Всички коефициенти са в `appsettings.json` → `SpeedAdvisor` и са илюстративни — да се сверят с реални препоръки.

## Бележки

- Шрифтовете (Unbounded, Onest, JetBrains Mono) се зареждат от Google Fonts.
- Анимациите следват принципите от `emil-design-eng` и се изключват при `prefers-reduced-motion`.
