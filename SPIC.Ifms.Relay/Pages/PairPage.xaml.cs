using SPIC.Ifms.Relay.Platforms.Android;
using SPIC.Ifms.Relay.Services;
using AndroidApp = Android.App.Application;

namespace SPIC.Ifms.Relay.Pages
{
	/// <summary>
	/// Visited once, when the phone is set up: pair it with the server, and
	/// decide which messages are allowed to leave it.
	/// </summary>
	public partial class PairPage : ContentPage
	{
		private bool _working;
		private bool _loading;

		public PairPage()
		{
			InitializeComponent();
		}

		protected override void OnAppearing()
		{
			base.OnAppearing();

			_loading = true;

			ApiBaseEntry.Text = RelaySettings.ApiBase;
			ForwardOnlyIfmsSwitch.IsToggled = RelaySettings.ForwardOnlyIfms;
			AcceptedSendersEntry.Text = RelaySettings.AcceptedSenders;

			_loading = false;

			RefreshPairedState();
		}

		private void RefreshPairedState()
		{
			var paired = RelaySettings.IsConfigured;

			PairTitle.Text = paired ? "Update pairing" : "Pair this phone";
			PairButton.Text = paired ? "Save" : "Pair";
			UnpairButton.IsVisible = paired;
		}

		private async void OnPairClicked(object? sender, EventArgs e)
		{
			if (_working)
				return;

			_working = true;
			PairButton.IsEnabled = false;
			PairButton.Text = "Working…";
			ResultLabel.IsVisible = false;

			try
			{
				var apiBase = (ApiBaseEntry.Text ?? string.Empty).Trim();
				var pairingKey = (DeviceKeyEntry.Text ?? string.Empty).Trim();

				if (pairingKey.Length == 0)
				{
					Show(false, "Enter the device key from the server's IfmsAutomation:DeviceKey setting.");
					return;
				}

				if (!Uri.TryCreate(apiBase, UriKind.Absolute, out var uri) ||
					(uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
				{
					Show(false, "That API address is not a valid URL.");
					return;
				}

				var sms = await Permissions.RequestAsync<SmsPermission>();

				if (sms != PermissionStatus.Granted)
				{
					Show(false,
						"Android did not grant permission to read SMS, so the OTP cannot be forwarded. " +
						"Allow it under Settings, Apps, SPIC IFMS Relay, Permissions, SMS.");
					return;
				}

				// Android 13+ silently drops notifications without this, which
				// would hide the CAPTCHA prompt entirely.
				if (OperatingSystem.IsAndroidVersionAtLeast(33))
					await Permissions.RequestAsync<Permissions.PostNotifications>();

				RelaySettings.ApiBase = apiBase;

				// Register before switching the relay on. A phone that failed to
				// pair must not sit there believing it is working.
				var result = await RelayClient.RegisterAsync(apiBase, pairingKey);

				if (!result.Success)
				{
					RelayLog.Fail($"Pairing failed: {result.Message}");
					Show(false, result.Message);
					return;
				}

				RelaySettings.Enabled = true;
				DeviceKeyEntry.Text = string.Empty;

				RelayLog.Ok($"Paired as {RelaySettings.DeviceId}");
				RelayForegroundService.EnsureRunning(AndroidApp.Context);

				Show(true,
					$"{result.Message} Registered as {RelaySettings.DeviceId}. This phone will now forward " +
					"the IFMS OTP and alert you if a CAPTCHA needs typing. Next: on the Home tab, allow " +
					"background running.");
			}
			finally
			{
				_working = false;
				PairButton.IsEnabled = true;
				RefreshPairedState();
			}
		}

		private void OnUnpairClicked(object? sender, EventArgs e)
		{
			if (_working)
				return;

			RelaySettings.Enabled = false;
			RelaySettings.DeviceToken = string.Empty;
			RelayForegroundService.Stop(AndroidApp.Context);

			RelayLog.Info("Relay turned off");
			Show(true, "Relay turned off. This phone will no longer forward messages.");
			RefreshPairedState();
		}

		private void OnForwardOnlyToggled(object? sender, ToggledEventArgs e)
		{
			if (_loading)
				return;

			RelaySettings.ForwardOnlyIfms = e.Value;

			RelayLog.Info(e.Value
				? "Filter on: only IFMS messages are forwarded"
				: "Filter off: every SMS is forwarded");

			FilterSaved(e.Value
				? "Saved. Only IFMS messages will be forwarded."
				: "Saved. Every SMS this phone receives will be forwarded.");
		}

		private void OnAcceptedSendersChanged(object? sender, EventArgs e)
		{
			if (_loading)
				return;

			var value = (AcceptedSendersEntry.Text ?? string.Empty).Trim();

			if (value == RelaySettings.AcceptedSenders)
				return;

			RelaySettings.AcceptedSenders = value;

			var count = RelaySettings.AcceptedSenderDigits().Count;
			FilterSaved(count == 0
				? "Saved. No accepted senders: only messages mentioning IFMS will be forwarded."
				: $"Saved. {count} accepted sender{(count == 1 ? "" : "s")}.");
		}

		private void FilterSaved(string message)
		{
			FilterSavedLabel.Text = message;
			FilterSavedLabel.IsVisible = true;
		}

		private void Show(bool good, string message)
		{
			ResultLabel.Text = message;
			ResultLabel.TextColor = (Color)Application.Current!.Resources[good ? "Good" : "Bad"];
			ResultLabel.IsVisible = true;
		}
	}
}
