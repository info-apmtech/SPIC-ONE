using System.Net.Http.Headers;
using System.Net.Http.Json;
using SPIC.Core.DTOs;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Services
{
    /// <summary>
    /// The native background tracking service's own, minimal HTTP client - deliberately NOT routed
    /// through the app's Blazor DI container (an Android Service/iOS background delegate doesn't
    /// have access to that scope, and shouldn't need to: this is a small, self-contained path).
    ///
    /// Reads the access/refresh token straight from the SAME secure store the foreground Blazor UI
    /// uses (SecureStorage, via the same keys SecureSessionStore/SessionKeys already define) - never
    /// a second, separately-held credential, and never anything less secure than what the rest of
    /// the app already trusts. On a 401 it calls POST api/Authentication/refresh itself (the same
    /// endpoint AuthHttpMessageHandler uses) and writes the renewed pair back to that same store, so
    /// the foreground UI picks up the renewal transparently next time it reads session state too.
    /// </summary>
    public static class JmdoTrackingBackgroundClient
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };

        /// <summary>Captures one fix and tries to upload it (plus flushing anything already queued
        /// for this session) - on any failure the fix is queued locally instead of lost.</summary>
        public static async Task ReportPointAsync(int sessionId, double latitude, double longitude, double? accuracy, CancellationToken ct)
        {
            var point = new PendingLocationPoint
            {
                SessionId = sessionId,
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                DeviceCapturedAt = DateTime.Now
            };

            // Try the queue first - if there's a backlog, send it (and this new point) as one
            // batch rather than posting this point live and leaving the backlog stuck behind it.
            var queued = PendingLocationQueue.PeekForSession(sessionId);
            if (queued.Count > 0)
            {
                PendingLocationQueue.Enqueue(point);
                await FlushQueueAsync(sessionId, ct);
                return;
            }

            var dto = new JMDOTrackingPointUpsertDto
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                Accuracy = point.Accuracy,
                DeviceCapturedAt = point.DeviceCapturedAt
            };

            var ok = await SendAsync(
                http => http.PostAsJsonAsync($"api/JMDOTracking/sessions/{sessionId}/points", dto, ct), ct);

            if (!ok) PendingLocationQueue.Enqueue(point);
        }

        public static async Task FlushQueueAsync(int sessionId, CancellationToken ct)
        {
            var queued = PendingLocationQueue.PeekForSession(sessionId);
            if (queued.Count == 0) return;

            // JMDOTrackingController.AddPointsBatch caps a request at 500 points.
            foreach (var batch in queued.Chunk(500))
            {
                var dtos = batch.Select(p => new JMDOTrackingPointUpsertDto
                {
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    Accuracy = p.Accuracy,
                    DeviceCapturedAt = p.DeviceCapturedAt
                }).ToList();

                var ok = await SendAsync(
                    http => http.PostAsJsonAsync($"api/JMDOTracking/sessions/{sessionId}/points/batch", dtos, ct), ct);

                if (!ok) return; // stop here - remaining batches stay queued for the next attempt

                PendingLocationQueue.RemoveUpTo(sessionId, batch);
            }
        }

        /// <summary>Attaches the current access token, sends, and on a 401 tries exactly one silent
        /// refresh (via the shared RefreshAccessTokenAsync) before giving up - same one-retry
        /// policy as AuthHttpMessageHandler's own silent-refresh attempt.</summary>
        private static async Task<bool> SendAsync(Func<HttpClient, Task<HttpResponseMessage>> send, CancellationToken ct)
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync(SessionKeys.Token);
                if (string.IsNullOrWhiteSpace(token)) return false;

                Http.BaseAddress ??= new Uri(SPIC.MauiBlazorApp.MauiProgram.ApiBaseUrl);
                Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await send(Http);
                if (response.IsSuccessStatusCode) return true;

                if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized) return false;

                var refreshed = await RefreshAccessTokenAsync(ct);
                if (!refreshed) return false;

                var newToken = await SecureStorage.Default.GetAsync(SessionKeys.Token);
                if (string.IsNullOrWhiteSpace(newToken)) return false;

                Http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
                var retry = await send(Http);
                return retry.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>Trades the stored refresh token for a new access+refresh pair, writing both back
        /// to SecureStorage - the exact same keys/mechanism the foreground app reads at startup
        /// (MainLayout.razor/Login.razor), so a renewal here is picked up by the UI automatically
        /// the next time it checks, with no separate sync path.</summary>
        private static async Task<bool> RefreshAccessTokenAsync(CancellationToken ct)
        {
            try
            {
                var refreshToken = await SecureStorage.Default.GetAsync(SessionKeys.RefreshToken);
                if (string.IsNullOrWhiteSpace(refreshToken)) return false;

                Http.BaseAddress ??= new Uri(SPIC.MauiBlazorApp.MauiProgram.ApiBaseUrl);
                Http.DefaultRequestHeaders.Authorization = null;

                var response = await Http.PostAsJsonAsync(
                    "api/Authentication/refresh",
                    new RefreshTokenRequestDto { RefreshToken = refreshToken },
                    ct);

                if (!response.IsSuccessStatusCode) return false;

                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(cancellationToken: ct);
                if (result is null || string.IsNullOrWhiteSpace(result.Token)) return false;

                var rawToken = result.Token.StartsWith("Bearer ", StringComparison.Ordinal)
                    ? result.Token["Bearer ".Length..]
                    : result.Token;

                await SecureStorage.Default.SetAsync(SessionKeys.Token, rawToken);
                await SecureStorage.Default.SetAsync(SessionKeys.Expiration, result.Expiration.ToString("O"));
                await SecureStorage.Default.SetAsync(SessionKeys.RefreshToken, result.RefreshToken);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
