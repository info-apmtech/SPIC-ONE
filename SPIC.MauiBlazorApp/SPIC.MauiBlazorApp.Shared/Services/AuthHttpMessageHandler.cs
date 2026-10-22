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

            var response = await base.SendAsync(request, cancellationToken);

            // A 401 means "session expired" only for a call that carried a token. The sign-in
            // endpoint answers 401 for a wrong password, and other anonymous calls never had a
            // session to lose; reloading /login there would wipe the form and its error message.
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
        private async Task<bool> TryRefreshAsync(CancellationToken cancellationToken)
        {
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
                    return false;

                var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(cancellationToken: cancellationToken);
                if (result is null || string.IsNullOrWhiteSpace(result.Token))
                    return false;

                var rawToken = result.Token.StartsWith("Bearer ", StringComparison.Ordinal)
                    ? result.Token["Bearer ".Length..]
                    : result.Token;

                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

                _loginState.Token = rawToken;
                _loginState.Expiration = result.Expiration;
                _loginState.RefreshToken = result.RefreshToken;

                // ISessionStore has no "does a persistent copy exist" check, only Set/Remove, so
                // this can't tell a "Remember me" login apart from a session-only one here - it
                // writes both tiers on every silent refresh. Worst case for a session-only Web
                // login, this leaves a copy in localStorage that outlives the browser tab a little
                // longer than intended; it's still cleared on explicit logout (ISessionStore.
                // ClearAsync wipes both tiers) and is no more exposed than the session copy already
                // was, so this is a minor persistence-duration quirk, not a security regression.
                await _session.SetAsync(SessionKeys.Token, rawToken);
                await _session.SetAsync(SessionKeys.Expiration, result.Expiration.ToString("O"));
                await _session.SetAsync(SessionKeys.RefreshToken, result.RefreshToken);
                await _session.SetPersistentAsync(SessionKeys.Token, rawToken);
                await _session.SetPersistentAsync(SessionKeys.Expiration, result.Expiration.ToString("O"));
                await _session.SetPersistentAsync(SessionKeys.RefreshToken, result.RefreshToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsAnonymousEndpoint(Uri? uri)
        {
            var path = uri?.AbsolutePath ?? uri?.OriginalString ?? string.Empty;
            // api/Telemetry/batch is a background post: its answer must never sign the user out
            return path.Contains("/api/Authentication/", StringComparison.OrdinalIgnoreCase)
                || path.Contains("/api/Telemetry/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task HandleUnauthorized()
        {
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
            if (relative.StartsWith("login", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                _navigation.NavigateTo("/login", forceLoad: true);
            }
            catch (NavigationException)
            {
                // Expected exception when navigating
            }
        }
    }
}
