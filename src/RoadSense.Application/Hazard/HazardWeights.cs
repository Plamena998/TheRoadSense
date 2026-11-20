namespace RoadSense.Application.Hazard;

/// <summary>Ред от таблицата: намаление на ограничението в проценти и абсолютен таван за условието.</summary>
public sealed record HazardRule(WeatherCondition Condition, int ReductionPercent, int? MaxSpeedKmh, string Advice);

/// <summary>
/// Тежест на всяко метеорологично условие (намаление на скоростта в %), от която
/// <see cref="HazardIndexCalculator"/> смята индекса на опасност. Стойностите са експертни и се настройват само тук.
/// Копие за четене: dataSource/processed/speed_reduction_rules.csv.
/// </summary>
public static class HazardWeights
{
    public static readonly IReadOnlyList<HazardRule> Rules =
    [
        new(WeatherCondition.LightRain,    10, null, "Слаб дъжд – увеличете дистанцията."),
        new(WeatherCondition.Rain,         20, 110,  "Дъжд – хлъзгава настилка, възможен аквапланинг."),
        new(WeatherCondition.HeavyRain,    30, 90,   "Пороен дъжд – ограничена видимост и аквапланинг."),
        new(WeatherCondition.LightSnow,    20, 80,   "Слаб сняг – внимание на спирачния път."),
        new(WeatherCondition.Snow,         35, 60,   "Сняг – използвайте зимни гуми, карайте плавно."),
        new(WeatherCondition.HeavySnow,    50, 40,   "Силен снеговалеж – карайте само при нужда."),
        new(WeatherCondition.Fog,          30, 80,   "Мъгла – включете мъгливите светлини."),
        new(WeatherCondition.DenseFog,     60, 50,   "Гъста мъгла – видимост под 50 м."),
        new(WeatherCondition.Ice,          50, 50,   "Опасност от поледица – не спирайте рязко."),
        new(WeatherCondition.FreezingRain, 60, 40,   "Фрезиращ дъжд – пътят е заледен."),
        new(WeatherCondition.StrongWind,   15, 100,  "Силен вятър – внимавайте на мостове и открити участъци."),
        new(WeatherCondition.Storm,        30, 70,   "Буря – рискът от силни пориви е висок."),
    ];

    /// <summary>Допълнително намаление (%) за всяко следващо едновременно условие.</summary>
    public const int ExtraPerAdditionalCondition = 5;

    /// <summary>Допълнително намаление (%) нощем при лоши условия.</summary>
    public const int NightExtra = 5;

    public const int MaxTotalReductionPercent = 70;
    public const int MinSpeedKmh = 30;

    public static HazardRule For(WeatherCondition c) => Rules.First(r => r.Condition == c);
}
