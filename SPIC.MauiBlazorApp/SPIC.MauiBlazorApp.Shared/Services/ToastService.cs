using System.Collections.ObjectModel;

namespace SPIC.MauiBlazorApp.Shared.Services;

public enum ToastLevel
{
    Success,
    Error,
    Info,
    Warning
}

/// <summary>One toast notification. Immutable; identity is <see cref="Id"/>.</summary>
public sealed record ToastMessage(
    Guid Id,
    ToastLevel Level,
    string Message,
    string? Title,
    int DurationMs,
    DateTime CreatedUtc);

/// <summary>
/// A pending confirmation request rendered by <c>ConfirmDialog</c>. Resolved through
/// <see cref="ToastService.ResolveConfirm(bool)"/>.
/// </summary>
public sealed class ConfirmRequest
{
    internal ConfirmRequest(string title, string message, string confirmText, string cancelText, bool danger)
    {
        Title = title;
        Message = message;
        ConfirmText = confirmText;
        CancelText = cancelText;
        Danger = danger;
    }

    public string Title { get; }
    public string Message { get; }
    public string ConfirmText { get; }
    public string CancelText { get; }
    public bool Danger { get; }

    internal TaskCompletionSource<bool> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>
/// Scoped notification hub. Register with <c>services.AddScoped&lt;ToastService&gt;()</c>
/// and render one <c>&lt;ToastHost /&gt;</c> and one <c>&lt;ConfirmDialog /&gt;</c> per layout.
///
/// Toasts beyond <see cref="MaxVisible"/> wait in a queue and are promoted as visible
/// ones are dismissed, so a burst of API errors never floods the screen.
/// Auto-dismiss timing is owned by <c>ToastHost</c> (it needs the render loop);
/// this class holds only state, so it never needs disposing.
/// </summary>
public class ToastService
{
    private readonly List<ToastMessage> _visible = new();
    private readonly Queue<ToastMessage> _pending = new();
    private readonly Queue<ConfirmRequest> _pendingConfirms = new();
    private readonly object _gate = new();

    /// <summary>Maximum toasts on screen at once. Extra toasts queue.</summary>
    public int MaxVisible { get; set; } = 4;

    /// <summary>Toasts currently on screen, oldest first.</summary>
    public IReadOnlyList<ToastMessage> Toasts
    {
        get { lock (_gate) return new ReadOnlyCollection<ToastMessage>(_visible.ToList()); }
    }

    /// <summary>Number of toasts waiting behind the visible ones.</summary>
    public int QueuedCount
    {
        get { lock (_gate) return _pending.Count; }
    }

    /// <summary>The confirmation currently shown, or null.</summary>
    public ConfirmRequest? PendingConfirm { get; private set; }

    /// <summary>Raised whenever toasts or the pending confirmation change.</summary>
    public event Action? OnChange;

    // ------------------------------------------------------------------ toasts

    public void Success(string message, string? title = null, int durationMs = 3500)
        => Show(ToastLevel.Success, message, title, durationMs);

    public void Error(string message, string? title = null, int durationMs = 3500)
        => Show(ToastLevel.Error, message, title, durationMs);

    public void Info(string message, string? title = null, int durationMs = 3500)
        => Show(ToastLevel.Info, message, title, durationMs);

    public void Warning(string message, string? title = null, int durationMs = 3500)
        => Show(ToastLevel.Warning, message, title, durationMs);

    /// <summary>
    /// Show a toast. <paramref name="durationMs"/> &lt;= 0 means "sticky" (stays until dismissed).
    /// Returns the toast id so callers can <see cref="Dismiss"/> it early.
    /// </summary>
    public Guid Show(ToastLevel level, string message, string? title = null, int durationMs = 3500)
    {
        var toast = new ToastMessage(Guid.NewGuid(), level, message ?? string.Empty, title, durationMs, DateTime.UtcNow);

        lock (_gate)
        {
            if (_visible.Count < MaxVisible) _visible.Add(toast);
            else _pending.Enqueue(toast);
        }

        OnChange?.Invoke();
        return toast.Id;
    }

    /// <summary>Remove a toast (visible or queued). Promotes the next queued toast.</summary>
    public void Dismiss(Guid id)
    {
        bool changed;
        lock (_gate)
        {
            changed = _visible.RemoveAll(t => t.Id == id) > 0;

            if (!changed && _pending.Count > 0)
            {
                var kept = _pending.Where(t => t.Id != id).ToList();
                changed = kept.Count != _pending.Count;
                _pending.Clear();
                foreach (var t in kept) _pending.Enqueue(t);
            }

            while (_visible.Count < MaxVisible && _pending.Count > 0)
            {
                _visible.Add(_pending.Dequeue());
                changed = true;
            }
        }

        if (changed) OnChange?.Invoke();
    }

    /// <summary>Remove every toast, visible and queued.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _visible.Clear();
            _pending.Clear();
        }
        OnChange?.Invoke();
    }

    // ----------------------------------------------------------------- confirm

    /// <summary>
    /// Ask the user a yes/no question through <c>ConfirmDialog</c> instead of the browser's
    /// <c>confirm()</c>. Resolves true on confirm, false on cancel / Escape / backdrop.
    /// Concurrent calls are shown one after another.
    /// </summary>
    public Task<bool> ConfirmAsync(
        string title,
        string message,
        string confirmText = "Confirm",
        bool danger = false,
        string cancelText = "Cancel")
    {
        var request = new ConfirmRequest(title, message, confirmText, cancelText, danger);

        lock (_gate)
        {
            if (PendingConfirm is null) PendingConfirm = request;
            else _pendingConfirms.Enqueue(request);
        }

        OnChange?.Invoke();
        return request.Completion.Task;
    }

    /// <summary>Called by <c>ConfirmDialog</c> when the user answers.</summary>
    public void ResolveConfirm(bool result)
    {
        ConfirmRequest? current;
        lock (_gate)
        {
            current = PendingConfirm;
            PendingConfirm = _pendingConfirms.Count > 0 ? _pendingConfirms.Dequeue() : null;
        }

        current?.Completion.TrySetResult(result);
        OnChange?.Invoke();
    }
}
