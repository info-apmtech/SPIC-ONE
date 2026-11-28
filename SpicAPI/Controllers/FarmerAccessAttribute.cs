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
	/// Global MVC filter: deny by default for the Farmer role. A request is refused with 403 when the caller is
	/// signed in with the Farmer role claim and neither the action nor its controller carries
	/// <see cref="FarmerAccessAttribute"/>, whatever else the endpoint says. That includes <c>[AllowAnonymous]</c>
	/// endpoints and endpoints without <c>[Authorize]</c> (residual fix R1): several of them authorize "any
	/// signed-in user" in their own code (IFMS relay, Excel import). A caller with no token is unaffected (still
	/// anonymous there), and so is every staff / dealer caller.
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
		/// The decision on an endpoint's metadata (controller + action attributes): a Farmer token reaches only
		/// endpoints marked <see cref="FarmerAccessAttribute"/>, anonymous ones included (residual fix R1).
		/// Free of HTTP types so the harness can walk every controller by reflection with the same rule.
		/// </summary>
		public static bool IsReachableByFarmer(IEnumerable<object> metadata) =>
			metadata.OfType<FarmerAccessAttribute>().Any();

		/// <summary>
		/// A signed-in caller who is NOT a Farmer: for endpoints that are <c>[AllowAnonymous]</c> but accept
		/// "any signed-in user" in their own code (defence in depth behind the filter, residual fix R1).
		/// </summary>
		public static bool IsSignedInStaff(ClaimsPrincipal? user) =>
			user?.Identity?.IsAuthenticated == true && !IsFarmer(user);

		/// <summary>True when the endpoint needs a signed-in caller (so the Farmer rule applies to it at all).</summary>
		public static bool RequiresSignIn(IEnumerable<object> metadata)
		{
			var list = metadata as IList<object> ?? metadata.ToList();
			return list.OfType<IAuthorizeData>().Any() && !list.OfType<IAllowAnonymous>().Any();
		}
	}
}
