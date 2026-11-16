using System;

namespace SPIC.MauiBlazorApp.Shared.Services
{
    /// <summary>
    /// Open/closed state of the "?" help panel. The button lives inside the top bar, which is its own
    /// stacking layer on phones (sticky, z-index 1020, under the bottom tab bar), so the panel is
    /// rendered at the layout root by HelpPanelHost and the two talk through this scoped state.
    /// </summary>
    public sealed class HelpPanelState
    {
        public bool IsOpen { get; private set; }

        public event Action? Changed;

        public void Toggle() => Set(!IsOpen);

        public void Close() => Set(false);

        private void Set(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open;
            Changed?.Invoke();
        }
    }
}
