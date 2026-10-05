using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Services.Telemetry;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Metrics page read API (docs/metrics-telemetry-plan.md section 4; contract and route list in
	/// SPIC.Core/DTOs/MetricsDtos.cs). Access: page key "Metrics" on an active designation, or role
	/// SuperAdmin / Admin / CorporateAdmin (MetricsAccess); anyone else gets 403 { Success, Message }.
	/// Queries live in MetricsQueryService (today live from the raw tables, earlier days from the
	/// daily rollups, every query AsNoTracking).
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/Metrics")]
	public class MetricsController : ControllerBase
	{
		private readonly MetricsAccess _access;
		private readonly MetricsQueryService _metrics;

		public MetricsController(MetricsAccess access, MetricsQueryService metrics)
		{
			_access = access;
			_metrics = metrics;
		}

		private async Task<ObjectResult?> DenyAsync(CancellationToken ct)
		{
			await _access.LoadAsync(User, ct);
			return _access.CanView
				? null
				: StatusCode(403, new { Success = false, Message = "You are not authorized to view metrics." });
		}

		private ObjectResult NotFoundMessage(string message) => NotFound(new { Success = false, Message = message });

		[HttpGet("summary")]
		public async Task<IActionResult> Summary([FromQuery] int? days, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.SummaryAsync(days, ct));
		}

		[HttpGet("live")]
		public async Task<IActionResult> Live(CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.LiveAsync(ct));
		}

		[HttpGet("series")]
		public async Task<IActionResult> Series([FromQuery] int? days, [FromQuery] TelemetryApp? app, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.SeriesAsync(days, app, ct));
		}

		[HttpGet("pages")]
		public async Task<IActionResult> Pages([FromQuery] int? days, [FromQuery] TelemetryApp? app, [FromQuery] int? top, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.RoutesAsync(TelemetryRouteKind.Page, days, app, top, "hits", ct));
		}

		/// <summary>sort = hits (default) | slow | errors.</summary>
		[HttpGet("endpoints")]
		public async Task<IActionResult> Endpoints([FromQuery] int? days, [FromQuery] TelemetryApp? app, [FromQuery] int? top,
			[FromQuery] string? sort, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.RoutesAsync(TelemetryRouteKind.Endpoint, days, app, top, sort, ct));
		}

		[HttpGet("users")]
		public async Task<IActionResult> Users([FromQuery] int? days, [FromQuery] string? q, [FromQuery] string? role,
			[FromQuery] TelemetryApp? app, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.UsersAsync(days, q, role, app, page, pageSize, ct));
		}

		/// <summary>resolved = true | false | omitted (all).</summary>
		[HttpGet("errors")]
		public async Task<IActionResult> Errors([FromQuery] int? days, [FromQuery] string? q, [FromQuery] TelemetrySource? source,
			[FromQuery] TelemetryApp? app, [FromQuery] bool? resolved, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			return Ok(await _metrics.ErrorsAsync(days, q, source, app, resolved, page, pageSize, ct));
		}

		[HttpGet("errors/{id:long}")]
		public async Task<IActionResult> Error(long id, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			var detail = await _metrics.ErrorDetailAsync(id, ct);
			return detail is null ? NotFoundMessage("Error not found.") : Ok(detail);
		}

		[HttpPost("errors/{fingerprint}/resolve")]
		public async Task<IActionResult> Resolve(string fingerprint, [FromBody] MetricsResolveDto? body, CancellationToken ct)
		{
			if (await DenyAsync(ct) is { } denied) return denied;
			var by = !string.IsNullOrWhiteSpace(_access.UserName) ? _access.UserName : _access.UserId;
			var group = await _metrics.ResolveAsync(fingerprint, body?.Resolved ?? true, by, ct);
			return group is null ? NotFoundMessage("Error group not found.") : Ok(group);
		}
	}
}
