using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SpicAPI.Services;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Razorpay webhook receiver for Guest House cancellation refunds.
	///
	/// POST /api/RazorpayWebhook - register this URL in the Razorpay Dashboard with the events
	/// refund.created, refund.processed, refund.failed and refund.speed_changed, and set the
	/// same secret as Razorpay__WebhookSecret.
	///
	/// Security: anonymous by necessity (Razorpay cannot send a JWT); every request is
	/// authenticated instead by X-Razorpay-Signature = HMAC_SHA256(raw body, webhook secret).
	/// Unsigned / wrongly signed requests are rejected with 400 and change nothing.
	///
	/// Idempotency: state-based - a refund leaves Processing at most once, so redelivered,
	/// duplicate or out-of-order events are no-ops (see GuestHouseRefundStatusSync).
	///
	/// Reconciliation: an event for a refund id not yet saved locally is matched by payment id +
	/// amount (+ our notes.reason) to an interrupted approval and finalises it. 503 (redeliver) is
	/// returned only while that approval is still inside its in-flight window.
	///
	/// Only refund.* events are handled; the payment/order/verify flow is not involved.
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[AllowAnonymous]
	public class RazorpayWebhookController : ControllerBase
	{
		private static readonly HashSet<string> RefundEvents = new(StringComparer.OrdinalIgnoreCase)
		{
			"refund.created",
			"refund.processed",
			"refund.failed",
			"refund.speed_changed"
		};

		private readonly AppDbContext _db;
		private readonly IRazorpayService _razorpay;
		private readonly ILogger<RazorpayWebhookController> _logger;

		public RazorpayWebhookController(AppDbContext db, IRazorpayService razorpay, ILogger<RazorpayWebhookController> logger)
		{
			_db = db;
			_razorpay = razorpay;
			_logger = logger;
		}

		[HttpPost]
		[RequestSizeLimit(256 * 1024)]
		public async Task<IActionResult> Receive()
		{
			// The signature is over the exact raw body - read it before anything parses it.
			string rawBody;
			using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
				rawBody = await reader.ReadToEndAsync();

			var signature = Request.Headers["X-Razorpay-Signature"].ToString();
			var eventId = Request.Headers["x-razorpay-event-id"].ToString();

			if (!_razorpay.VerifyWebhookSignature(rawBody, signature))
			{
				_logger.LogWarning("Rejected Razorpay webhook {EventId}: invalid or missing signature.", eventId);
				return BadRequest();
			}

			string? eventName;
			JsonElement refundEntity;
			string? paymentId = null;
			try
			{
				using var doc = JsonDocument.Parse(rawBody);
				var root = doc.RootElement;
				eventName = root.TryGetProperty("event", out var ev) ? ev.GetString() : null;

				if (eventName == null || !RefundEvents.Contains(eventName))
					return Ok(); // subscribed to something we do not handle - acknowledge and ignore

				if (!root.TryGetProperty("payload", out var payload)
					|| !payload.TryGetProperty("refund", out var refundWrapper)
					|| !refundWrapper.TryGetProperty("entity", out var entity))
				{
					_logger.LogWarning("Razorpay webhook {EventId} ({Event}) had no refund entity.", eventId, eventName);
					return Ok();
				}

				refundEntity = entity.Clone();
				if (refundEntity.TryGetProperty("payment_id", out var pid) && pid.ValueKind == JsonValueKind.String)
					paymentId = pid.GetString();
			}
			catch (JsonException)
			{
				_logger.LogWarning("Razorpay webhook {EventId} body was not valid JSON.", eventId);
				return BadRequest();
			}

			var refund = RazorpayService.ParseRefundEntity(refundEntity);

			GuestHouseRefundStatusSync.WebhookOutcome outcome;
			try
			{
				outcome = await GuestHouseRefundStatusSync.ApplyWebhookRefundAsync(_db, refund, paymentId);
			}
			catch (Exception ex)
			{
				// Non-2xx makes Razorpay redeliver; state-based idempotency makes that safe.
				_logger.LogError(ex, "Razorpay webhook {EventId} ({Event}) for refund {RefundId} failed to apply.", eventId, eventName, refund.RefundId);
				return StatusCode(500);
			}

			_logger.LogInformation("Razorpay webhook {EventId} ({Event}) refund {RefundId} status {Status}: {Outcome}.",
				eventId, eventName, refund.RefundId, refund.Status, outcome);

			return outcome == GuestHouseRefundStatusSync.WebhookOutcome.RetryLater
				? StatusCode(503)
				: Ok();
		}
	}
}
