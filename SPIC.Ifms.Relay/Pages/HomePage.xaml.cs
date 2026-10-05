using SPIC.Ifms.Relay.Platforms.Android;
using SPIC.Ifms.Relay.Services;

namespace SPIC.Ifms.Relay.Pages
{
	/// <summary>
	/// The screen the person sees when the phone rings at four in the morning:
	/// the CAPTCHA, if one is waiting, then the health of the relay. It polls the
	/// server every fifteen seconds while visible because a CAPTCHA prompt is
	/// time-limited and must be noticed without anybody pressing Refresh.
	/// </summary>
	public partial class HomePage : ContentPage
	{
		private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

		private readonly IDispatcherTimer _poller;
		private readonly IDispatcherTimer _countdown;

		private PendingChallenge? _challenge;
		private int _secondsRemaining;
		private bool _refreshing;
		private bool _sending;

		public HomePage()
		{
			InitializeComponent();

			// Capitals by default and no suggestions: a CAPTCHA is not a word.
			CaptchaEntry.Keyboard = Keyboard.Create(KeyboardFlags.CapitalizeCharacter);

			_poller = Dispatcher.CreateTimer();
			_poller.Interval = PollInterval;
			_poller.Tick += async (_, _) => await RefreshServerAsync();

			_countdown = Dispatcher.CreateTimer();
			_countdown.Interval = TimeSpan.FromSeconds(1);
			_countdown.Tick += (_, _) => TickCountdown();
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();

			RelayLog.Changed += OnLogChanged;
			RelayEvents.ChallengeChanged += OnChallengeChanged;

			// A tap on the notification while the app was closed lands here.
			var jumpToCaptcha = RelayEvents.ConsumeShowCaptcha();

			RefreshLocal();
			await RefreshServerAsync();

			if (jumpToCaptcha && CaptchaCard.IsVisible)
				CaptchaEntry.Focus();

			_poller.Start();
			_countdown.Start();
		}

		protected override void OnDisappearing()
		{
			_poller.Stop();
			_countdown.Stop();

			RelayLog.Changed -= OnLogChanged;
			RelayEvents.ChallengeChanged -= OnChallengeChanged;

			base.OnDisappearing();
		}

		private void OnLogChanged() =>
			MainThread.BeginInvokeOnMainThread(RefreshActivity);

		private void OnChallengeChanged() =>
			MainThread.BeginInvokeOnMainThread(async () => await RefreshServerAsync());

		// ------------------------------------------------------------- refresh

		/// <summary>Everything that can be answered without the network.</summary>
		private async void RefreshLocal()
		{
			var configured = RelaySettings.IsConfigured;

			SetYesNo(RelayValue, configured, "On", "Off");
			DeviceIdValue.Text = RelaySettings.DeviceId;
			LastContactValue.Text = Describe(RelaySettings.LastContactUtc);
			LastSmsValue.Text = Describe(RelaySettings.LastSmsRelayedUtc);
			SetYesNo(BatteryValue, BatteryOptimisation.IsIgnored(), "Ignored", "Still on");
			SetYesNo(WatcherValue, RelayForegroundService.IsRunning, "Running", "Stopped");

			RefreshActivity();

			try
			{
				var sms = await Permissions.CheckStatusAsync<SmsPermission>();
				SetYesNo(SmsValue, sms == PermissionStatus.Granted, "Granted", "Not granted");

				var notify = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
				SetYesNo(NotifyValue, notify == PermissionStatus.Granted, "Allowed", "Blocked");
			}
			catch
			{
				SmsValue.Text = "?";
				NotifyValue.Text = "?";
			}
		}

		private async Task RefreshServerAsync()
		{
			if (_refreshing)
				return;

			_refreshing = true;

			try
			{
				if (!RelaySettings.IsConfigured)
				{
					SetHeadline("Not paired. Open the Pair tab to set this phone up.", attention: true);
					ApplyChallenge(null);
					return;
				}

				var status = await RelayClient.GetStatusAsync();

				if (!status.Success || status.Value is null)
				{
					SetHeadline(status.Message, attention: true);
					return;
				}

				SetHeadline(status.Value.Headline, status.Value.NeedsAttention);
				ApplyChallenge(status.Value.PendingChallenge);
			}
			finally
			{
				_refreshing = false;

				LastContactValue.Text = Describe(RelaySettings.LastContactUtc);
				SetYesNo(WatcherValue, RelayForegroundService.IsRunning, "Running", "Stopped");
			}
		}

		private void RefreshActivity()
		{
			var entries = RelayLog.Entries;

			NoActivityLabel.IsVisible = entries.Count == 0;
			BindableLayout.SetItemsSource(ActivityList, entries);
		}

		// ------------------------------------------------------------- captcha

		private void ApplyChallenge(PendingChallenge? challenge)
		{
			if (challenge is null)
			{
				if (_challenge is not null)
				{
					_challenge = null;
					CaptchaEntry.Text = string.Empty;
					CaptchaResultLabel.IsVisible = false;
					CaptchaImage.Source = null;
				}

				CaptchaCard.IsVisible = false;
				return;
			}

			var isNew = _challenge?.Id != challenge.Id;
			_challenge = challenge;
			_secondsRemaining = challenge.SecondsRemaining;

			if (isNew)
			{
				CaptchaEntry.Text = string.Empty;
				CaptchaResultLabel.IsVisible = false;
				CaptchaImage.Source = DecodeImage(challenge.ImageBase64);
			}

			FailedGuessesLabel.IsVisible = !string.IsNullOrWhiteSpace(challenge.FailedGuesses);
			FailedGuessesLabel.Text = $"The solver read it as: {challenge.FailedGuesses}";

			CaptchaCard.IsVisible = true;
			TickCountdown();

			if (isNew)
				CaptchaEntry.Focus();
		}

		private static ImageSource? DecodeImage(string? base64)
		{
			if (string.IsNullOrWhiteSpace(base64))
				return null;

			try
			{
				var bytes = Convert.FromBase64String(base64);
				return ImageSource.FromStream(() => new MemoryStream(bytes));
			}
			catch (FormatException)
			{
				return null;
			}
		}

		private void TickCountdown()
		{
			if (!CaptchaCard.IsVisible)
				return;

			if (_secondsRemaining > 0)
				_secondsRemaining--;

			CountdownLabel.Text = _secondsRemaining > 0
				? $"expires in {TimeSpan.FromSeconds(_secondsRemaining):mm\\:ss}"
				: "expired — a fresh one is on its way";
		}

		private async void OnSendClicked(object? sender, EventArgs e)
		{
			if (_challenge is null || _sending)
				return;

			var answer = (CaptchaEntry.Text ?? string.Empty).Trim();

			if (answer.Length == 0)
				return;

			_sending = true;
			SendButton.IsEnabled = false;
			SendButton.Text = "Sending…";

			try
			{
				var challengeId = _challenge.Id;
				var result = await RelayClient.AnswerChallengeAsync(challengeId, answer);

				ShowResult(CaptchaResultLabel, result.Success, result.Message);

				if (result.Success)
				{
					RelayLog.Ok($"CAPTCHA answer sent for challenge {challengeId}");
					_challenge = null;
					CaptchaCard.IsVisible = false;
					CaptchaEntry.Text = string.Empty;
				}
				else
				{
					// The common case is answering just after it expired, which is
					// recoverable: the automation sends a fresh image straight away.
					RelayLog.Warn($"CAPTCHA answer rejected: {result.Message}");
				}

				await RefreshServerAsync();
			}
			finally
			{
				_sending = false;
				SendButton.IsEnabled = true;
				SendButton.Text = "Send";
			}
		}

		// ------------------------------------------------------------- buttons

		private async void OnRefreshClicked(object? sender, EventArgs e)
		{
			ActionResultLabel.IsVisible = false;
			RefreshLocal();
			await RefreshServerAsync();
		}

		private async void OnTestConnectionClicked(object? sender, EventArgs e)
		{
			ShowResult(ActionResultLabel, true, "Testing…");

			var result = await RelayClient.TestConnectionAsync();

			ShowResult(ActionResultLabel, result.Success, result.Message);

			if (result.Success)
				RelayLog.Ok("Test connection OK");
			else
				RelayLog.Fail($"Test connection failed: {result.Message}");

			LastContactValue.Text = Describe(RelaySettings.LastContactUtc);
		}

		private void OnAllowBackgroundClicked(object? sender, EventArgs e)
		{
			var message = BatteryOptimisation.RequestIgnore();
			ShowResult(ActionResultLabel, true, message);
		}

		private void OnOpenBatterySettingsClicked(object? sender, EventArgs e)
		{
			var message = BatteryOptimisation.OpenAppSettings();
			ShowResult(ActionResultLabel, true, message);
		}

		// ------------------------------------------------------------- helpers

		private void SetHeadline(string text, bool attention)
		{
			HeadlineLabel.Text = text;
			HeadlineLabel.TextColor = (Color)Application.Current!.Resources[attention ? "Warn" : "TextMuted"];
		}

		private static void SetYesNo(Label label, bool good, string yes, string no)
		{
			label.Text = good ? yes : no;
			label.TextColor = (Color)Application.Current!.Resources[good ? "Good" : "Bad"];
		}

		private static void ShowResult(Label label, bool good, string message)
		{
			label.Text = message;
			label.TextColor = (Color)Application.Current!.Resources[good ? "Good" : "Bad"];
			label.IsVisible = true;
		}

		private static string Describe(DateTime? utc) =>
			utc is null ? "never" : utc.Value.ToLocalTime().ToString("dd MMM HH:mm");
	}
}
