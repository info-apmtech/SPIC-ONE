using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// Marks an API endpoint (an action, or a whole controller) that a signed-in <see cref="AppRole.Farmer"/>
	/// may call. Farmers sign themselves up with a phone OTP (FarmerAuthController), so a Farmer JWT is
	/// public: every endpoint that only says <c>[Authorize]</c> would otherwise be open to the internet.
	/// <see cref="FarmerDenyByDefaultFilter"/> therefore answers 403 to a Farmer on every signed-in endpoint
	/// WITHOUT this marker (docs/farmer-portal.md "Hardening (final fix wave)").
	/// <para>
	/// The marker only lets the request reach the action. Each marked action still does its own checks
	/// (role, ownership, scoping to the caller). The full list of marked endpoints is pinned by
	/// SPIC.RegressionHarness/FarmerPortalHardeningTests: adding a marker needs a deliberate test change.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
	public sealed class FarmerAccessAttribute : Attribute
	{
	}

	/// <summary>
	/// Global MVC filter: deny by default for the Farmer role. A request is refused with 403 when ALL hold:
	/// the caller is signed in, the caller's role claim is Farmer, the endpoint requires sign-in
	/// (an <c>[Authorize]</c> applies and no <c>[AllowAnonymous]</c>), and neither the action nor its
	/// controller carries <see cref="FarmerAccessAttribute"/>. Anonymous endpoints and every staff / dealer
	/// caller are untouched.
	/// </summary>
	public sealed class FarmerDenyByDefaultFilter : IAuthorizationFilter
	{
		/// <summary>The body a refused farmer gets (plain words; the farmer UI never calls these).</summary>
		public const string DeniedMessage = "This is not available in the farmer app.";

		private readonly ILogger<FarmerDenyByDefaultFilter> _logger;

		public FarmerDenyByDefaultFilter(ILogger<FarmerDenyByDefaultFilter> logger)
		{
			_logger = logger;
		}

		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var user = context.HttpContext.User;
			if (!IsFarmer(user)) return;
			if (IsReachableByFarmer(context.ActionDescriptor.EndpointMetadata)) return;

			// Route template and verb only: no user id, phone or query string in the log.
			_logger.LogInformation("Farmer role refused on {Method} {Route} (no [FarmerAccess])",
				context.HttpContext.Request.Method,
				context.ActionDescriptor.AttributeRouteInfo?.Template ?? context.ActionDescriptor.DisplayName);

			context.Result = new ObjectResult(new { message = DeniedMessage }) { StatusCode = StatusCodes.Status403Forbidden };
		}

		/// <summary>True when the signed-in principal's role claim is Farmer.</summary>
		public static bool IsFarmer(ClaimsPrincipal? user)
		{
			if (user?.Identity?.IsAuthenticated != true) return false;
			return user.Claims.Any(c =>
				(c.Type == ClaimTypes.Role || c.Type == "role")
				&& string.Equals(c.Value, nameof(AppRole.Farmer), StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// The decision on an endpoint's metadata (controller + action attributes): anonymous endpoints
		/// (no <c>[Authorize]</c>, or an <c>[AllowAnonymous]</c>) and endpoints marked
		/// <see cref="FarmerAccessAttribute"/> are reachable; every other signed-in endpoint is not.
		/// Free of HTTP types so the harness can walk every controller by reflection with the same rule.
		/// </summary>
		public static bool IsReachableByFarmer(IEnumerable<object> metadata)
		{
			var list = metadata as IList<object> ?? metadata.ToList();
			if (list.OfType<IAllowAnonymous>().Any()) return true;
			if (!list.OfType<IAuthorizeData>().Any()) return true;
			return list.OfType<FarmerAccessAttribute>().Any();
		}

		/// <summary>True when the endpoint needs a signed-in caller (so the Farmer rule applies to it at all).</summary>
		public static bool RequiresSignIn(IEnumerable<object> metadata)
		{
			var list = metadata as IList<object> ?? metadata.ToList();
			return list.OfType<IAuthorizeData>().Any() && !list.OfType<IAllowAnonymous>().Any();
		}
	}
}
