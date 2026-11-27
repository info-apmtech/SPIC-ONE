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
