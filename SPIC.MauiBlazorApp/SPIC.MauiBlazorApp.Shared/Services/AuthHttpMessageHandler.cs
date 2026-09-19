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

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await HandleUnauthorized();
            }

            return response;
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
