using SPIC.Core.DTOs;

namespace SPIC.Core.FarmerPortal;

/// <summary>
/// Farmer portal Task 3 (docs/farmer-portal.md): the 7-day forecast for "Trends in my area" and the
/// home weather strip. Open-Meteo is called with the farm point rounded to 0.1 degree (about 11 km), so
/// the forecast service never learns the exact farm. The farm advisories are simple, fixed rules on the
/// forecast, pinned by SPIC.RegressionHarness (FarmerPortalTrendsTests).
/// </summary>
public static class FarmerWeatherRules
{
    /// <summary>Forecast days asked from Open-Meteo.</summary>
    public const int ForecastDays = 7;

    /// <summary>Rain over the next 3 days above this (mm) is "heavy rain": no top-dressing or spraying.</summary>
    public const double HeavyRainMm3Days = 50;

    /// <summary>A day with at least this much rain (mm) or this chance (%) in the next 2 days is "rain soon".</summary>
    public const double RainSoonMm = 10;
    public const int RainSoonChancePct = 70;

    /// <summary>A day this hot (deg C max) in the next 3 days gets the heat advice.</summary>
    public const double HotDayC = 38;

    /// <summary>Wind this strong (km/h max) in the next 2 days: do not spray.</summary>
    public const double StrongWindKmh = 30;

    /// <summary>A week with less rain than this (mm) and no day above <see cref="DryWeekMaxChancePct"/> is dry.</summary>
    public const double DryWeekMm = 5;
    public const int DryWeekMaxChancePct = 40;

    /// <summary>Two days each below these limits (and no warning) make a good spraying / top-dressing window.</summary>
    public const double CalmDryMm = 1;
    public const int CalmDryChancePct = 30;
    public const double CalmWindKmh = 20;

    /// <summary>At most this many advisories are shown.</summary>
    public const int MaxAdvisories = 3;

    /// <summary>
    /// The point sent to Open-Meteo: rounded to 0.1 degree (about 11 km). The same value is the cache key,
    /// so farmers of one village share one forecast.
    /// </summary>
    public static (double Lat, double Lng) RoundForForecast(double lat, double lng) =>
        (Math.Round(lat, 1, MidpointRounding.AwayFromZero), Math.Round(lng, 1, MidpointRounding.AwayFromZero));

    /// <summary>Plain-words label and Bootstrap icon for a WMO weather code (Open-Meteo "weather_code").</summary>
    public static (string Label, string Icon) Describe(int? code) => code switch
    {
        0 => ("Sunny", "bi-sun"),
        1 or 2 => ("Partly cloudy", "bi-cloud-sun"),
        3 => ("Cloudy", "bi-cloud"),
        45 or 48 => ("Fog", "bi-cloud-fog2"),
        >= 51 and <= 57 => ("Drizzle", "bi-cloud-drizzle"),
        65 or 67 or 82 => ("Heavy rain", "bi-cloud-rain-heavy"),
        >= 61 and <= 67 => ("Rain", "bi-cloud-rain"),
        >= 80 and <= 82 => ("Showers", "bi-cloud-rain"),
        >= 71 and <= 77 or 85 or 86 => ("Snow", "bi-cloud-snow"),
        >= 95 and <= 99 => ("Thunderstorm", "bi-cloud-lightning-rain"),
        _ => ("Weather", "bi-cloud-sun")
    };

    /// <summary>
    /// The farm advisories for a forecast (day 0 = today), most important first, at most
    /// <see cref="MaxAdvisories"/>. Missing values count as 0 (no rain, no wind) except temperatures,
    /// which are skipped.
    /// </summary>
    public static List<FarmerAdvisoryDto> Advisories(IReadOnlyList<FarmerWeatherDayDto> days)
    {
        var list = new List<FarmerAdvisoryDto>();
        if (days is null || days.Count == 0) return list;

        static double Rain(FarmerWeatherDayDto d) => d.RainMm is { } r && double.IsFinite(r) && r > 0 ? r : 0;
        static int Chance(FarmerWeatherDayDto d) => d.RainChancePct ?? 0;
        static double Wind(FarmerWeatherDayDto d) => d.WindKmh is { } w && double.IsFinite(w) ? w : 0;

        var next3 = days.Take(3).ToList();
        var next2 = days.Take(2).ToList();

        var rain3 = next3.Sum(Rain);
        var heavy = rain3 > HeavyRainMm3Days;
        var rainSoon = !heavy && next2.Any(d => Rain(d) >= RainSoonMm || Chance(d) >= RainSoonChancePct);
        var hot = next3.Any(d => d.MaxTempC is { } t && t >= HotDayC);
        var windy = next2.Any(d => Wind(d) >= StrongWindKmh);
        var dryWeek = days.Sum(Rain) < DryWeekMm && days.All(d => Chance(d) < DryWeekMaxChancePct);
        var calm = next2.Count == 2 && !heavy && !rainSoon && !windy
                   && next2.All(d => Rain(d) < CalmDryMm && Chance(d) < CalmDryChancePct && Wind(d) < CalmWindKmh);

        if (heavy)
            list.Add(new FarmerAdvisoryDto
            {
                Code = "heavy-rain",
                Level = FarmerAdvisoryLevel.Caution,
                Icon = "bi-cloud-rain-heavy",
                Title = $"Heavy rain in the next 3 days (about {Math.Round(rain3):0} mm)",
                Text = "Please avoid fertilizer top-dressing and spraying before the rain: the rain washes them away. Keep the field drains open."
            });
        if (rainSoon)
            list.Add(new FarmerAdvisoryDto
            {
                Code = "rain-soon",
                Level = FarmerAdvisoryLevel.Caution,
                Icon = "bi-cloud-drizzle",
                Title = "Rain is likely in the next 2 days",
                Text = "Spray only when about 6 dry hours are expected, so the rain does not wash it off. Top-dress after the rain, when the soil is moist."
            });
        if (hot)
            list.Add(new FarmerAdvisoryDto
            {
                Code = "heat",
                Level = FarmerAdvisoryLevel.Caution,
                Icon = "bi-thermometer-sun",
                Title = "Very hot days ahead",
                Text = "Water your crop in the early morning or evening. Do not spray in the afternoon heat."
            });
        if (windy)
            list.Add(new FarmerAdvisoryDto
            {
                Code = "wind",
                Level = FarmerAdvisoryLevel.Caution,
                Icon = "bi-wind",
                Title = "Strong wind in the next 2 days",
                Text = "Avoid spraying in strong wind: the spray drifts away from your crop."
            });
        if (dryWeek)
            list.Add(new FarmerAdvisoryDto
            {
                Code = "dry-week",
                Level = FarmerAdvisoryLevel.Info,
                Icon = "bi-droplet-half",
                Title = "A dry week ahead",
                Text = "Plan your irrigation. Apply fertilizer when the soil is moist, or water soon after."
            });
        if (calm)
            list.Add(new FarmerAdvisoryDto
            {
                Code = "spray-window",
                Level = FarmerAdvisoryLevel.Good,
                Icon = "bi-check2-circle",
                Title = "Good days for field work",
                Text = "The next 2 days look dry and calm: a good time for spraying and fertilizer top-dressing."
            });

        return list.Take(MaxAdvisories).ToList();
    }
}
