using System.Net.Http.Json;
using System.Text.Json;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// The ONLY HTTP surface of the JMDO Attendance module (<c>api/JMDOAttendance/...</c>; the route
/// list lives at the top of SPIC.Core/DTOs/JMDOAttendanceDtos.cs). Mirrors SasApi's conventions:
/// every call is defensive - a network problem or a non-2xx answer raises one toast and returns
/// null, so the page never has to catch anything.
///
/// Registered with <c>services.AddScoped&lt;JMDOAttendanceApi&gt;()</c> in both hosts
/// (SPIC.MauiBlazorApp.Web/Program.cs and SPIC.MauiBlazorApp/MauiProgram.cs).
/// </summary>
public sealed class JMDOAttendanceApi
{
    private const string Root = "api/JMDOAttendance";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ToastService _toast;

    public JMDOAttendanceApi(HttpClient http, ToastService toast)
    {
        _http = http;
        _toast = toast;
    }

    /// <summary>The caller's own attendance row for today, or null if none has been marked yet.</summary>
    public async Task<JMDOAttendanceDto?> GetTodayAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync($"{Root}/today", ct);
            if (!response.IsSuccessStatusCode)
            {
                _toast.Error("Could not load today's attendance.");
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body) || body == "null") return null;

            return JsonSerializer.Deserialize<JMDOAttendanceDto>(body, Json);
        }
        catch (Exception)
        {
            _toast.Error("Could not reach the server to load today's attendance.");
            return null;
        }
    }

    /// <summary>
    /// Creates or updates the caller's attendance for the given date and sets them On Duty.
    /// <paramref name="latitude"/>/<paramref name="longitude"/> are the GPS fix captured at the
    /// moment Update Attendance was clicked; the server rejects the request without them.
    /// </summary>
    public async Task<JMDOAttendanceDto?> UpdateAttendanceAsync(
        DateTime date, string visitArea, JMDOAttendanceStatus status,
        double latitude, double longitude, CancellationToken ct = default)
    {
        try
        {
            var dto = new JMDOAttendanceUpsertDto
            {
                Date = date.Date,
                VisitArea = visitArea,
                AttendanceStatus = status,
                Latitude = latitude,
                Longitude = longitude
            };

            var response = await _http.PostAsJsonAsync(Root, dto, Json, ct);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<JMDOAttendanceDto>(Json, ct);

            var message = await ReadErrorAsync(response, ct);
            _toast.Error(message ?? "Could not update attendance.");
            return null;
        }
        catch (Exception)
        {
            _toast.Error("Could not reach the server to update attendance.");
            return null;
        }
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(body)) return null;

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("Message", out var m) ? m.GetString()
                : doc.RootElement.TryGetProperty("message", out var m2) ? m2.GetString()
                : null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
