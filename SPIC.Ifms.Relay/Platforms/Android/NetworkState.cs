using Android.Content;
using Android.Net;

namespace SPIC.Ifms.Relay.Platforms.Android
{
	/// <summary>
	/// What the phone's network looked like at the moment a call failed.
	///
	/// "Server unreachable" on its own cannot tell a dead Wi-Fi from a dead
	/// server, and two nights were lost arguing about which it was. One word
	/// in the activity log settles it.
	/// </summary>
	public static class NetworkState
	{
		public static string Describe(Context context)
		{
			try
			{
				if (context.GetSystemService(Context.ConnectivityService) is not ConnectivityManager cm)
					return "network unknown";

				var network = cm.ActiveNetwork;
				if (network is null)
					return "no network";

				var caps = cm.GetNetworkCapabilities(network);
				if (caps is null)
					return "network unknown";

				var kind = caps.HasTransport(TransportType.Wifi) ? "wifi"
					: caps.HasTransport(TransportType.Cellular) ? "mobile data"
					: "other network";

				var validated = caps.HasCapability(NetCapability.Validated) ? "" : ", no internet";

				return kind + validated;
			}
			catch
			{
				return "network unknown";
			}
		}
	}
}
