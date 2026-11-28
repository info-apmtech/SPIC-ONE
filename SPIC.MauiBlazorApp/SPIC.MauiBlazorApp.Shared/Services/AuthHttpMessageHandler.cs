using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SPIC.Core.DTOs;
using SPIC.MauiBlazorApp.Shared.Services.Telemetry;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigation;
        private readonly ISessionStore _session;
        private readonly LoginState _loginState;
        private readonly ClientInfo _client;
        private readonly IServiceProvider _services;

        public AuthHttpMessageHandler(NavigationManager navigation, ISessionStore session, LoginState loginState, ClientInfo client, IServiceProvider services)
        {
            _navigation = navigation;
            _session = session;
            _loginState = loginState;
            _client = client;
            _services = services;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Which app made the call (Metrics page, docs/metrics-telemetry-plan.md row 9).
            if (!request.Headers.Contains("X-Spic-Client")) request.Headers.TryAddWithoutValidation("X-Spic-Client", _client.ClientHeader);
            if (!request.Headers.Contains("X-Spic-Version")) request.Headers.TryAddWithoutValidation("X-Spic-Version", _client.Version);

            HttpResponseMessage response;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Explicitly requested cancellation by the caller (e.g. page navigation or component disposal)
                throw;
            }
            catch (OperationCanceledException)
            {
                // Cancellation was NOT requested by the caller (cancellationToken.IsCancellationRequested is false).
                // It was caused by HttpClient's timeout or an aborted transport connection (e.g. API server terminated/restarting).
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    RequestMessage = request,
                    ReasonPhrase = "API Server Unavailable or Connection Aborted",
                    Content = new StringContent("{\"message\":\"Unable to connect to the SPIC API server. Please ensure the backend is running.\"}")
                };
            }
            catch (HttpRequestException ex)
            {
                // Connection refused or network unreachable
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    RequestMessage = request,
                    ReasonPhrase = "API Server Request Failed",
                    Content = new StringContent($"{{\"message\":\"{ex.Message.Replace("\"", "\\\"")}\"}}")
                };
            }
            catch (IOException ex)
            {
                // Transport stream I/O failure (e.g. WSA_OPERATION_ABORTED when server drops)
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    RequestMessage = request,
                    ReasonPhrase = "API Server Transport Aborted",
                    Content = new StringContent($"{{\"message\":\"{ex.Message.Replace("\"", "\\\"")}\"}}")
                };
            }

            // IIS supplies the server's UTC Date header. Synchronize before callers publish
            // a new token, so an incorrect iPhone clock cannot immediately expire it.
            if (response.Headers.Date is { } serverTime)
                _loginState.Clock.Synchronize(serverTime);

            // A 401 means "session expired" only for a call that carried a token. The sign-in
            // endpoint answers 401 for a wrong password, and other anonymous calls never had a
            // session to lose; reloading /login there would wipe the form and its error message.s
            if (response.StatusCode == HttpStatusCode.Unauthorized
                && request.Headers.Authorization is not null
                && !IsAnonymousEndpoint(request.RequestUri))
            {
                // Try one silent refresh before giving up. This is what keeps a normal UI session
                // alive past the 60-minute JWT lifetime instead of forcing a full re-login on the
                // very next API call - the refreshed token lands in LoginState/ISessionStore for
                // whatever the user does next (the request that just 401'd is NOT retried here:
                // safely resending an HttpRequestMessage that may already have had its content
                // stream read is its own can of worms, so the caller still sees this 401 once, but
                // the session itself survives for every call after it).
                if (await TryRefreshAsync(cancellationToken))
                {
                    return response;
                }

                // Recheck residual N3 (Farmer role only): when the refresh call itself could not reach the API
                // (offline, timeout, 5xx), keep the session; only an explicit refusal signs the farmer out. Staff as before.
                if (_lastRefreshUnreachable && _loginState.UserRole == SPIC.Core.Entities.AppRole.Farmer)
                {
                    return response;
                }

                await HandleUnauthorized();
            }

            return response;
        }

        // Resolves HttpClient lazily (not via constructor injection) on purpose: HttpClient's own
        // DI factory resolves THIS handler to build itself (see MauiProgram.cs/Program.cs), so
        // taking a constructor dependency on HttpClient here would be circular. By the time
        // SendAsync runs, the scoped HttpClient this handler belongs to already exists, so this
        // just returns that same instance - calling it again re-enters this handler's SendAsync
        // for the refresh call itself, which is safe (IsAnonymousEndpoint keeps it from recursing
        // into HandleUnauthorized even if the refresh call somehow also 401s).
        /// <summary>The last refresh attempt failed without a refusal (no answer, timeout, 5xx; recheck residual N3).</summary>
        private bool _lastRefreshUnreachable;

        private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
        {
            _lastRefreshUnreachable = false;
            var refreshToken = _loginState.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                refreshToken = await _session.GetAsync(SessionKeys.RefreshToken);
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
                return false;

            try
            {
                var http = _services.GetRequiredService<HttpClient>();
                var response = await http.PostAsJsonAsync(
                    "api/Authentication/refresh",
                    new RefreshTokenRequestDto { RefreshToken = refreshToken },
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _lastRefreshUnreachable = !SessionPersistence.IsRefusal(response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(cancellationToken: cancellationToken);
                if (result is null || string.IsNullOrWhiteSpace(result.Token))
                {
                    _lastRefreshUnreachable = true;
                    return false;
                }

                var rawToken = result.Token.StartsWith("Bearer ", StringComparison.Ordinal)
                    ? result.Token["Bearer ".Length..]
                    : result.Token;

                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

                _loginState.Token = rawToken;
                _loginState.Expiration = result.Expiration;
                _loginState.RefreshToken = result.RefreshToken;

                // Staff: both tiers on every refresh, as before (a session-only staff login may leave a localStorage
                // copy until logout; unchanged by ruling).
                // Recheck residual N2: staff as before (both tiers); a farmer's refreshed session goes to persistent
                // storage only when the sign-in ticked "Keep me signed in".
                var persist = await SessionPersistence.PersistRefreshedAsync(_session, SessionPersistence.RoleOf(rawToken) ?? _loginState.UserRole);
                await SessionPersistence.StoreRefreshedAsync(_session, _loginState, rawToken, result.Expiration, result.RefreshToken, persist);

                return true;
            }
            catch
            {
                // No answer, a timeout or bad JSON: not a refusal (recheck residual N3).
                _lastRefreshUnreachable = true;
                return false;
            }
        }

        private static bool IsAnonymousEndpoint(Uri? uri)
        {
            var path = uri?.AbsolutePath ?? uri?.OriginalString ?? string.Empty;
            // api/Telemetry/batch is a background post: its answer must never sign the user out
            return path.Contains("/api/Authentication/", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/api/FarmerAuth/", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/api/Telemetry/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task HandleUnauthorized()
        {
            // Recheck U5: a farmer goes back to the farmer sign-in; read before the session is cleared.
            var loginPath = SessionPersistence.LoginPathFor(_loginState.UserRole);
            try
            {
                _loginState.Token = null;
                _loginState.RefreshToken = null;
                _loginState.ClearAllowedPages();
                await _session.ClearAsync();
            }
            catch
            {
            }

            // Already on the sign-in page: keep it (and whatever it is showing) instead of reloading.
            var relative = _navigation.ToBaseRelativePath(_navigation.Uri);
            if (relative.StartsWith("login", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("farmer/login", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                _navigation.NavigateTo(loginPath, forceLoad: true);
            }
            catch (NavigationException)
            {
                // Expected exception when navigating
            }
        }
    }
}
