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

        /// <summary>Removes every key in <see cref="SessionKeys.All"/>.</summary>
        async Task ClearAsync()
        {
            foreach (var key in SessionKeys.All)
            {
                await RemoveAsync(key);
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

        public static readonly string[] All = { Token, Expiration, RoleAccess, DesignationName };
    }
}
