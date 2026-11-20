namespace RoadSense.Application.Hazard;

/// <summary>Опасни метеорологични условия за пътя. Тежестта на всяко е в <see cref="HazardWeights"/>.</summary>
public enum WeatherCondition
{
    LightRain,
    Rain,
    HeavyRain,
    LightSnow,
    Snow,
    HeavySnow,
    Fog,
    DenseFog,

    /// <summary>Поледица (черен лед, скреж) без валеж.</summary>
    Ice,

    /// <summary>Фрезиращ дъжд – дъжд при температура ≤ 0°C.</summary>
    FreezingRain,
    StrongWind,
    Storm
}
