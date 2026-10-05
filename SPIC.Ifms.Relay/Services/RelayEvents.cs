namespace SPIC.Ifms.Relay.Services
{
	/// <summary>
	/// The thin seam between the Android pieces and the page. The watcher and the
	/// activity live outside MAUI's dependency injection, so a static event is
	/// the honest way for them to say "something changed, look again".
	///
	/// Events are raised on whatever thread noticed the change; subscribers
	/// marshal to the main thread themselves.
	/// </summary>
	public static class RelayEvents
	{
		/// <summary>The watcher saw a CAPTCHA appear or clear; the page should refresh.</summary>
		public static event Action? ChallengeChanged;

		/// <summary>
		/// The person tapped the CAPTCHA notification. Set before the page exists,
		/// consumed by the Home page when it appears, so the tap is not lost when
		/// the app was closed at the time.
		/// </summary>
		public static bool ShowCaptchaRequested { get; private set; }

		public static void RaiseChallengeChanged() => ChallengeChanged?.Invoke();

		public static void RequestShowCaptcha() => ShowCaptchaRequested = true;

		/// <summary>Returns whether a request was waiting, and clears it.</summary>
		public static bool ConsumeShowCaptcha()
		{
			var was = ShowCaptchaRequested;
			ShowCaptchaRequested = false;
			return was;
		}
	}
}
