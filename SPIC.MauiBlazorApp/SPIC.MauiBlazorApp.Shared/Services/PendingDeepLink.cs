namespace SPIC.MauiBlazorApp.Shared.Services;

/// <summary>
/// Static hand-off for "the launch Intent wants to open a specific page" - currently used by
/// tapping the JMDO live-tracking foreground-service notification to reopen the app on
/// FieldDashboard instead of wherever it would otherwise cold-start.
///
/// Platform code (Android MainActivity.OnCreate, the cold-start case - the process was killed, so
/// there's no Blazor circuit yet to hand this to directly) sets Route before any circuit exists.
/// Routes.razor consumes and clears it on its own first render - the same point that already
/// signals the native boot overlay to come down (see notifyAppReady) - since that's the earliest
/// the circuit can act on anything.
///
/// The warm case (app process still alive, just backgrounded) doesn't need this at all:
/// MainActivity.OnNewIntent calls NavHistoryService.Current.NavigateTo(...) directly, since the
/// circuit already exists then.
/// </summary>
public static class PendingDeepLink
{
    public static string? Route { get; set; }
}
