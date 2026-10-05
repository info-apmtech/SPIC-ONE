using System;

namespace SPIC.Ifms.Automation.Portal
{
	/// <summary>
	/// Turns a configured path into an absolute portal URL.
	/// </summary>
	public static class IfmsUrl
	{
		/// <summary>
		/// Joins <paramref name="baseUrl"/> and <paramref name="pathOrUrl"/>, leaving
		/// an already-absolute http(s) address alone.
		///
		/// The scheme check is the whole point, and it is not defensive padding.
		/// <c>Uri.TryCreate("/mFMS/loginNew.action", UriKind.Absolute, …)</c> returns
		/// <c>false</c> on Windows but <c>true</c> on Linux, where a leading slash is
		/// a valid absolute file path — so it yields <c>file:///mFMS/loginNew.action</c>
		/// and the base URL is silently dropped.
		///
		/// That difference does not show up in development on Windows. It showed up
		/// on the server as a site probe politely retrying a local file every sixty
		/// seconds, having never touched the portal at all.
		/// </summary>
		public static string Absolute(string? baseUrl, string? pathOrUrl)
		{
			pathOrUrl ??= string.Empty;

			if (Uri.TryCreate(pathOrUrl, UriKind.Absolute, out var absolute) &&
				(absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
			{
				return absolute.ToString();
			}

			var root = (baseUrl ?? string.Empty).TrimEnd('/');

			if (root.Length == 0)
			{
				throw new InvalidOperationException(
					$"Ifms:BaseUrl is not set, so '{pathOrUrl}' cannot be resolved to a portal address.");
			}

			return $"{root}/{pathOrUrl.TrimStart('/')}";
		}
	}
}
