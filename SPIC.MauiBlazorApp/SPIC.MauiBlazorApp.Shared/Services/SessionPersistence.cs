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
/// <summary>What a refresh attempt came to (recheck residual N3).</summary>
public enum RefreshOutcome
{
    /// <summary>A new token pair is in place.</summary>
    Refreshed,
    /// <summary>The API refused the refresh token (400 / 401 / 403), or none is stored: sign in again.</summary>
    Refused,
    /// <summary>Offline, a timeout or a server error: keep the stored session and try again later.</summary>
    Unreachable
}

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
        await store.SetAsync(SessionKeys.Remembered, remember ? "1" : "0");

        if (remember)
        {
            await store.SetPersistentAsync(SessionKeys.Remembered, "1");
            await store.SetPersistentAsync(SessionKeys.Token, rawToken);
            await store.SetPersistentAsync(SessionKeys.Expiration, expiration.ToString("O"));
            await store.SetPersistentAsync(SessionKeys.RoleAccess, roleAccess ?? "");
            await store.SetPersistentAsync(SessionKeys.DesignationName, designationName ?? "");
            await store.SetPersistentAsync(SessionKeys.RefreshToken, refreshToken ?? "");
            await store.SetPersistentAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);
        }
        else
        {
            await store.RemovePersistentAsync(SessionKeys.Remembered);
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
                    // Recheck residual M4: a short timeout of its own, so a stalled connection cannot hold the start
                    // screen; a timeout counts as Unreachable.
                    var outcome = await TryRefreshAsync(http, store, login, refreshToken, timeout: ColdStartRefreshTimeout);
                    if (outcome == RefreshOutcome.Refreshed)
                        return login.IsLoggedIn && !login.IsTokenExpired;
                    login.ClearAllowedPages();
                    login.SetDesignationName(null);
                    // Recheck residual N3: only an explicit refusal forgets the refresh token. Offline, a timeout or a
                    // server error keeps the stored session, so the next start (or tap) tries again without an SMS.
                    if (outcome == RefreshOutcome.Refused)
                        await ForgetRefreshTokenAsync(store);
                    return false;
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

    /// <summary>How long a refresh may take before it counts as Unreachable (recheck residual M4).</summary>
    public static readonly TimeSpan ColdStartRefreshTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// True when <paramref name="status"/> from api/Authentication/refresh is an explicit refusal (400 / 401 / 403):
    /// the refresh token is gone, expired or revoked. Anything else that is not a success (5xx, 429, 404 from a proxy
    /// during a deploy) is transient (recheck residual N3).
    /// </summary>
    public static bool IsRefusal(System.Net.HttpStatusCode status) =>
        status is System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden;

    /// <summary>
    /// Where a refreshed session is written (recheck residual N2): staff as before (both tiers); a farmer only to the
    /// persistent tier when the sign-in ticked "Keep me signed in" (<see cref="SessionKeys.Remembered"/>).
    /// </summary>
    public static async Task<bool> PersistRefreshedAsync(ISessionStore store, AppRole? role)
    {
        if (role != AppRole.Farmer) return true;
        try { return await store.GetAsync(SessionKeys.Remembered) == "1"; }
        catch { return false; }
    }

    /// <summary>Writes a refreshed token pair to the session tier, and to the persistent tier when <paramref name="persist"/>.</summary>
    public static async Task StoreRefreshedAsync(ISessionStore store, LoginState login, string rawToken, DateTime expiration,
        string? refreshToken, bool persist)
    {
        await store.SetAsync(SessionKeys.Token, rawToken);
        await store.SetAsync(SessionKeys.Expiration, expiration.ToString("O"));
        await store.SetAsync(SessionKeys.RefreshToken, refreshToken ?? "");
        await store.SetAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);
        if (!persist) return;
        await store.SetPersistentAsync(SessionKeys.Token, rawToken);
        await store.SetPersistentAsync(SessionKeys.Expiration, expiration.ToString("O"));
        await store.SetPersistentAsync(SessionKeys.RefreshToken, refreshToken ?? "");
        await store.SetPersistentAsync(SessionKeys.ServerClockOffset, login.Clock.SavedOffset);
    }

    /// <summary>
    /// Trades the stored refresh token for a new JWT + refresh token pair. Refreshed: the new pair is stored where the
    /// sign-in put the session (<see cref="PersistRefreshedAsync"/>). Refused: the API answered 400 / 401 / 403 (or no
    /// refresh token is stored). Unreachable: offline, a timeout (<paramref name="timeout"/>, default
    /// <see cref="ColdStartRefreshTimeout"/>), a 5xx or any other failure. Nothing is changed unless Refreshed, so the
    /// caller decides what to clear: only Refused may forget the refresh token or sign out (recheck residual N3).
    /// </summary>
    public static async Task<RefreshOutcome> TryRefreshAsync(HttpClient http, ISessionStore store, LoginState login, string? refreshToken = null,
        CancellationToken ct = default, TimeSpan? timeout = null)
    {
        try
        {
            refreshToken ??= login.RefreshToken;
            if (string.IsNullOrWhiteSpace(refreshToken)) refreshToken = await store.GetAsync(SessionKeys.RefreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken)) return RefreshOutcome.Refused;

            using var limit = CancellationTokenSource.CreateLinkedTokenSource(ct);
            limit.CancelAfter(timeout ?? ColdStartRefreshTimeout);

            using var response = await http.PostAsJsonAsync("api/Authentication/refresh",
                new RefreshTokenRequestDto { RefreshToken = refreshToken }, limit.Token);
            if (IsRefusal(response.StatusCode)) return RefreshOutcome.Refused;
            if (!response.IsSuccessStatusCode) return RefreshOutcome.Unreachable;

            var result = await response.Content.ReadFromJsonAsync<RefreshTokenResponseDto>(cancellationToken: limit.Token);
            if (result is null || string.IsNullOrWhiteSpace(result.Token)) return RefreshOutcome.Unreachable;

            var rawToken = result.Token.StartsWith("Bearer ", StringComparison.Ordinal) ? result.Token["Bearer ".Length..] : result.Token;
            var role = RoleOf(rawToken) ?? login.UserRole;
            var persist = await PersistRefreshedAsync(store, role);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);
            await StoreRefreshedAsync(store, login, rawToken, result.Expiration, result.RefreshToken, persist);

            login.Expiration = result.Expiration;
            login.RefreshToken = result.RefreshToken;
            login.Token = rawToken;
            return RefreshOutcome.Refreshed;
        }
        catch
        {
            // HttpRequestException (offline), TaskCanceledException (timeout), JSON or storage errors: transient.
            return RefreshOutcome.Unreachable;
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
