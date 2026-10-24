using System.Collections.Concurrent;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Services
{
    /// <summary>
    /// MAUI host implementation backed by the platform secure store
    /// (Android Keystore-encrypted preferences, iOS/Mac Keychain, Windows DPAPI),
    /// so a login survives the OS killing and relaunching the app.
    ///
    /// If the secure store is unavailable on a device (a rare keystore fault on some
    /// Android builds) the values are kept in memory for the life of the process
    /// instead of failing the login, which is exactly what sessionStorage did before.
    /// </summary>
    public sealed class SecureSessionStore : ISessionStore
    {
        private readonly ConcurrentDictionary<string, string> _fallback = new();
        private bool _secureStorageBroken;

        public async Task<string?> GetAsync(string key)
        {
            if (_fallback.TryGetValue(key, out var cached))
            {
                return cached;
            }

            if (_secureStorageBroken)
            {
                return null;
            }

            try
            {
                return await SecureStorage.Default.GetAsync(key);
            }
            catch
            {
                _secureStorageBroken = true;
                return null;
            }
        }

        public async Task SetAsync(string key, string value)
        {
            _fallback[key] = value;

            if (!_secureStorageBroken)
            {
                try
                {
                    await SecureStorage.Default.SetAsync(key, value);
                    return;
                }
                catch
                {
                    _secureStorageBroken = true;
                }
            }
        }

        public Task SetPersistentAsync(string key, string value) => SetAsync(key, value);

        /// <summary>
        /// On MAUI, SetAsync holds the active session. Removing the "persistent" copy
        /// (e.g. when RememberMe is false) must NOT wipe the active session!
        /// </summary>
        public Task RemovePersistentAsync(string key) => Task.CompletedTask;

        public Task RemoveAsync(string key)
        {
            _fallback.TryRemove(key, out _);

            if (!_secureStorageBroken)
            {
                try
                {
                    SecureStorage.Default.Remove(key);
                }
                catch
                {
                    _secureStorageBroken = true;
                }
            }

            return Task.CompletedTask;
        }
    }
}
