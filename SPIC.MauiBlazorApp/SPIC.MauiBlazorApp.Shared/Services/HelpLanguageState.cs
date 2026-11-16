using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPIC.Core.Help;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// The Help Desk language for this device, remembered between visits (localStorage on web,
    /// secure storage on MAUI via <see cref="ISessionStore"/>). Storage can be missing or throw
    /// (private window, prerender, cleared data); every access is guarded and falls back to the
    /// default language, so help always renders.
    /// </summary>
    public sealed class HelpLanguageState
    {
        private const string Key = "help.lang";

        private readonly ISessionStore _store;
        private readonly HelpLibrary _library;
        private bool _loaded;

        public HelpLanguageState(ISessionStore store, HelpLibrary library)
        {
            _store = store;
            _library = library;
            Current = library.Config.DefaultLanguage;
        }

        public string Current { get; private set; }

        public IReadOnlyList<HelpLanguage> Languages => _library.Config.Languages;

        public event Action? Changed;

        /// <summary>Reads the remembered language once. Call from OnAfterRenderAsync (JS interop).</summary>
        public async Task LoadAsync()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                var saved = await _store.GetAsync(Key);
                if (IsKnown(saved) && saved != Current)
                {
                    Current = saved!;
                    Changed?.Invoke();
                }
            }
            catch
            {
                // Storage unavailable: keep the default language.
            }
        }

        public async Task SetAsync(string code)
        {
            if (!IsKnown(code) || code == Current) return;
            Current = code;
            Changed?.Invoke();
            try { await _store.SetPersistentAsync(Key, code); }
            catch { /* not remembered on this device; still applied for this visit */ }
        }

        private bool IsKnown(string? code) =>
            !string.IsNullOrWhiteSpace(code) && Languages.Any(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
    }
}
