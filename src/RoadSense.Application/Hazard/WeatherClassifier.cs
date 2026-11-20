namespace RoadSense.Application.Hazard;

/// <summary>
/// Определя активните опасни условия от измерванията. Данните от Open-Meteo не съдържат наблюдавана
/// мъгла и поледица (няма WMO кодове 45/48 и 56/57/66/67), затова те се извеждат с правила от температура,
/// влажност, точка на оросяване и вятър. Когато има измерена видимост или температура на настилката,
/// те имат предимство. Праговете са експертна оценка, не обучени стойности.
/// </summary>
public static class WeatherClassifier
{
    public static IReadOnlyList<WeatherCondition> Classify(WeatherObservation w)
    {
        var result = new List<WeatherCondition>();
        var code = w.WeatherCode;

        var freezingCode = code is 56 or 57 or 66 or 67;
        var snowCode = code is 71 or 73 or 75 or 77 or 85 or 86;
        var rainCode = code is (>= 51 and <= 55) or (>= 61 and <= 65) or (>= 80 and <= 82);
        var noPrecip = w.PrecipitationMmPerHour < 0.1 && w.SnowfallCmPerHour < 0.1;

        // Сняг
        var snowing = snowCode || w.SnowfallCmPerHour >= 0.1;
        if (snowing)
        {
            if (w.SnowfallCmPerHour >= 2.0 || code == 75) result.Add(WeatherCondition.HeavySnow);
            else if (w.SnowfallCmPerHour >= 0.5 || code == 73) result.Add(WeatherCondition.Snow);
            else result.Add(WeatherCondition.LightSnow);
        }

        // Дъжд / фрезиращ дъжд (валеж, който не е сняг)
        var raining = !snowing && (rainCode || freezingCode || w.PrecipitationMmPerHour >= 0.1);
        if (raining)
        {
            if (freezingCode || w.TemperatureC <= 0.0 || w.SurfaceTempC is <= 0.0)
                result.Add(WeatherCondition.FreezingRain);
            else if (w.PrecipitationMmPerHour >= 7.6 || code is 65 or 82)
                result.Add(WeatherCondition.HeavyRain);
            else if (w.PrecipitationMmPerHour >= 2.5 || code is 63 or 81)
                result.Add(WeatherCondition.Rain);
            else
                result.Add(WeatherCondition.LightRain);
        }

        // Мъгла: ако има измерена видимост, ползваме нея; иначе – извод от влажност/оросяване
        if (w.VisibilityM is { } vis)
        {
            if (vis < 100) result.Add(WeatherCondition.DenseFog);
            else if (vis < 1000) result.Add(WeatherCondition.Fog);
        }
        else
        {
            var spread = w.TemperatureC - DewPointC(w.TemperatureC, w.HumidityPercent);
            if (code == 48 || (noPrecip && w.HumidityPercent >= 98 && spread <= 0.5 && w.WindKmh <= 5))
                result.Add(WeatherCondition.DenseFog);
            else if (code == 45 || (noPrecip && w.HumidityPercent >= 95 && spread <= 1.5 && w.WindKmh <= 10))
                result.Add(WeatherCondition.Fog);
        }

        // Поледица без валеж: ако има температура на настилката, ползваме нея
        var surfaceFrozen = w.SurfaceTempC is <= 0.0;
        var surfaceNearFreezing = w.SurfaceTempC is <= 2.0 && w.HumidityPercent >= 85;
        var iceFromAir = w.SurfaceTempC is null
            && ((w.TemperatureC is <= 1.0 and >= -8.0 && w.HumidityPercent >= 90)
                || (w.TemperatureC <= 0.0 && w.SnowDepthM > 0.0));

        if (noPrecip && !freezingCode && (surfaceFrozen || surfaceNearFreezing || iceFromAir))
            result.Add(WeatherCondition.Ice);

        // Вятър
        if (w.GustKmh >= 90 || code is 96 or 99) result.Add(WeatherCondition.Storm);
        else if (w.GustKmh >= 60) result.Add(WeatherCondition.StrongWind);

        return result;
    }

    /// <summary>Точка на оросяване по формулата на Магнус.</summary>
    public static double DewPointC(double tempC, double humidityPercent)
    {
        const double a = 17.62, b = 243.12;
        var rh = Math.Clamp(humidityPercent, 1, 100) / 100.0;
        var g = Math.Log(rh) + a * tempC / (b + tempC);
        return b * g / (a - g);
    }
}