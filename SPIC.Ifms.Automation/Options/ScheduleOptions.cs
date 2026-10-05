namespace SPIC.Ifms.Automation.Options
{
	/// <summary>
	/// When the nightly run fires and how hard it tries before giving up.
	/// </summary>
	public sealed class ScheduleOptions
	{
		public const string SectionName = "Schedule";

		public bool Enabled { get; set; } = true;

		/// <summary>Local time of day for the run, HH:mm. IFMS opens at 04:00.</summary>
		public string RunAt { get; set; } = "04:05";

		/// <summary>
		/// Windows uses "India Standard Time", Linux uses "Asia/Kolkata".
		/// Both are accepted; the resolver tries the other id if the first misses,
		/// so the same appsettings.json works on your PC and on the VPS.
		/// </summary>
		public string TimeZone { get; set; } = "Asia/Kolkata";

		/// <summary>
		/// Run once at startup as well. Useful the first day and after a redeploy;
		/// leave false in production so a restart at noon does not refetch.
		/// </summary>
		public bool RunOnStartup { get; set; }

		/// <summary>
		/// If the host was down at 04:05, run as soon as it comes back — provided
		/// we are still inside CatchUpWindowHours of the scheduled time.
		/// </summary>
		public bool CatchUpMissedRuns { get; set; } = true;
		public int CatchUpWindowHours { get; set; } = 6;

		/// <summary>Whole-run attempts before the night is declared a failure.</summary>
		public int MaxAttempts { get; set; } = 3;

		/// <summary>Minutes between whole-run attempts.</summary>
		public int RetryDelayMinutes { get; set; } = 15;

		/// <summary>
		/// The portal is not reliably up at 04:00. Before attempting a login the
		/// automation polls the site for up to this long.
		/// </summary>
		public int SiteProbeMaxWaitMinutes { get; set; } = 60;
		public int SiteProbeIntervalSeconds { get; set; } = 60;
		public int SiteProbeTimeoutSeconds { get; set; } = 30;

		/// <summary>
		/// Which business date the reports are pulled for, relative to the run date.
		/// -1 means "yesterday", which is what a 04:05 run normally wants.
		/// Individual jobs can override this.
		/// </summary>
		public int ReportDateOffsetDays { get; set; } = -1;
	}
}
