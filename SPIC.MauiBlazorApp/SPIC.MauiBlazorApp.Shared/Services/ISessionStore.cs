namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Persists the signed-in session (JWT, expiry, page permissions, designation)
    /// for the current host. The Web host keeps it in browser sessionStorage; the
    /// MAUI host keeps it in the platform secure store so a login survives the OS
    /// killing and relaunching the app.
    /// </summary>
    public interface ISessionStore
    {
        Task<string?> GetAsync(string key);
        Task SetAsync(string key, string value);
        Task RemoveAsync(string key);

        /// <summary>
        /// Writes to persistent storage (localStorage on web; same as SetAsync on MAUI
        /// since SecureStorage is already persistent).
        /// </summary>
        Task SetPersistentAsync(string key, string value) => SetAsync(key, value);

        /// <summary>
        /// Removes from persistent storage (localStorage on web; no-op if store is single-tier).
        /// </summary>
        Task RemovePersistentAsync(string key) => Task.CompletedTask;

        /// <summary>Removes every key in <see cref="SessionKeys.All"/> from all stores.</summary>
        async Task ClearAsync()
        {
            foreach (var key in SessionKeys.All)
            {
                await RemoveAsync(key);
                await RemovePersistentAsync(key);
            }
        }
    }

    /// <summary>The keys written at login and read back by MainLayout / PageGuard.</summary>
    public static class SessionKeys
    {
        public const string Token = "jwt_token";
        public const string Expiration = "jwt_expiration";
        public const string RoleAccess = "spic_role_access";
        public const string DesignationName = "spic_designation_name";
        public const string RefreshToken = "spic_refresh_token";
        public const string ServerClockOffset = "spic_server_clock_offset";

        /// <summary>
        /// "1" when the sign-in ticked "Keep me signed in" (written to both tiers), "0" (session tier only) when it did
        /// not. Recheck residual N2: a refreshed farmer session goes back only where the sign-in put it, so a farmer who
        /// chose not to be remembered never gets a token or refresh token in persistent (local) storage.
        /// </summary>
        public const string Remembered = "spic_remembered";

        public static readonly string[] All = { Token, Expiration, RoleAccess, DesignationName, RefreshToken, ServerClockOffset, Remembered };

        /// <summary>
        /// "1" when the last sign-in on this device was a farmer (recheck fix U5). Persistent and deliberately NOT in
        /// <see cref="All"/>: it survives sign-out so "/" and a signed-out /farmer/* link open the farmer sign-in.
        /// Holds no personal data.
        /// </summary>
        public const string LastSignInFarmer = "spic_last_signin_farmer";
    }
}
