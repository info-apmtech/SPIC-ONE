using Microsoft.JSInterop;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Web host implementation: browser sessionStorage, which is what the app used
    /// before the store abstraction existed. Per-tab, cleared when the tab closes.
    /// Every call is guarded because JS interop is unavailable during prerender.
    /// </summary>
    public sealed class BrowserSessionStore : ISessionStore
    {
        private readonly IJSRuntime _js;

        public BrowserSessionStore(IJSRuntime js)
        {
            _js = js;
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                var value = await _js.InvokeAsync<string?>("sessionStorage.getItem", key);
                if (!string.IsNullOrEmpty(value))
                    return value;

                // Fall back to localStorage for a remembered (persistent) session.
                return await _js.InvokeAsync<string?>("localStorage.getItem", key);
            }
            catch
            {
                return null;
            }
        }

        public async Task SetAsync(string key, string value)
        {
            try
            {
                await _js.InvokeVoidAsync("sessionStorage.setItem", key, value);
            }
            catch
            {
                // Prerender or a closed circuit; nothing to persist to.
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _js.InvokeVoidAsync("sessionStorage.removeItem", key);
            }
            catch
            {
                // Same as above.
            }
        }

        public async Task SetPersistentAsync(string key, string value)
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.setItem", key, value);
            }
            catch { }
        }

        public async Task RemovePersistentAsync(string key)
        {
            try
            {
                await _js.InvokeVoidAsync("localStorage.removeItem", key);
            }
            catch { }
        }

        public async Task ClearAsync()
        {
            foreach (var k in SessionKeys.All)
            {
                await RemoveAsync(k);
                await RemovePersistentAsync(k);
            }
        }
    }
}
