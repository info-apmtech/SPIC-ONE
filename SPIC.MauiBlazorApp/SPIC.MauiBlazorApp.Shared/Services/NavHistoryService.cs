using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// App-wide back-stack for the Android hardware/gesture back button (see MainActivity's own doc
/// comment for why the native WebView's own CanGoBack()/GoBack() can't be trusted for this app).
///
/// Tracks every in-app navigation via NavigationManager.LocationChanged, the same way a browser
/// tracks its own history, and lets the native side step back through it one page at a time -
/// across the whole app, not just one section of it. The first version of this scoped itself to
/// six JMDO portal pages and cleared the stack on leaving them, which broke the exact case this
/// exists for: FieldDashboard -> "My Dashboard" (outside that list) wiped FieldDashboard out of
/// the stack, so back from there had nothing left and closed the app instead of returning to it.
///
/// When there is genuinely nowhere left to go (the first page opened this session - typically
/// DefaultWelcome, right after login), TryGoBackAsync asks via the existing ConfirmDialog
/// (ToastService.ConfirmAsync) instead of letting the app close silently.
/// </summary>
public sealed class NavHistoryService : IDisposable
{
    // Bounds memory for a long-running session - older entries just fall off the bottom, which
    // only matters if someone backs up more steps than this in one sitting.
    private const int MaxHistory = 100;

    private readonly NavigationManager _nav;
    private readonly ToastService _toasts;
    private readonly List<string> _history = new();

    /// <summary>Last-constructed instance - a pragmatic bridge for native code (MainActivity, via
    /// DeviceInterop) to reach the current circuit's back-stack without going through DI, since
    /// this hybrid app only ever has one active window/circuit at a time.</summary>
    public static NavHistoryService? Current { get; private set; }

    public NavHistoryService(NavigationManager nav, ToastService toasts)
    {
        _nav = nav;
        _toasts = toasts;
        Current = this;
        _nav.LocationChanged += OnLocationChanged;
        Track(_nav.Uri);
        System.Diagnostics.Debug.WriteLine($"[BACKDBG] NavHistoryService constructed (instance={GetHashCode()}), Toasts hash={toasts.GetHashCode()}");
    }

    public bool CanGoBack => _history.Count > 1;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) => Track(e.Location);

    private void Track(string url)
    {
        try
        {
            var relative = _nav.ToBaseRelativePath(url);
            var path = relative.Split('#')[0]; // keep the query string (e.g. "?view=tracking"), drop any fragment

            if (_history.Count > 0 && string.Equals(_history[^1], path, StringComparison.OrdinalIgnoreCase))
                return; // duplicate LocationChanged for the same path (e.g. a no-op re-render)

            // Login.razor is registered at BOTH "/login" and "/" (its @page directives) and
            // NavigateTo(LandingPage, replace: true)s away the instant a session is already
            // authenticated - including the very first navigation of a cold start, since
            // index.html's <base href="/"> always lands the WebView on "/" before any restore
            // logic runs. So the entry right before this one is routinely "" (root) just as often
            // as it is "login" itself. Treating either hop as a reset means the landing page
            // becomes the back-stack's actual root: without this, the hardware back button on the
            // landing page steps back into "/" or "/login", which immediately redirects straight
            // back out (same NavigateTo) since the session is still valid - net effect: back
            // silently bounces in place instead of showing the exit-confirm dialog.
            var isLeavingLogin = _history.Count > 0
                && IsLoginBoundary(_history[^1])
                && !IsLoginBoundary(path);
            if (isLeavingLogin)
            {
                _history.Clear();
            }

            _history.Add(path);
            if (_history.Count > MaxHistory)
                _history.RemoveAt(0);

            System.Diagnostics.Debug.WriteLine($"[BACKDBG] Track: [{string.Join(" | ", _history)}]");
        }
        catch
        {
            // Never allow URL tracking to throw or interrupt navigation.
        }
    }

    // Login.razor's two @page routes: "" (root, matched by index.html's <base href="/">) and "login".
    // Compares only the first path segment so a query string (e.g. "login?returnUrl=...") still matches.
    private static bool IsLoginBoundary(string path)
    {
        var firstSegment = path.Split('?')[0].Split('/')[0];
        return firstSegment.Length == 0 || string.Equals(firstSegment, "login", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Steps back one page and returns true, or returns false (and does nothing) when there is
    /// nowhere left to go. For an in-page back-icon button, not the hardware gesture - "nothing to
    /// go back to" there just means the caller falls back to its own default (e.g.
    /// Navigation.NavigateTo("/FieldDashboard")), never an app-exit prompt; that only makes sense
    /// for the hardware button, which is what TryGoBackAsync below is for.
    /// </summary>
    public bool TryGoBack()
    {
        if (_history.Count <= 1) return false;

        _history.RemoveAt(_history.Count - 1);
        _nav.NavigateTo(_history[^1]);
        return true;
    }

    // Set while the exit-confirm dialog is up, so a rapid repeat back-press (the system can take a
    // beat to actually paint the dialog, especially mid predictive-back preview animation) re-joins
    // the SAME ConfirmAsync() call instead of queuing another identical "Exit SPIC ONE?" request -
    // without this, N rapid presses before the first paint queued N duplicate dialogs, each needing
    // its own Cancel tap, which looked like "the first press did nothing" even though it had worked.
    private Task<bool>? _pendingExitPrompt;

    /// <summary>
    /// The hardware/gesture back button's own entry point (via DeviceInterop.TryNavigateBack).
    /// Steps back the same way TryGoBack does and returns true, except when there is nowhere left
    /// to go (the first page opened this session - typically DefaultWelcome, right after login),
    /// where it asks the user to confirm via the existing ConfirmDialog instead of silently exiting:
    ///   - Confirmed -> returns false, so the caller (DeviceInterop.FallbackHardwareBack) proceeds
    ///     to actually exit the app, same as a browser closing on its very first page.
    ///   - Cancelled -> returns true ("handled" - stay right where we are).
    /// </summary>
    public Task<bool> TryGoBackAsync()
    {
        if (_pendingExitPrompt is { } inFlight)
        {
            System.Diagnostics.Debug.WriteLine("[BACKDBG] TryGoBackAsync: exit prompt already in flight, joining it.");
            return inFlight;
        }

        var task = TryGoBackCoreAsync();
        _pendingExitPrompt = task;
        return task;
    }

    private async Task<bool> TryGoBackCoreAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[BACKDBG] TryGoBackAsync: history=[{string.Join(" | ", _history)}]");
            if (TryGoBack())
            {
                System.Diagnostics.Debug.WriteLine("[BACKDBG] TryGoBackAsync: stepped back within history.");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("[BACKDBG] TryGoBackAsync: nowhere to go, calling ConfirmAsync...");
            var confirmedExit = await _toasts.ConfirmAsync(
                "Exit SPIC ONE?",
                "Are you sure you want to close the app?",
                confirmText: "Exit",
                danger: true);

            System.Diagnostics.Debug.WriteLine($"[BACKDBG] TryGoBackAsync: ConfirmAsync resolved with confirmedExit={confirmedExit}");
            return !confirmedExit;
        }
        finally
        {
            _pendingExitPrompt = null;
        }
    }

    public void Dispose()
    {
        _nav.LocationChanged -= OnLocationChanged;
        if (ReferenceEquals(Current, this)) Current = null;
    }
}
