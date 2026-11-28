using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

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

        // Recheck U5: remember whether this device last signed in a farmer (no personal data, survives sign-out).
        try
        {
            if (RoleOf(rawToken) == AppRole.Farmer) await store.SetPersistentAsync(SessionKeys.LastSignInFarmer, "1");
            else
            {
                await store.RemoveAsync(SessionKeys.LastSignInFarmer);          // MAUI: one secure store
                await store.RemovePersistentAsync(SessionKeys.LastSignInFarmer); // web: localStorage
            }
        }
        catch { /* a convenience flag only */ }

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
    public static Task<bool> TryRestoreAsync(ISessionStore store, LoginState login) => TryRestoreAsync(store, login, null);

    /// <summary>
    /// <see cref="TryRestoreAsync(ISessionStore, LoginState)"/>, and (recheck fix U1, Farmer role only) when the stored
    /// farmer JWT is expired or has under a minute left and a refresh token is stored, trades the refresh token first
    /// (POST api/Authentication/refresh through <paramref name="http"/>). The stored refresh token is cleared only
    /// after that attempt fails. Staff sessions are restored exactly as before.
    /// </summary>
    public static async Task<bool> TryRestoreAsync(ISessionStore store, LoginState login, HttpClient? http)
    {
        // Also when the in-memory session is still there but expired (the expiry dialog navigated here without
        // clearing it): the stored farmer session gets its refresh attempt either way.
        if (http is not null && (!login.IsLoggedIn || login.IsTokenExpired))
        {
            var stored = await store.GetAsync(SessionKeys.Token);
            var expirationText = await store.GetAsync(SessionKeys.Expiration);
            login.Clock.Restore(await store.GetAsync(SessionKeys.ServerClockOffset));
            DateTime? storedExpiry = DateTime.TryParse(expirationText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var e) ? e : null;
            if (NeedsFarmerRefresh(RoleOf(stored), storedExpiry, login.Clock.UtcNow))
            {
                var refreshToken = await store.GetAsync(SessionKeys.RefreshToken);
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    // Pages first, so the first route check after the new token already sees them.
                    login.SetAllowedPages(await store.GetAsync(SessionKeys.RoleAccess));
                    login.SetDesignationName(await store.GetAsync(SessionKeys.DesignationName));
                    if (await TryRefreshAsync(http, store, login, refreshToken))
                        return login.IsLoggedIn && !login.IsTokenExpired;
                    login.ClearAllowedPages();
                    login.SetDesignationName(null);
                    await ForgetRefreshTokenAsync(store);
                }
            }
        }

        return await RestoreStoredAsync(store, login);
    }

    private static async Task<bool> RestoreStoredAsync(ISessionStore store, LoginState login)
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

    // ------------------------------------------------------------------ farmer refresh (recheck fixes U1 / U5)

    /// <summary>How long before expiry a farmer session is refreshed silently (before the 2-minute warning dialog).</summary>
    public static readonly TimeSpan FarmerRefreshAhead = TimeSpan.FromMinutes(3);

    /// <summary>The role claim of a JWT (not validated: only to choose client behaviour); null when unreadable.</summary>
    public static AppRole? RoleOf(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var probe = new LoginState { Token = token };
            return probe.UserRole;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>True for a Farmer session whose JWT is expired or has under a minute left (cold-start restore rule).</summary>
    public static bool NeedsFarmerRefresh(AppRole? role, DateTime? expiration, DateTime utcNow) =>
        role == AppRole.Farmer && (expiration is null || expiration.Value.ToUniversalTime() <= utcNow.AddMinutes(1));

    /// <summary>The sign-in page for a role: farmers have their own (recheck fix U5); everyone else keeps /login.</summary>
    public static string LoginPathFor(AppRole? role) => role == AppRole.Farmer ? FarmerLoginPath : StaffLoginPath;

    /// <summary>The sign-in page for a signed-out visitor: /farmer/login for a /farmer/* link or when this device last
    /// signed in a farmer; /login otherwise.</summary>
    public static string SignedOutLoginPath(string? relativePath, bool lastSignInFarmer)
    {
        var path = (relativePath ?? "").TrimStart('/');
        var farmerLink = path.Equals("farmer", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("farmer/", StringComparison.OrdinalIgnoreCase)
                         || path.StartsWith("farmer?", StringComparison.OrdinalIgnoreCase);
        return farmerLink || lastSignInFarmer ? FarmerLoginPath : StaffLoginPath;
    }

    public const string FarmerLoginPath = "/farmer/login";
    public const string StaffLoginPath = "/login";

    /// <summary>True when this device's last sign-in was a farmer (never throws).</summary>
    public static async Task<bool> LastSignInWasFarmerAsync(ISessionStore store)
    {
        try { return await store.GetAsync(SessionKeys.LastSignInFarmer) == "1"; }
        catch { return false; }
    }

    /// <summary>
    /// Trades the stored refresh token for a new JWT + refresh token pair and stores both tiers (as
    /// AuthHttpMessageHandler does). Returns false and changes nothing on any failure, so the caller decides what to clear.
    /// </summary>
    public static async Task<bool> TryRefreshAsync(HttpClient http, ISessionStore store, LoginState login, string? refreshToken = null,
        CancellationToken ct = default)
    {
        try
        {
            refreshToken ??= login.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken)) refreshToken = await store.GetAsync(SessionKeys.RefreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            using var response = await http.PostAsJsonAsync("api/Authentication/refresh",
                new RefreshTokenRequestDto { RefreshToken = refreshToken }, ct);
            if (!response.IsSuccessStatusCode) return false;

            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(cancellationToken: ct);
            if (result is null || string.IsNullOrWhiteSpace(result.Token)) return false;

            var rawToken = result.Token.StartsWith("Bearer ", StringComparison.Ordinal) ? result.Token["Bearer ".Length..] : result.Token;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

            await store.SetAsync(SessionKeys.Token, rawToken);
            await store.SetAsync(SessionKeys.Expiration, result.Expiration.ToString("O"));
            await store.SetAsync(SessionKeys.RefreshToken, result.RefreshToken ?? "");
            await store.SetAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);
            await store.SetPersistentAsync(SessionKeys.Token, rawToken);
            await store.SetPersistentAsync(SessionKeys.Expiration, result.Expiration.ToString("O"));
            await store.SetPersistentAsync(SessionKeys.RefreshToken, result.RefreshToken ?? "");
            await store.SetPersistentAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);

            login.Expiration = result.Expiration;
            login.RefreshToken = result.RefreshToken;
            login.Token = rawToken;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Removes a refresh token that the server refused (both tiers; never throws).</summary>
    public static async Task ForgetRefreshTokenAsync(ISessionStore store)
    {
        try
        {
            await store.RemoveAsync(SessionKeys.RefreshToken);
            await store.RemovePersistentAsync(SessionKeys.RefreshToken);
        }
        catch { /* best effort */ }
    }
}
