using SPIC.Ifms.Relay.Services;

namespace SPIC.Ifms.Relay.Pages
{
	/// <summary>
	/// Who the nightly automation emails, and through which mail server. The
	/// settings live on the server: this page only reads them back, edits them
	/// and asks for a test. The SMTP password is the one thing that passes
	/// through here without coming back — it is typed, sent once in the PUT,
	/// and cleared; nothing on the phone remembers it.
	/// </summary>
	public partial class AlertsPage : ContentPage
	{
		private const int DefaultSmtpPort = 587;

		/// <summary>The automation promises a test within ~20 s; poll gently, give up at a minute.</summary>
		private static readonly TimeSpan TestPollInterval = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan TestPollLimit = TimeSpan.FromSeconds(60);

		private AlertSettings? _current;
		private bool _busy;
		private CancellationTokenSource? _testWatch;

		public AlertsPage()
		{
			InitializeComponent();
		}

		protected override async void OnAppearing()
		{
			base.OnAppearing();
			await LoadAsync();
		}

		protected override void OnDisappearing()
		{
			// A test poll left running would write to labels that are off screen;
			// the result is on the server anyway and shows on the next open.
			CancelTestWatch();
			base.OnDisappearing();
		}

		// ------------------------------------------------------------- load

		private async Task LoadAsync()
		{
			if (!RelaySettings.IsConfigured)
			{
				NotPairedCard.IsVisible = true;
				Form.IsEnabled = false;
				SaveButton.IsEnabled = false;
				SendTestButton.IsEnabled = false;
				_current = null;
				return;
			}

			NotPairedCard.IsVisible = false;
			Form.IsEnabled = true;

			if (_busy)
				return;

			SetBusy(true);
			ResultLabel.IsVisible = false;

			try
			{
				var result = await RelayClient.GetAlertSettingsAsync();

				if (!result.Success || result.Value is null)
				{
					Show(false, result.Message);
					return;
				}

				Apply(result.Value);
			}
			finally
			{
				SetBusy(false);
			}
		}

		/// <summary>
		/// Redraws every control from what the server holds. Also runs after a
		/// save, so what is on screen is always the saved state, never the draft.
		/// </summary>
		private void Apply(AlertSettings settings)
		{
			_current = settings;

			EmailEnabledSwitch.IsToggled = settings.EmailEnabled;
			SmtpHostEntry.Text = settings.SmtpHost ?? string.Empty;
			SmtpPortEntry.Text = (settings.SmtpPort > 0 ? settings.SmtpPort : DefaultSmtpPort).ToString();
			UseStartTlsSwitch.IsToggled = settings.UseStartTls;
			UserNameEntry.Text = settings.UserName ?? string.Empty;
			PasswordEntry.Text = string.Empty;
			PasswordStateLabel.Text = settings.HasPassword ? "Password saved" : "No password saved";
			PasswordStateLabel.TextColor = Colour(settings.HasPassword ? "Good" : "TextMuted");
			FromAddressEntry.Text = settings.FromAddress ?? string.Empty;
			FromNameEntry.Text = settings.FromName ?? string.Empty;
			ToAddressesEditor.Text = settings.ToAddresses ?? string.Empty;
			CcAddressesEditor.Text = settings.CcAddresses ?? string.Empty;
			FailuresOnlySwitch.IsToggled = settings.FailuresOnly;
			AttachReportsSwitch.IsToggled = settings.AttachReports;

			ShowStamp(LastTestLabel, "Last test", settings.LastTestAt, settings.LastTestResult);
			ShowStamp(LastAlertLabel, "Last alert", settings.LastSentAt, settings.LastSendResult);

			if (settings.UpdatedAt != default)
			{
				var by = string.IsNullOrWhiteSpace(settings.UpdatedBy) ? "unknown" : settings.UpdatedBy;
				UpdatedLabel.Text = $"Updated {Describe(settings.UpdatedAt)} by {by}";
				UpdatedLabel.IsVisible = true;
			}
			else
			{
				UpdatedLabel.IsVisible = false;
			}

			RefreshButtons();
		}

		/// <summary>
		/// Reads the form into a PUT body, or returns null after showing why it
		/// cannot. The only local check is the port; everything else (addresses,
		/// host reachability) is the server's to judge, and its message is shown.
		/// </summary>
		private AlertSettingsUpdate? Collect()
		{
			var portText = (SmtpPortEntry.Text ?? string.Empty).Trim();
			var port = DefaultSmtpPort;

			if (portText.Length > 0 && (!int.TryParse(portText, out port) || port < 1 || port > 65535))
			{
				Show(false, "The port must be a number between 1 and 65535 (587 is usual).");
				return null;
			}

			var password = PasswordEntry.Text ?? string.Empty;

			return new AlertSettingsUpdate
			{
				EmailEnabled = EmailEnabledSwitch.IsToggled,
				SmtpHost = (SmtpHostEntry.Text ?? string.Empty).Trim(),
				SmtpPort = port,
				UseStartTls = UseStartTlsSwitch.IsToggled,
				UserName = (UserNameEntry.Text ?? string.Empty).Trim(),
				Password = password.Length == 0 ? null : password,
				FromAddress = (FromAddressEntry.Text ?? string.Empty).Trim(),
				FromName = (FromNameEntry.Text ?? string.Empty).Trim(),
				ToAddresses = (ToAddressesEditor.Text ?? string.Empty).Trim(),
				CcAddresses = (CcAddressesEditor.Text ?? string.Empty).Trim(),
				FailuresOnly = FailuresOnlySwitch.IsToggled,
				AttachReports = AttachReportsSwitch.IsToggled
			};
		}

		// ------------------------------------------------------------- buttons

		private async void OnSaveClicked(object? sender, EventArgs e)
		{
			if (_busy || !RelaySettings.IsConfigured)
				return;

			var update = Collect();

			if (update is null)
				return;

			CancelTestWatch();
			SetBusy(true);
			SaveButton.Text = "Saving…";
			ResultLabel.IsVisible = false;

			try
			{
				var result = await RelayClient.SaveAlertSettingsAsync(update);

				if (!result.Success || result.Value is null)
				{
					// The server's own sentence, verbatim: it knows why it refused.
					RelayLog.Fail($"Alert settings not saved: {result.Message}");
					Show(false, result.Message);
					return;
				}

				// Apply() clears the password field; the typed value has done
				// its one job and there is no reason for it to stay on screen.
				Apply(result.Value);
				RelayLog.Ok(result.Value.EmailEnabled
					? "Email alert settings saved (alerts on)"
					: "Email alert settings saved (alerts off)");
				Show(true, "Saved.");
			}
			finally
			{
				SaveButton.Text = "Save";
				SetBusy(false);
			}
		}

		private async void OnSendTestClicked(object? sender, EventArgs e)
		{
			if (_busy || !RelaySettings.IsConfigured || _current is null)
				return;

			CancelTestWatch();
			SetBusy(true);
			SendTestButton.Text = "Requesting…";
			ResultLabel.IsVisible = false;

			var previousTestAt = _current.LastTestAt;
			var queued = false;

			try
			{
				var result = await RelayClient.RequestTestEmailAsync();

				if (!result.Success)
				{
					RelayLog.Fail($"Test email not queued: {result.Message}");
					Show(false, result.Message);
					return;
				}

				RelayLog.Info("Test email requested");
				Show(true, "Test queued…");
				queued = true;
			}
			finally
			{
				SendTestButton.Text = "Send test email";
				SetBusy(false);
			}

			if (queued)
				await WatchForTestResultAsync(previousTestAt);
		}

		/// <summary>
		/// Polls the settings until LastTestAt moves on from what it was before
		/// the request, then shows the server's verdict. Stops on its own after
		/// a minute or when the page goes away.
		/// </summary>
		private async Task WatchForTestResultAsync(DateTime? previousTestAt)
		{
			var watch = new CancellationTokenSource();
			_testWatch = watch;

			var deadline = DateTime.UtcNow + TestPollLimit;

			try
			{
				while (DateTime.UtcNow < deadline)
				{
					await Task.Delay(TestPollInterval, watch.Token);

					var poll = await RelayClient.GetAlertSettingsAsync(watch.Token);

					if (watch.IsCancellationRequested)
						return;

					if (!poll.Success || poll.Value is null)
						continue;

					var latest = poll.Value;

					if (latest.LastTestAt is null || latest.LastTestAt == previousTestAt)
						continue;

					// Not Apply(): that would overwrite anything typed meanwhile.
					// Only the stamps and the send-test gate come from the poll.
					_current = latest;
					ShowStamp(LastTestLabel, "Last test", latest.LastTestAt, latest.LastTestResult);
					ShowStamp(LastAlertLabel, "Last alert", latest.LastSentAt, latest.LastSendResult);
					RefreshButtons();

					var verdict = string.IsNullOrWhiteSpace(latest.LastTestResult)
						? "The test ran but the server recorded no result."
						: latest.LastTestResult;
					var sent = verdict.StartsWith("Sent", StringComparison.OrdinalIgnoreCase);

					if (sent)
						RelayLog.Ok($"Test email: {verdict}");
					else
						RelayLog.Warn($"Test email: {verdict}");

					Show(sent, verdict);
					return;
				}

				Show(false, "No result after a minute. The server may still be sending; reopen this tab to check.");
			}
			catch (OperationCanceledException)
			{
				// Page left or a new request started; nothing to report.
			}
			finally
			{
				if (ReferenceEquals(_testWatch, watch))
					_testWatch = null;

				watch.Dispose();
			}
		}

		private void CancelTestWatch()
		{
			try
			{
				_testWatch?.Cancel();
			}
			catch (ObjectDisposedException)
			{
				// Already finished and disposed on its own way out.
			}

			_testWatch = null;
		}

		// ------------------------------------------------------------- helpers

		private void SetBusy(bool busy)
		{
			_busy = busy;
			RefreshButtons();
		}

		/// <summary>
		/// Save needs a paired phone and no call in flight. Send test also needs
		/// a saved state with alerts on: testing settings that are not saved, or
		/// that are switched off, would only report on the wrong thing.
		/// </summary>
		private void RefreshButtons()
		{
			var paired = RelaySettings.IsConfigured;

			SaveButton.IsEnabled = paired && !_busy;
			SendTestButton.IsEnabled = paired && !_busy && _current is { EmailEnabled: true };
		}

		private static void ShowStamp(Label label, string caption, DateTime? at, string? result)
		{
			if (at is null)
			{
				label.IsVisible = false;
				return;
			}

			var text = string.IsNullOrWhiteSpace(result) ? "no result recorded" : result;
			label.Text = $"{caption}: {Describe(at.Value)} — {text}";
			label.IsVisible = true;
		}

		private void Show(bool good, string message)
		{
			ResultLabel.Text = message;
			ResultLabel.TextColor = Colour(good ? "Good" : "Bad");
			ResultLabel.IsVisible = true;
		}

		private static Color Colour(string key) =>
			(Color)Application.Current!.Resources[key];

		private static string Describe(DateTime utc) =>
			utc.ToLocalTime().ToString("dd MMM HH:mm");
	}
}
