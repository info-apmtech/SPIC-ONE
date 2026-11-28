using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal final fix wave (docs/farmer-portal.md "Hardening (final fix wave)").
/// Group A: the Farmer deny-by-default filter, the pinned [FarmerAccess] allow-list (walked by
/// reflection over every SpicAPI controller) and the UserController role restriction.
/// </summary>
internal static class FarmerPortalHardeningTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal hardening: Farmer allow-list, account admin roles");
		Filter();
		AllowList();
		UserAdmin();
	}

	// ---------------------------------------------------------------- Group A: filter

	private static ClaimsPrincipal Principal(string? role, string claimType = ClaimTypes.Role)
	{
		if (role is null) return new ClaimsPrincipal(new ClaimsIdentity());
		var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "u1"), new(claimType, role) };
		return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
	}

	private static IActionResult? RunFilter(ClaimsPrincipal user, params object[] metadata)
	{
		var http = new DefaultHttpContext { User = user };
		http.Request.Method = "GET";
		var descriptor = new ActionDescriptor
		{
			EndpointMetadata = metadata.ToList(),
			AttributeRouteInfo = new AttributeRouteInfo { Template = "api/Test" }
		};
		var context = new AuthorizationFilterContext(new ActionContext(http, new RouteData(), descriptor), new List<IFilterMetadata>());
		new FarmerDenyByDefaultFilter(NullLogger<FarmerDenyByDefaultFilter>.Instance).OnAuthorization(context);
		return context.Result;
	}

	private static bool Is403(IActionResult? r) => r is ObjectResult { StatusCode: 403 };

	private static void Filter()
	{
		var authorize = new AuthorizeAttribute();
		Check.That("filter: farmer on a bare [Authorize] endpoint gets 403", Is403(RunFilter(Principal("Farmer"), authorize)));
		Check.That("filter: farmer on [Authorize(Roles=Admin)] without marker gets 403", Is403(RunFilter(Principal("Farmer"), new AuthorizeAttribute { Roles = "Admin" })));
		Check.That("filter: 403 body is the plain farmer message",
			RunFilter(Principal("Farmer"), authorize) is ObjectResult o && (o.Value?.ToString() ?? "").Contains(FarmerDenyByDefaultFilter.DeniedMessage));
		Check.That("filter: farmer on a [FarmerAccess] endpoint passes", RunFilter(Principal("Farmer"), authorize, new FarmerAccessAttribute()) is null);
		Check.That("filter: farmer on an [AllowAnonymous] endpoint passes", RunFilter(Principal("Farmer"), authorize, new AllowAnonymousAttribute()) is null);
		Check.That("filter: farmer on an endpoint without [Authorize] passes (anonymous)", RunFilter(Principal("Farmer")) is null);
		Check.That("filter: role claim compared case-insensitively", Is403(RunFilter(Principal("farmer"), authorize)));
		Check.That("filter: short 'role' claim type is also a Farmer", Is403(RunFilter(Principal("Farmer", "role"), authorize)));
		foreach (var role in Enum.GetValues<AppRole>().Where(r => r != AppRole.Farmer))
			Check.That($"filter: {role} on a bare [Authorize] endpoint is untouched", RunFilter(Principal(role.ToString()), authorize) is null);
		Check.That("filter: an anonymous caller is untouched (the [Authorize] middleware answers 401)", RunFilter(Principal(null), authorize) is null);
		Check.That("filter: IsReachableByFarmer(no metadata) is true (anonymous)", FarmerDenyByDefaultFilter.IsReachableByFarmer(Array.Empty<object>()));
		Check.That("filter: RequiresSignIn is false under [AllowAnonymous]",
			!FarmerDenyByDefaultFilter.RequiresSignIn(new object[] { authorize, new AllowAnonymousAttribute() }));
	}

	// ---------------------------------------------------------------- Group A: allow-list

	/// <summary>One action route: "VERB template", e.g. "GET api/Farmer/profile".</summary>
	internal sealed record ApiAction(string Route, Type Controller, MethodInfo Method, object[] Metadata);

	/// <summary>Every attribute-routed action of every SpicAPI controller, with the metadata MVC would see.</summary>
	internal static List<ApiAction> AllActions()
	{
		var list = new List<ApiAction>();
		var controllers = typeof(FarmerAccessAttribute).Assembly.GetTypes()
			.Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t)
				&& t.GetCustomAttribute<NonControllerAttribute>(true) is null);
		foreach (var type in controllers)
		{
			var classAttrs = type.GetCustomAttributes(true);
			var prefix = (type.GetCustomAttributes<RouteAttribute>(true).FirstOrDefault()?.Template ?? "")
				.Replace("[controller]", type.Name.EndsWith("Controller") ? type.Name[..^"Controller".Length] : type.Name);
			foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				if (m.GetCustomAttribute<NonActionAttribute>(true) is not null) continue;
				var methodAttrs = m.GetCustomAttributes(true);
				foreach (var http in methodAttrs.OfType<HttpMethodAttribute>())
				{
					var t = http.Template ?? "";
					var route = t.StartsWith("/") || t.StartsWith("~/") ? t.TrimStart('~', '/')
						: string.IsNullOrEmpty(t) ? prefix : $"{prefix}/{t}";
					foreach (var verb in http.HttpMethods)
						list.Add(new ApiAction($"{verb} {route}", type, m, classAttrs.Concat(methodAttrs).ToArray()));
				}
			}
		}
		return list;
	}

	/// <summary>
	/// Every signed-in endpoint a Farmer JWT may call. Built from what the farmer UI calls (farmer pages,
	/// Fp components, farmer services, the shell, RouteAccess farmer routes and the local request log).
	/// Changing a [FarmerAccess] marker means changing this list on purpose.
	/// </summary>
	internal static readonly string[] ExpectedFarmerReachable =
	{
		// Shell: logout and the once-per-load permission re-read (refresh is anonymous).
		"GET api/Authentication/permissions",
		"POST api/Authentication/logout",
		// Knowledge Community (RouteAccess opens /Community/* to farmers; "Community near me").
		"DELETE api/Community/attachments/{id:int}",
		"DELETE api/Community/discussions/{id:int}",
		"DELETE api/Community/me/avatar",
		"DELETE api/Community/replies/{id:int}",
		"GET api/Community/discussions",
		"GET api/Community/discussions/{id:int}",
		"GET api/Community/file/{*path}",
		"GET api/Community/lookups",
		"GET api/Community/me",
		"GET api/Community/near",
		"GET api/Community/products",
		"GET api/Community/recent",
		"GET api/Community/stats",
		"PATCH api/Community/discussions/{id:int}/status",
		"POST api/Community/discussions",
		"POST api/Community/discussions/similar",
		"POST api/Community/discussions/{id:int}/attachments",
		"POST api/Community/discussions/{id:int}/follow",
		"POST api/Community/discussions/{id:int}/like",
		"POST api/Community/discussions/{id:int}/replies",
		"POST api/Community/discussions/{id:int}/save",
		"POST api/Community/me/avatar",
		"POST api/Community/products/{name}/join",
		"POST api/Community/replies/{id:int}/attachments",
		"POST api/Community/replies/{id:int}/like",
		"PUT api/Community/discussions/{id:int}",
		// Farmer profile dropdowns and the pincode fill.
		"GET api/District/byState/{stateId}",
		"GET api/PincodeLookup/{pincode}",
		"GET api/Region/byState/{stateId}",
		"GET api/State",
		// Farmer portal controllers (all Farmer role, scoped to the caller).
		"DELETE api/Farmer/assistant/conversations/{id:long}",
		"GET api/Farmer/assistant/conversations",
		"GET api/Farmer/assistant/conversations/{id:long}",
		"GET api/Farmer/assistant/status",
		"POST api/Farmer/assistant/ask",
		"GET api/Farmer/contacts",
		"GET api/Farmer/contacts/requests",
		"GET api/Farmer/dealers",
		"POST api/Farmer/contacts/{contactId}/request",
		"GET api/Farmer/fertilizer/history",
		"GET api/Farmer/fertilizer/history/{id:long}",
		"GET api/Farmer/fertilizer/latest",
		"GET api/Farmer/fertilizer/options",
		"GET api/Farmer/fertilizer/soil-report",
		"POST api/Farmer/fertilizer/recommend",
		"DELETE api/Farmer/leaf-check/{id:long}",
		"GET api/Farmer/leaf-check/history",
		"GET api/Farmer/leaf-check/status",
		"GET api/Farmer/leaf-check/{id:long}",
		"GET api/Farmer/leaf-check/{id:long}/image",
		"POST api/Farmer/leaf-check",
		"GET api/Farmer/profile",
		"PUT api/Farmer/profile",
		"GET api/Farmer/trends",
		"GET api/Farmer/weather",
		"GET api/Farmer/yield/history",
		"GET api/Farmer/yield/history/{id:long}",
		"GET api/Farmer/yield/options",
		"POST api/Farmer/yield/predict",
		// SAS lab reports (My Samples / My Reports; the API scopes a Farmer to their own reports).
		"GET api/Lab/languages",
		"GET api/Lab/reports",
		"GET api/Lab/reports/{id:int}",
		"GET api/Lab/reports/{id:int}/pdf",
		// Digital Library item pages linked from SPIC AI answers (published content only).
		"GET api/Library/file/{*path}",
		"GET api/Library/{id:int}",
		// SAS My Samples and the v1 details page (farmer-scoped reads).
		"GET api/Sas/collections",
		"GET api/Sas/collections/{id:int}",
		"GET api/Sas/consignments/{id:int}",
		"GET api/Sas/lookups",
		"GET api/Sas/samples/{itemId:int}/results",
		// SAS Payments (farmer read-only mode, scoped by the API).
		"GET api/Sas/payments",
		"GET api/Sas/payments/me",
		"GET api/Sas/payments/stats",
		"GET api/Sas/payments/{id:int}",
		"GET api/Sas/payments/{id:int}/proof",
	};

	private static void AllowList()
	{
		var actions = AllActions();
		Check.That("allow-list: reflection found the SpicAPI controllers", actions.Count > 500, $"{actions.Count} actions");

		var reachable = actions
			.Where(a => FarmerDenyByDefaultFilter.RequiresSignIn(a.Metadata) && FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata))
			.Select(a => a.Route).Distinct().OrderBy(r => r, StringComparer.Ordinal).ToList();
		var expected = ExpectedFarmerReachable.Distinct().OrderBy(r => r, StringComparer.Ordinal).ToList();

		var extra = reachable.Except(expected).ToList();
		var missing = expected.Except(reachable).ToList();
		Check.That("allow-list: no signed-in endpoint is open to a Farmer beyond the pinned list", extra.Count == 0, string.Join("; ", extra));
		Check.That("allow-list: every pinned endpoint is still open to a Farmer", missing.Count == 0, string.Join("; ", missing));

		foreach (var staffOnly in new[]
		{
			"POST api/User/create-user", "PUT api/User/update-user", "DELETE api/User/delete-user/{userId}",
			"PATCH api/User/update-status/{userId}", "GET api/EmployeeRegistration", "GET api/DealerFile/view/{*filePath}",
			"GET api/CSR2/by-csr1/{csr1Id:int}", "POST api/Library/assistant/ask", "GET api/FarmerRequests",
			"POST api/Community/products/{name}/image", "GET api/Help/audio/{module}/{sectionId}/{lang}",
			"GET api/Sas/file/{*path}", "GET api/Lab/reports/{id:int}/xlsx", "POST api/Sas/collections"
		})
		{
			var found = actions.Where(a => a.Route == staffOnly).ToList();
			Check.That($"allow-list: '{staffOnly}' exists and is closed to a Farmer",
				found.Count > 0 && found.All(a => FarmerDenyByDefaultFilter.RequiresSignIn(a.Metadata) && !FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata)),
				found.Count == 0 ? "route not found" : null);
		}

		// The farmer OTP sign-in stays anonymous (it is how a Farmer gets a token at all).
		Check.That("allow-list: api/FarmerAuth/* is anonymous",
			actions.Where(a => a.Controller == typeof(FarmerAuthController)).All(a => !FarmerDenyByDefaultFilter.RequiresSignIn(a.Metadata)));
		Check.That("allow-list: refresh stays anonymous",
			actions.Any(a => a.Route == "POST api/Authentication/refresh" && !FarmerDenyByDefaultFilter.RequiresSignIn(a.Metadata)));
		Check.That("allow-list: LibraryAssistant (unbudgeted AI) is closed to a Farmer",
			actions.Where(a => a.Controller == typeof(LibraryAssistantController)).All(a => !FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata)));
		Check.That("allow-list: the staff FarmerRequests controller is closed to a Farmer",
			actions.Where(a => a.Controller == typeof(FarmerRequestsController)).All(a => !FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata)));
		Check.That("allow-list: the State override keeps exactly one GET api/State route",
			actions.Count(a => a.Route == "GET api/State") == 1);
	}

	// ---------------------------------------------------------------- Group A: UserController

	private static void UserAdmin()
	{
		foreach (var name in new[] { nameof(UserController.CreateUser), nameof(UserController.UpdateUser), nameof(UserController.DeleteUser), nameof(UserController.UpdateStatus) })
		{
			var roles = typeof(UserController).GetMethod(name)!.GetCustomAttributes<AuthorizeAttribute>(true).Select(a => a.Roles).FirstOrDefault();
			Check.Equal($"user admin: {name} is limited to the account admin roles", UserController.AccountAdminRoles, roles);
		}
		Check.Equal("user admin: account admin roles are Admin, SuperAdmin, CorporateAdmin", "Admin,SuperAdmin,CorporateAdmin", UserController.AccountAdminRoles);
		Check.That("user admin: Admin may create an MDO", UserController.MayAssignRole(AppRole.Admin, AppRole.MDO));
		Check.That("user admin: Admin may create an Admin (unchanged)", UserController.MayAssignRole(AppRole.Admin, AppRole.Admin));
		Check.That("user admin: Admin may NOT create a SuperAdmin", !UserController.MayAssignRole(AppRole.Admin, AppRole.SuperAdmin));
		Check.That("user admin: CorporateAdmin may NOT create a Farmer", !UserController.MayAssignRole(AppRole.CorporateAdmin, AppRole.Farmer));
		Check.That("user admin: SuperAdmin may create a SuperAdmin", UserController.MayAssignRole(AppRole.SuperAdmin, AppRole.SuperAdmin));
		Check.That("user admin: an unknown caller role may NOT create a SuperAdmin", !UserController.MayAssignRole(null, AppRole.SuperAdmin));
		Check.That("user admin: seed-default-user is unchanged (anonymous, owner item)",
			typeof(UserController).GetMethod(nameof(UserController.SeedDefaultUser))!.GetCustomAttributes<AllowAnonymousAttribute>(true).Any());
	}
}
