using Android.App;
using Android.Content;

namespace SPIC.MauiBlazorApp.Platforms.Android
{
    /// <summary>
    /// Fired by the AlarmManager alarm JmdoTrackingForegroundService arms while Live Tracking is
    /// active - a periodic self-healing check for the case the process was killed in a way that
    /// skipped OnTaskRemoved entirely (some OEM task-manager "clear all" kills SIGKILL the process
    /// without running any component's normal shutdown lifecycle). See
    /// JmdoTrackingForegroundService.WatchdogCheck for what actually happens on each fire.
    /// </summary>
    [BroadcastReceiver(Enabled = true, Exported = false)]
    public sealed class TrackingWatchdogReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (context is null) return;
            JmdoTrackingForegroundService.WatchdogCheck(context);
        }
    }
}
