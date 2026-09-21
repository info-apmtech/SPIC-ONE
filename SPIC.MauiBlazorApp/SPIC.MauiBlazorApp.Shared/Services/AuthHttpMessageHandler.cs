using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    public class AuthHttpMessageHandler : DelegatingHandler
    {
        private readonly NavigationManager _navigation;
        private readonly ISessionStore _session;
        private readonly LoginState _loginState;

        public AuthHttpMessageHandler(NavigationManager navigation, ISessionStore session, LoginState loginState)
        {
            _navigation = navigation;
            _session = session;
            _loginState = loginState;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            // A 401 means "session expired" only for a call that carried a token. The sign-in
            // endpoint answers 401 for a wrong password, and other anonymous calls never had a
            // session to lose; reloading /login there would wipe the form and its error message.
            if (response.StatusCode == HttpStatusCode.Unauthorized
                && request.Headers.Authorization is not null
                && !IsAnonymousEndpoint(request.RequestUri))
            {
                await HandleUnauthorized();
            }

            return response;
        }

        private static bool IsAnonymousEndpoint(Uri? uri)
        {
            var path = uri?.AbsolutePath ?? uri?.OriginalString ?? string.Empty;
            return path.Contains("/api/Authentication/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task HandleUnauthorized()
        {
            try
            {
                _loginState.Token = null;
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
