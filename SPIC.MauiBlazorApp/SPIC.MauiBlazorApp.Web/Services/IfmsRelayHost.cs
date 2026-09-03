using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.MauiBlazorApp.Web.Services
{
	/// <summary>
	/// The web head cannot read SMS, so pairing is not offered here. The setup
	/// page uses this to explain that the relay lives on the Android app rather
	/// than showing a form that could never work.
	/// </summary>
	public sealed class IfmsRelayHost : IIfmsRelayHost
	{
		public bool IsSupported => false;

		public IfmsRelayStatus GetStatus() => IfmsRelayStatus.Unsupported;

		public Task<IfmsRelayPairResult> PairAsync(string apiBase, string deviceKey) =>
			Task.FromResult(new IfmsRelayPairResult
			{
				Success = false,
				Message = "The OTP relay has to be paired on the SPIC Android app, on the phone holding the IFMS SIM."
			});

		public Task UnpairAsync() => Task.CompletedTask;
	}
}
