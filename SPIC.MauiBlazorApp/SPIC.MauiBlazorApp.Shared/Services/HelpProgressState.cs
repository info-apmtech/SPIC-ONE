using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Which guide screens this user has read on this device ("module/section" keys), for the read
    /// ticks and progress bars. Kept in the same device store as the help language
    /// (localStorage on web, secure storage on MAUI); never sent to the server. Storage may be
    /// missing or throw, so every call is guarded and progress simply starts empty.
    /// </summary>
    public sealed class HelpProgressState
    {
        private readonly ISessionStore _store;
        private readonly LoginState _login;
        private HashSet<string>? _read;
        private string? _loadedFor;

        public HelpProgressState(ISessionStore store, LoginState login)
        {
            _store = store;
            _login = login;
        }

        public event Action? Changed;

        private string Key => $"help.read.{(_login.EmployeeId > 0 ? _login.EmployeeId.ToString() : _login.UserId ?? "anon")}";

        public static string ItemKey(string module, string section) => $"{module}/{section}".ToLowerInvariant();

        /// <summary>The read keys for the signed-in user (loaded once per user). Call after first render.</summary>
        public async Task<IReadOnlySet<string>> ReadAsync()
        {
            if (_read is not null && _loadedFor == Key) return _read;
            _loadedFor = Key;
            _read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var json = await _store.GetAsync(Key);
                if (!string.IsNullOrWhiteSpace(json))
                    foreach (var k in JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>())
                        _read.Add(k);
            }
            catch
            {
                // Unreadable or missing: start empty.
            }
            return _read;
        }

        /// <summary>Marks a screen as read. Returns true when it was not read before.</summary>
        public async Task<bool> MarkReadAsync(string module, string section)
        {
            var read = (HashSet<string>)await ReadAsync();
            if (!read.Add(ItemKey(module, section))) return false;
            Changed?.Invoke();
            try { await _store.SetPersistentAsync(Key, JsonSerializer.Serialize(read.OrderBy(k => k).ToList())); }
            catch { /* kept for this visit only */ }
            return true;
        }

        public int CountIn(string module, IEnumerable<string> sectionIds) =>
            _read is null ? 0 : sectionIds.Count(id => _read.Contains(ItemKey(module, id)));
    }
}
