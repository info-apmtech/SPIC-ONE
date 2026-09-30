using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Services.Telemetry;
using SPIC.Core.DTOs;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Telemetry ingest (docs/metrics-telemetry-plan.md section 4; contract SPIC.Core/DTOs/MetricsDtos.cs).
	/// POST api/Telemetry/batch takes a bearer token (rows attributed to the caller), X-Telemetry-Key
	/// (server-attributed, the web host) or nothing (errors only, 5 per batch, 60 batches / minute / IP).
	/// Rules live in TelemetryIngestService. Bad rows are dropped, never answered with a 500; the
	/// request middleware does not record calls to this controller.
	/// </summary>
	[ApiController]
	[Route("api/Telemetry")]
	public class TelemetryController : ControllerBase
	{
		private readonly TelemetryIngestService _ingest;

		public TelemetryController(TelemetryIngestService ingest) => _ingest = ingest;

		[AllowAnonymous]
		[HttpPost("batch")]
		[RequestSizeLimit(2_000_000)]
		public IActionResult Batch([FromBody] ClientTelemetryBatchDto? batch)
		{
			try
			{
				var result = _ingest.Ingest(HttpContext, batch);
				if (result.RateLimited)
					return StatusCode(429, new { Success = false, Message = "Too many telemetry batches; try again in a minute." });
				return StatusCode(202, new { accepted = result.Accepted, dropped = result.Dropped });
			}
			catch
			{
				var offered = (batch?.PageViews?.Count ?? 0) + (batch?.Errors?.Count ?? 0);
				return StatusCode(202, new { accepted = 0, dropped = offered });
			}
		}
	}
}
