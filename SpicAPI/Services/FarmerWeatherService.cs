using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using SPIC.Core.DTOs;
using SPIC.Core.FarmerPortal;

namespace SpicAPI.Services
{
    /// <summary>
    /// Farmer portal Task 3: the 7-day forecast from Open-Meteo (free endpoint, or the commercial customer endpoint
    /// with FarmerPortal:WeatherApiKey). The ONLY thing sent is the
    /// farm point rounded to 0.1 degree (about 11 km; <see cref="FarmerWeatherRules.RoundForForecast"/>):
    /// no name, phone or user id. 8 s timeout (named HttpClient "OpenMeteo"); an answer is cached for one
    /// hour per rounded point, a failure for 5 minutes (so a broken service is not hammered). Any failure
    /// gives <see cref="FarmerWeatherStatus.Unavailable"/> and the page leaves the section out.
    /// Points are never logged.
    /// </summary>
    public interface IFarmerWeatherService
    {
        Task<FarmerWeatherDto> ForecastAsync(double? lat, double? lng, CancellationToken ct);
    }

    public sealed class FarmerWeatherService : IFarmerWeatherService
    {
        public const string HttpClientName = "OpenMeteo";
        public const string DefaultUrl = "https://api.open-meteo.com/v1/forecast";
        /// <summary>The commercial endpoint, used when FarmerPortal:WeatherApiKey is set (final fix wave, I6).</summary>
        public const string CustomerUrl = "https://customer-api.open-meteo.com/v1/forecast";
        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(8);
        public static readonly TimeSpan CacheFor = TimeSpan.FromHours(1);
        public static readonly TimeSpan FailureCacheFor = TimeSpan.FromMinutes(5);

        private readonly IHttpClientFactory _http;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly ILogger<FarmerWeatherService> _log;

        public FarmerWeatherService(IHttpClientFactory http, IMemoryCache cache, IConfiguration config, ILogger<FarmerWeatherService> log)
        {
            _http = http;
            _cache = cache;
            _config = config;
            _log = log;
        }

        public async Task<FarmerWeatherDto> ForecastAsync(double? lat, double? lng, CancellationToken ct)
        {
            if (!FarmerGeo.IsValid(lat, lng))
                return new FarmerWeatherDto { Status = FarmerWeatherStatus.NoLocation };

            var (rLat, rLng) = FarmerWeatherRules.RoundForForecast(lat!.Value, lng!.Value);
            var key = string.Create(CultureInfo.InvariantCulture, $"fp:weather:{rLat:0.0}:{rLng:0.0}");
            if (_cache.TryGetValue(key, out FarmerWeatherDto? cached) && cached is not null)
                return Copy(cached);

            var fresh = await FetchAsync(rLat, rLng, ct);
            _cache.Set(key, fresh, fresh.Status == FarmerWeatherStatus.Ok ? CacheFor : FailureCacheFor);
            return Copy(fresh);
        }

        private async Task<FarmerWeatherDto> FetchAsync(double lat, double lng, CancellationToken ct)
        {
            // The URL may carry the commercial key: it is never logged (the "OpenMeteo" client has no HTTP
            // loggers, Program.cs, and failures below log only the status code or the exception type).
            var url = BuildUrl(_config["FarmerPortal:WeatherUrl"], _config["FarmerPortal:WeatherApiKey"], lat, lng);

            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeout.CancelAfter(Timeout);
                var client = _http.CreateClient(HttpClientName);
                using var response = await client.GetAsync(url, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _log.LogWarning("Open-Meteo forecast failed with HTTP {Status}", (int)response.StatusCode);
                    return Unavailable();
                }

                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);
                var days = Parse(doc.RootElement);
                if (days.Count == 0)
                {
                    _log.LogWarning("Open-Meteo forecast had no daily rows");
                    return Unavailable();
                }

                return new FarmerWeatherDto
                {
                    Status = FarmerWeatherStatus.Ok,
                    Days = days,
                    Advisories = FarmerWeatherRules.Advisories(days),
                    FetchedAtUtc = DateTime.UtcNow
                };
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Timeout, DNS, TLS or bad JSON: the page simply leaves the weather out.
                _log.LogWarning("Open-Meteo forecast failed: {Error}", ex.GetType().Name);
                return Unavailable();
            }
        }

        /// <summary>
        /// The forecast URL. FarmerPortal:WeatherUrl (a base URL) wins when set; otherwise the free endpoint, or the
        /// customer endpoint when FarmerPortal:WeatherApiKey is set. A key is appended as &amp;apikey= (escaped).
        /// </summary>
        public static string BuildUrl(string? configuredBaseUrl, string? apiKey, double lat, double lng)
        {
            var hasKey = !string.IsNullOrWhiteSpace(apiKey);
            var baseUrl = !string.IsNullOrWhiteSpace(configuredBaseUrl) ? configuredBaseUrl.Trim()
                : hasKey ? CustomerUrl : DefaultUrl;
            var url = FormattableString.Invariant($"{baseUrl}?latitude={lat:0.0}&longitude={lng:0.0}")
                + "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,wind_speed_10m_max"
                + FormattableString.Invariant($"&timezone=Asia%2FKolkata&forecast_days={FarmerWeatherRules.ForecastDays}");
            return hasKey ? url + "&apikey=" + Uri.EscapeDataString(apiKey!.Trim()) : url;
        }

        /// <summary>Reads Open-Meteo's "daily" arrays (any of them may be missing or hold nulls).</summary>
        public static List<FarmerWeatherDayDto> Parse(JsonElement root)
        {
            var days = new List<FarmerWeatherDayDto>();
            if (!root.TryGetProperty("daily", out var daily) || daily.ValueKind != JsonValueKind.Object) return days;
            if (!daily.TryGetProperty("time", out var time) || time.ValueKind != JsonValueKind.Array) return days;

            static double? Num(JsonElement daily, string name, int i)
            {
                if (!daily.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array || i >= arr.GetArrayLength()) return null;
                var v = arr[i];
                return v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && double.IsFinite(d) ? d : null;
            }

            var count = Math.Min(time.GetArrayLength(), FarmerWeatherRules.ForecastDays);
            for (var i = 0; i < count; i++)
            {
                var date = time[i].ValueKind == JsonValueKind.String ? time[i].GetString() : null;
                if (string.IsNullOrWhiteSpace(date)) continue;
                days.Add(new FarmerWeatherDayDto
                {
                    Date = date,
                    WeatherCode = Num(daily, "weather_code", i) is { } c ? (int)c : null,
                    MaxTempC = Round(Num(daily, "temperature_2m_max", i)),
                    MinTempC = Round(Num(daily, "temperature_2m_min", i)),
                    RainMm = Round(Num(daily, "precipitation_sum", i)),
                    RainChancePct = Num(daily, "precipitation_probability_max", i) is { } p ? (int)Math.Round(p) : null,
                    WindKmh = Round(Num(daily, "wind_speed_10m_max", i))
                });
            }
            return days;
        }

        private static double? Round(double? v) => v is { } d ? Math.Round(d, 1) : null;

        private static FarmerWeatherDto Unavailable() => new() { Status = FarmerWeatherStatus.Unavailable };

        // The cached object is shared; hand out a copy so nobody can change it.
        private static FarmerWeatherDto Copy(FarmerWeatherDto w) => new()
        {
            Status = w.Status,
            FetchedAtUtc = w.FetchedAtUtc,
            Source = w.Source,
            Days = w.Days.Select(d => new FarmerWeatherDayDto
            {
                Date = d.Date, WeatherCode = d.WeatherCode, MaxTempC = d.MaxTempC, MinTempC = d.MinTempC,
                RainMm = d.RainMm, RainChancePct = d.RainChancePct, WindKmh = d.WindKmh
            }).ToList(),
            Advisories = w.Advisories.Select(a => new FarmerAdvisoryDto
            {
                Code = a.Code, Level = a.Level, Icon = a.Icon, Title = a.Title, Text = a.Text
            }).ToList()
        };
    }
}
