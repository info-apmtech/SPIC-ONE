using System.Globalization;
using System.Net.Http.Headers;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Starting and restoring a signed-in session, moved verbatim out of LoginCard.razor (start) and
/// Login.razor (restore) on 2026-11-27 so the farmer OTP sign-in (Pages/Farmer/FarmerLogin.razor)
/// stores its session through exactly the same code path as the username / password login.
/// </summary>
public static class SessionPersistence
{
    /// <summary>
    /// Applies a login response: the Authorization header, the session store (and, when
    /// <paramref name="remember"/>, the persistent store - otherwise any persistent copy is removed),
    /// then LoginState as one synchronous batch. Returns the raw token (without "Bearer ").
    /// </summary>
    public static async Task<string> StartAsync(HttpClient http, ISessionStore store, LoginState login,
        string token, DateTime expiration, string? roleAccess, string? designationName, string? refreshToken, bool remember)
    {
        var rawToken = token.StartsWith("Bearer ")
            ? token.Substring("Bearer ".Length)
            : token;

        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", rawToken);

        // Persist to the session store FIRST, then populate LoginState as one
        // atomic synchronous batch. This way every LoginState.OnChange
        // subscriber (NavMenu, PageGuard) and the authenticated layout only
        // ever sees the complete authenticated state, never intermediate
        // partial states during login.
        await store.SetAsync(SessionKeys.Token, rawToken);
        await store.SetAsync(SessionKeys.Expiration, expiration.ToString("O"));
        await store.SetAsync(SessionKeys.RoleAccess, roleAccess ?? "");
        await store.SetAsync(SessionKeys.DesignationName, designationName ?? "");
        await store.SetAsync(SessionKeys.RefreshToken, refreshToken ?? "");
        await store.SetAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);

        if (remember)
        {
            await store.SetPersistentAsync(SessionKeys.Token, rawToken);
            await store.SetPersistentAsync(SessionKeys.Expiration, expiration.ToString("O"));
            await store.SetPersistentAsync(SessionKeys.RoleAccess, roleAccess ?? "");
            await store.SetPersistentAsync(SessionKeys.DesignationName, designationName ?? "");
            await store.SetPersistentAsync(SessionKeys.RefreshToken, refreshToken ?? "");
            await store.SetPersistentAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);
        }
        else
        {
            await store.RemovePersistentAsync(SessionKeys.Token);
            await store.RemovePersistentAsync(SessionKeys.Expiration);
            await store.RemovePersistentAsync(SessionKeys.RoleAccess);
            await store.RemovePersistentAsync(SessionKeys.DesignationName);
            await store.RemovePersistentAsync(SessionKeys.RefreshToken);
            await store.RemovePersistentAsync(SessionKeys.ServerClockOffset);
        }

        // Persist token + page permissions for refreshes and other pages
        login.Token = rawToken;
        login.Expiration = expiration;
        login.RefreshToken = refreshToken;
        login.SetAllowedPages(roleAccess);
        login.SetDesignationName(designationName);

        return rawToken;
    }

    /// <summary>
    /// Restores a session saved by a previous run (SecureStorage on the app, sessionStorage /
    /// localStorage on the web) into LoginState when it is still valid for at least a minute.
    /// Returns true when the user is signed in afterwards. Throws only what the store throws.
    /// </summary>
    public static async Task<bool> TryRestoreAsync(ISessionStore store, LoginState login)
    {
        if (!login.IsLoggedIn)
        {
            var token = await store.GetAsync(SessionKeys.Token);
            var expirationText = await store.GetAsync(SessionKeys.Expiration);
            login.Clock.Restore(await store.GetAsync(SessionKeys.ServerClockOffset));

            if (!string.IsNullOrWhiteSpace(token)
                && DateTime.TryParse(expirationText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiration)
                && expiration.ToUniversalTime() > login.Clock.UtcNow.AddMinutes(1))
            {
                login.Token = token;
                login.Expiration = expiration;
                login.RefreshToken = await store.GetAsync(SessionKeys.RefreshToken);
                login.SetAllowedPages(await store.GetAsync(SessionKeys.RoleAccess));
                login.SetDesignationName(await store.GetAsync(SessionKeys.DesignationName));
            }
        }

        return login.IsLoggedIn && !login.IsTokenExpired;
    }
}
