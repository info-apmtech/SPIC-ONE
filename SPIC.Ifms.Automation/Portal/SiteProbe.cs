using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPIC.Ifms.Automation.Options;

namespace SPIC.Ifms.Automation.Portal
{
	public interface ISiteProbe
	{
		/// <summary>Single cheap check. True when the portal answers.</summary>
		Task<bool> IsUpAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Polls until the portal answers or the wait budget runs out. IFMS opens
		/// around 04:00 but not punctually, so the run starts at 04:05 and waits
		/// here rather than failing on the first refusal.
		/// </summary>
		Task<bool> WaitUntilUpAsync(CancellationToken cancellationToken);
	}

	public sealed class SiteProbe : ISiteProbe
	{
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly IfmsOptions _ifms;
		private readonly ScheduleOptions _schedule;
		private readonly ILogger<SiteProbe> _logger;

		public SiteProbe(
			IHttpClientFactory httpClientFactory,
			IOptions<IfmsOptions> ifms,
			IOptions<ScheduleOptions> schedule,
			ILogger<SiteProbe> logger)
		{
			_httpClientFactory = httpClientFactory;
			_ifms = ifms.Value;
			_schedule = schedule.Value;
			_logger = logger;
		}

		public async Task<bool> IsUpAsync(CancellationToken cancellationToken)
		{
			var url = BuildProbeUrl();

			try
			{
				var client = _httpClientFactory.CreateClient("ifms-probe");
				client.Timeout = TimeSpan.FromSeconds(Math.Max(5, _schedule.SiteProbeTimeoutSeconds));

				// GET rather than HEAD: several NIC portals answer HEAD with 405
				// while serving the page perfectly well.
				using var request = new HttpRequestMessage(HttpMethod.Get, url);
				using var response = await client.SendAsync(
					request,
					HttpCompletionOption.ResponseHeadersRead,
					cancellationToken);

				var up = (int)response.StatusCode < 500;

				_logger.LogDebug(
					"Probe of {Url} returned {Status}; treating the portal as {State}.",
					url, (int)response.StatusCode, up ? "up" : "down");

				return up;
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogDebug("Probe of {Url} failed: {Message}", url, ex.Message);
				return false;
			}
		}

		public async Task<bool> WaitUntilUpAsync(CancellationToken cancellationToken)
		{
			var deadline = DateTime.UtcNow.AddMinutes(Math.Max(1, _schedule.SiteProbeMaxWaitMinutes));
			var interval = TimeSpan.FromSeconds(Math.Max(10, _schedule.SiteProbeIntervalSeconds));
			var attempt = 0;

			while (true)
			{
				cancellationToken.ThrowIfCancellationRequested();
				attempt++;

				if (await IsUpAsync(cancellationToken))
				{
					_logger.LogInformation("IFMS portal is up (probe {Attempt}).", attempt);
					return true;
				}

				if (DateTime.UtcNow >= deadline)
				{
					_logger.LogError(
						"IFMS portal did not come up within {Minutes} minutes ({Attempts} probes).",
						_schedule.SiteProbeMaxWaitMinutes, attempt);
					return false;
				}

				_logger.LogInformation(
					"IFMS portal is not answering yet; retrying in {Seconds}s.",
					interval.TotalSeconds);

				await Task.Delay(interval, cancellationToken);
			}
		}

		private string BuildProbeUrl()
		{
			var path = string.IsNullOrWhiteSpace(_ifms.HealthCheckPath)
				? _ifms.LoginPath
				: _ifms.HealthCheckPath;

			return IfmsUrl.Absolute(_ifms.BaseUrl, path);
		}
	}
}
