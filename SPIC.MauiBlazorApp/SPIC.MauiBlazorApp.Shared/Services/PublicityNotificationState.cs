namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Lightweight polling-based live unread-count for Publicity notifications, shared between the
/// header bell (MainLayout) and the Publicity pages' own Notifications button/page. There is no
/// SignalR or other push infrastructure anywhere in this app (confirmed before writing this), so
/// this is a simple poll-and-diff loop against the existing notifications API - not a new generic
/// notification system. Scoped, same lifetime as PublicityApi/LoginState, so one instance per
/// user session/circuit.
/// </summary>
public sealed class PublicityNotificationState : IDisposable
{
    private const int PollSeconds = 25;
    private readonly PublicityApi _api;
    private readonly LoginState _login;
    private CancellationTokenSource? _cts;
    private int? _lastKnownCount;

    public int UnreadCount { get; private set; }
    public bool Muted { get; private set; }

    /// <summary>Fired whenever UnreadCount changes (including the very first load) - bind the badge
    /// to this, not to a one-time read of UnreadCount.</summary>
    public event Action? OnChange;

    /// <summary>Fired only when the count genuinely INCREASED since the last known value - a real new
    /// notification arrived. Never fires on the very first poll (no prior baseline to compare against,
    /// so a page refresh never replays the sound) and never on a drop (Mark as Read).</summary>
    public event Action? OnNewNotification;

    public PublicityNotificationState(PublicityApi api, LoginState login)
    {
        _api = api;
        _login = login;
        _login.OnChange += HandleLoginChanged;
    }

    private void HandleLoginChanged()
    {
        // A different user may just have signed in on the same circuit (MAUI keeps one scope for the
        // whole app session) - the old count is meaningless for them, and must not look like a
        // decrease/increase relative to whatever the previous user's count was.
        _lastKnownCount = null;
        UnreadCount = 0;
        OnChange?.Invoke();

        // Root-cause fix for the slow first badge: LoginState.Token is restored asynchronously
        // (MainLayout.razor's OnAfterRenderAsync awaits a SessionStore/JS-interop round-trip before
        // setting it), but EnsurePolling() gets called from component OnInitializedAsync methods that
        // can run BEFORE that - on a fresh circuit (first load after login, or any hard refresh), the
        // poll loop's very first tick fires with no Authorization header yet, 401s, and is silently
        // swallowed (RefreshAsync below now skips it instead - see the IsLoggedIn guard), leaving the
        // badge to wait up to PollSeconds for the next tick. LoginState.Token's setter already fires
        // OnChange on every change, including the moment it goes from not-set to set - that is the
        // one authoritative, timing-independent signal that an authenticated request will now actually
        // succeed, regardless of which component happened to call EnsurePolling() first or when.
        // Firing the real fetch from here means the badge updates the instant auth is ready, not on
        // whatever the next poll tick happens to be.
        if (_login.IsLoggedIn) _ = RefreshAsync();
    }

    public void SetMuted(bool muted) => Muted = muted;

    /// <summary>Starts the poll loop once per session - safe to call from every component that wants
    /// live updates (MainLayout, PublicityList, PublicityNotifications); a second call while already
    /// running is a no-op, so callers never need to coordinate who "owns" starting it.</summary>
    public void EnsurePolling()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        _ = PollLoopAsync(_cts.Token);
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RefreshAsync();
            try { await Task.Delay(TimeSpan.FromSeconds(PollSeconds), ct); }
            catch (TaskCanceledException) { return; }
        }
    }

    /// <summary>Immediately updates the live unread count with a confirmed count from the server,
    /// firing OnChange so all header and page badges reflect the change with zero delay.</summary>
    public void SetUnreadCount(int count)
    {
        var safeCount = Math.Max(0, count);
        var previous = _lastKnownCount;
        _lastKnownCount = safeCount;
        UnreadCount = safeCount;
        OnChange?.Invoke();
        if (previous is int p && safeCount > p) OnNewNotification?.Invoke();
    }

    /// <summary>Safely decrements the in-memory unread count by 1 (minimum 0) and notifies subscribers.</summary>
    public void DecrementUnread()
    {
        var next = Math.Max(0, UnreadCount - 1);
        _lastKnownCount = next;
        UnreadCount = next;
        OnChange?.Invoke();
    }

    /// <summary>Re-fetches the unread count immediately, outside the poll cadence - called right after
    /// Mark as Read/Mark All as Read so both badges update without waiting up to PollSeconds.</summary>
    public async Task RefreshAsync()
    {
        // Skip the call entirely (not just let it fail) when auth isn't ready yet - a poll tick or an
        // EnsurePolling() first-fetch that races LoginState.Token's async restore would otherwise go
        // out with no bearer token, get a guaranteed 401, and trigger ApiClientBase's "Your session
        // has expired" toast for someone who is, in fact, still logged in. HandleLoginChanged above
        // fires the real fetch the instant IsLoggedIn actually becomes true.
        if (!_login.IsLoggedIn) return;
        var result = await _api.UnreadCountAsync();
        if (result is null) return;
        SetUnreadCount(result.Count);
    }

    public void Dispose()
    {
        _login.OnChange -= HandleLoginChanged;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
