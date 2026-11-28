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
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using SpicAPI.Controllers;
using SpicAPI.Services;
using SPIC.MauiBlazorApp.Shared.Services;
using SPIC.MauiBlazorApp.Shared.Components.FarmerPortal;
using SPIC.Core.DTOs;
using Microsoft.Extensions.Configuration;

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
		Correctives();
		Weather();
		PhotoRoot();
		Infra();
		Caps();
		Refresh();
		Privacy();
		Polish();
	}

	private static string RepoFile(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln")))
			dir = dir.Parent;
		return Path.Combine(new[] { dir?.FullName ?? throw new InvalidOperationException("SpicOne.sln not found") }.Concat(parts).ToArray());
	}

	// ---------------------------------------------------------------- Group B: correctives (V42)

	private static readonly System.Text.RegularExpressions.Regex NutrientKg = new(@"kg (Mn|P|B|S|Zn|N|K)\b");

	private static void Correctives()
	{
		var seed = NutrientCorrectiveSeed.Rows;
		var active = seed.Where(r => r.IsActive).ToList();
		var mn = active.Where(r => r.Nutrient == "Mn" && r.Crop is null).ToList();
		Check.That("V42: general Mn foliar is TNAU's 0.5 % spray, 5 g manganese sulphate per litre",
			mn.Any(r => r.Method == "Foliar" && r.Dose == "0.5 % spray: 5 g manganese sulphate per litre of water"
				&& r.SourceUrl == "https://agritech.tnau.ac.in/agriculture/plant_nutri/gnut_mangan.html"));
		Check.That("V42: general Mn soil is 5-10 kg manganese sulphate per hectare (TNAU groundnut page)",
			mn.Any(r => r.Method == "Soil" && r.Dose.StartsWith("5-10 kg manganese sulphate per hectare") && r.SourceUrl!.Contains("gnut_mangan")));
		var nutrientDoses = active.Where(r => NutrientKg.IsMatch(r.Dose)).Select(r => r.Id).ToList();
		Check.That("V42: no active dose is given in kg of nutrient (kg Mn / P / B / S / Zn per hectare)", nutrientDoses.Count == 0, string.Join("; ", nutrientDoses));
		Check.That("V42: the kg-of-nutrient rows are inactive (22, 31, 32, 61, 62)",
			new[] { 22, 31, 32, 61, 62 }.All(id => !seed.Single(r => r.Id == id).IsActive));
		var noPerLitre = active.Where(r => r.Method == "Foliar" && r.Dose.Contains('%') && !r.Dose.Contains("per litre")).Select(r => r.Id).ToList();
		Check.That("V42: every active percentage spray also gives grams per litre", noPerLitre.Count == 0, string.Join("; ", noPerLitre));
		var paddyMn = LeafCheckRules.SelectCorrectives(seed, "Paddy", new[] { new LeafNutrientGuess("Mn", 0.7) });
		Check.That("V42: paddy manganese now gets the general 0.5 % spray and soil rows",
			paddyMn.Count == 2 && paddyMn.All(c => c.Crop is null) && paddyMn.Any(c => c.Dose.StartsWith("0.5 %")));
		var maizeB = LeafCheckRules.SelectCorrectives(seed, "Maize", new[] { new LeafNutrientGuess("B", 0.7) });
		Check.That("V42: maize boron gets the general borax row", maizeB.Count == 1 && maizeB[0].Dose.StartsWith("10 kg per hectare"));
		Check.That("V42: banana mixed spray gives grams per litre", seed.Where(r => r.Crop == "Banana").All(r => r.Dose.Contains("5 g + 2 g + 2 g + 1 g per litre")));
	}

	// ---------------------------------------------------------------- Group B: weather key (I6)

	private static void Weather()
	{
		var free = FarmerWeatherService.BuildUrl(null, null, 9.2, 77.9);
		Check.That("weather: no key = the free endpoint, no apikey", free.StartsWith(FarmerWeatherService.DefaultUrl + "?") && !free.Contains("apikey"));
		var paid = FarmerWeatherService.BuildUrl(null, " k&y ", 9.2, 77.9);
		Check.That("weather: key = customer-api.open-meteo.com with an escaped apikey",
			paid.StartsWith("https://customer-api.open-meteo.com/v1/forecast?") && paid.EndsWith("&apikey=k%26y"));
		Check.That("weather: a configured WeatherUrl wins", FarmerWeatherService.BuildUrl("https://example.test/f", "k", 9.2, 77.9).StartsWith("https://example.test/f?latitude=9.2"));
		Check.That("weather: the point stays rounded to 0.1 deg", paid.Contains("latitude=9.2&longitude=77.9"));
	}

	// ---------------------------------------------------------------- Group B: leaf photo root guard (#49)

	private static void PhotoRoot()
	{
		var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "spic-app"));
		var web = Path.Combine(root, "wwwroot");
		var def = Path.Combine(root, "PrivateUploads");
		Check.Equal("photo root: not configured = PrivateUploads", def, FarmerLeafCheckController.SafePrivateRoot(root, web, null, out var r0));
		Check.That("photo root: not configured is not refused", !r0);
		var outside = Path.Combine(Path.GetTempPath(), "spic-private");
		Check.Equal("photo root: an outside folder is kept", Path.GetFullPath(outside), FarmerLeafCheckController.SafePrivateRoot(root, web, outside, out var r1));
		Check.That("photo root: an outside folder is not refused", !r1);
		Check.Equal("photo root: under Uploads falls back to the default", def,
			FarmerLeafCheckController.SafePrivateRoot(root, web, Path.Combine(root, "Uploads", "Leaf"), out var r2));
		Check.That("photo root: under Uploads is refused", r2);
		FarmerLeafCheckController.SafePrivateRoot(root, web, Path.Combine(root, "wwwroot"), out var r3);
		Check.That("photo root: wwwroot itself is refused", r3);
		FarmerLeafCheckController.SafePrivateRoot(root, web, "Uploads/../Uploads/x", out var r4);
		Check.That("photo root: a relative path into Uploads is refused", r4);
		FarmerLeafCheckController.SafePrivateRoot(root, web, Path.Combine(root, "UploadsPrivate"), out var r5);
		Check.That("photo root: a sibling named UploadsPrivate is allowed", !r5);
	}

	// ---------------------------------------------------------------- Group B: bicep wiring (I2) and docker (#48)

	private static void Infra()
	{
		var bicep = File.ReadAllText(RepoFile("infra", "azure", "main.bicep")).Replace("\r\n", "\n");
		foreach (var param in new[] { "farmerTrustedClientKey", "farmerOtpPepper", "msg91AuthKey", "anthropicApiKey", "farmerWeatherApiKey" })
			Check.That($"bicep: @secure() {param} with an empty default", bicep.Contains($"@secure()\nparam {param} string = ''"));
		foreach (var param in new[] { "smsProvider", "msg91TemplateId", "msg91SenderId" })
			Check.That($"bicep: {param} with an empty default", bicep.Contains($"param {param} string = ''"));
		foreach (var env in new[] { "FarmerPortal__TrustedClientKey", "FarmerPortal__OtpPepper", "Sms__Provider", "Sms__Msg91__AuthKey",
			"Sms__Msg91__TemplateId", "Sms__Msg91__SenderId", "Assistant__AnthropicApiKey", "FarmerPortal__WeatherApiKey" })
			Check.That($"bicep: {env} is added only when its value is set",
				System.Text.RegularExpressions.Regex.IsMatch(bicep, @"empty\(\w+\) \? \[\] : \[\{ name: '" + env + "'"));
		Check.That("bicep: the trusted key is on the web app too", bicep.Contains("], webEnvFarmer)") && bicep.Contains("], webSecretsFarmer)"));
		Check.That("bicep: the API env and secrets take the farmer arrays",
			bicep.Contains("concat(apiEnvBase, apiEnvIfms, apiEnvHelp, apiEnvFarmer)") && bicep.Contains("concat(apiSecretsBase, apiSecretsIfms, apiSecretsHelp, apiSecretsFarmer)"));
		var common = File.ReadAllText(RepoFile("deploy", "azure", "common.ps1"));
		Check.That("deploy: the farmer secrets are read back from Key Vault on every deploy",
			new[] { "farmer-trusted-client-key", "farmer-otp-pepper", "msg91-auth-key", "assistant-anthropic-key", "farmer-weather-key" }.All(common.Contains));
		var docker = File.ReadAllText(RepoFile(".dockerignore"));
		Check.That("docker: SpicAPI/PrivateUploads/ is not copied into the image", docker.Split('\n').Any(l => l.Trim() == "SpicAPI/PrivateUploads/"));
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
		// Residual fix R1: a Farmer token is refused on anonymous endpoints too, unless they carry [FarmerAccess].
		Check.That("filter (R1): farmer on an [AllowAnonymous] endpoint without the marker gets 403", Is403(RunFilter(Principal("Farmer"), authorize, new AllowAnonymousAttribute())));
		Check.That("filter (R1): farmer on an endpoint without [Authorize] gets 403", Is403(RunFilter(Principal("Farmer"))));
		Check.That("filter (R1): farmer on an [AllowAnonymous] + [FarmerAccess] endpoint passes", RunFilter(Principal("Farmer"), new AllowAnonymousAttribute(), new FarmerAccessAttribute()) is null);
		Check.That("filter (R1): no token on an [AllowAnonymous] endpoint is untouched", RunFilter(Principal(null), new AllowAnonymousAttribute()) is null);
		Check.That("filter (R1): staff on an [AllowAnonymous] endpoint is untouched", RunFilter(Principal("MDO"), new AllowAnonymousAttribute()) is null);
		Check.That("filter (R1): IsSignedInStaff is false for a farmer and for no token, true for staff",
			!FarmerDenyByDefaultFilter.IsSignedInStaff(Principal("Farmer")) && !FarmerDenyByDefaultFilter.IsSignedInStaff(Principal(null)) && FarmerDenyByDefaultFilter.IsSignedInStaff(Principal("Admin")));
		Check.That("filter: role claim compared case-insensitively", Is403(RunFilter(Principal("farmer"), authorize)));
		Check.That("filter: short 'role' claim type is also a Farmer", Is403(RunFilter(Principal("Farmer", "role"), authorize)));
		foreach (var role in Enum.GetValues<AppRole>().Where(r => r != AppRole.Farmer))
			Check.That($"filter: {role} on a bare [Authorize] endpoint is untouched", RunFilter(Principal(role.ToString()), authorize) is null);
		Check.That("filter: an anonymous caller is untouched (the [Authorize] middleware answers 401)", RunFilter(Principal(null), authorize) is null);
		Check.That("filter (R1): IsReachableByFarmer(no metadata) is false", !FarmerDenyByDefaultFilter.IsReachableByFarmer(Array.Empty<object>()));
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
		"DELETE api/Farmer/profile",
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

	/// <summary>
	/// Anonymous endpoints a Farmer token may reach (residual fix R1): those a signed-in farmer's app calls with its
	/// token attached. Every other [AllowAnonymous] / unauthenticated endpoint answers 403 to a Farmer token.
	/// </summary>
	internal static readonly string[] ExpectedFarmerReachableAnonymous =
	{
		"POST api/Authentication/refresh",   // AuthHttpMessageHandler / SessionExpiryDialog, expired token attached
		"POST api/FarmerAuth/otp/request",   // farmer sign-in (FarmerApi)
		"POST api/FarmerAuth/otp/verify",
		"POST api/Telemetry/batch",          // ClientTelemetry page views / errors (rate limited for farmers, R3)
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

		// R1: the anonymous set, pinned both ways.
		var anonReachable = actions
			.Where(a => !FarmerDenyByDefaultFilter.RequiresSignIn(a.Metadata) && FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata))
			.Select(a => a.Route).Distinct().OrderBy(r => r, StringComparer.Ordinal).ToList();
		var anonExpected = ExpectedFarmerReachableAnonymous.OrderBy(r => r, StringComparer.Ordinal).ToList();
		Check.That("allow-list (R1): no anonymous endpoint is open to a Farmer token beyond the pinned list",
			!anonReachable.Except(anonExpected).Any(), string.Join("; ", anonReachable.Except(anonExpected)));
		Check.That("allow-list (R1): every pinned anonymous endpoint is open to a Farmer token",
			!anonExpected.Except(anonReachable).Any(), string.Join("; ", anonExpected.Except(anonReachable)));
		var ifms = actions.Where(a => a.Controller == typeof(IfmsAutomationController)).ToList();
		Check.That("allow-list (R1): every IfmsAutomation action exists and is closed to a Farmer token",
			ifms.Count > 5 && ifms.All(a => !FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata)));
		foreach (var closed in new[] { "POST api/ExcelBulkUpload/import", "POST api/Authentication/login", "POST api/ContactUs/enquiry",
			"GET api/Lab/batches/{id:int}/samples/export", "GET api/Lab/batches/{id:int}/parameters/export", "GET api/User/seed-default-user" })
		{
			var found = actions.Where(a => a.Route == closed).ToList();
			Check.That($"allow-list (R1): anonymous '{closed}' exists and is closed to a Farmer token",
				found.Count > 0 && found.All(a => !FarmerDenyByDefaultFilter.RequiresSignIn(a.Metadata) && !FarmerDenyByDefaultFilter.IsReachableByFarmer(a.Metadata)),
				found.Count == 0 ? "route not found" : null);
		}

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

	// ---------------------------------------------------------------- Group C: caps (I3, M2, M3), refresh (M4), privacy (I4)

	private static void Caps()
	{
		Check.Equal("caps: fertilizer default 50 per IST day", 50, FarmerDailyCapRules.Limit(null, FarmerDailyCapRules.FertilizerRunsDefault));
		Check.Equal("caps: yield default 50 per IST day", 50, FarmerDailyCapRules.Limit("", FarmerDailyCapRules.YieldForecastsDefault));
		Check.Equal("caps: config value is used", 7, FarmerDailyCapRules.Limit(" 7 ", 50));
		Check.Equal("caps: zero / negative / text fall back", 50, FarmerDailyCapRules.Limit("0", 50) + FarmerDailyCapRules.Limit("-3", 50) + FarmerDailyCapRules.Limit("lots", 50) - 100);
		Check.Equal("caps: 10 call-backs a day across officers", 10, FarmerDailyCapRules.CallBacksPerDay);
		Check.That("caps: community 5 posts / 30 replies / 20 MB", FarmerDailyCapRules.CommunityPostsDefault == 5 && FarmerDailyCapRules.CommunityRepliesDefault == 30
			&& FarmerDailyCapRules.MegaBytes(FarmerDailyCapRules.CommunityAttachmentMbDefault) == 20L * 1024 * 1024);
		Check.That("caps: the 50th is allowed, the 51st is not", FarmerDailyCapRules.Allows(49, 50) && !FarmerDailyCapRules.Allows(50, 50));
		Check.That("caps: bytes up to the limit pass, one more does not",
			FarmerDailyCapRules.AllowsBytes(19L * 1024 * 1024, 1024 * 1024, 20L * 1024 * 1024) && !FarmerDailyCapRules.AllowsBytes(19L * 1024 * 1024, 1024 * 1024 + 1, 20L * 1024 * 1024));
		Check.That("caps: farmers attach photos and PDFs, not DOC / DOCX",
			FarmerDailyCapRules.FarmerAttachmentExtensions.SetEquals(new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" }) && !FarmerDailyCapRules.FarmerAttachmentExtensions.Contains(".docx"));
		// 20:00 UTC on 28 Nov = 01:30 IST on 29 Nov: the IST day began at 18:30 UTC on 28 Nov.
		var utcNow = new DateTime(2026, 11, 28, 20, 0, 0, DateTimeKind.Utc);
		Check.Equal("caps: IST day start on a UTC server clock", new DateTime(2026, 11, 28, 18, 30, 0), FarmerDailyCapRules.IstDayStartServerLocal(utcNow, TimeZoneInfo.Utc));
		var ist = TimeZoneInfo.CreateCustomTimeZone("IST-test", TimeSpan.FromHours(5.5), "IST", "IST");
		Check.Equal("caps: IST day start on an IST server clock is midnight", new DateTime(2026, 11, 29, 0, 0, 0), FarmerDailyCapRules.IstDayStartServerLocal(utcNow, ist));
		foreach (var m in new[] { FarmerDailyCapRules.FertilizerMessage(50), FarmerDailyCapRules.YieldMessage(50), FarmerDailyCapRules.CallBackMessage(10),
			FarmerDailyCapRules.CommunityPostMessage(5), FarmerDailyCapRules.CommunityReplyMessage(30), FarmerDailyCapRules.CommunityAttachmentMessage(20) })
			Check.That($"caps: plain message names the limit and midnight: '{m}'", m.Contains("midnight") && System.Text.RegularExpressions.Regex.IsMatch(m, @"\d"));
	}

	private static void Refresh()
	{
		IConfiguration Config(params (string Key, string Value)[] pairs) =>
			new Microsoft.Extensions.Configuration.ConfigurationBuilder()
				.AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value))).Build();
		Check.Equal("refresh: Farmer default 30 days", 30d, TokenIssuer.RefreshDays(Config(), AppRole.Farmer));
		Check.Equal("refresh: FarmerPortal:RefreshExpiryDays is used for farmers", 45d, TokenIssuer.RefreshDays(Config(("FarmerPortal:RefreshExpiryDays", "45")), AppRole.Farmer));
		Check.Equal("refresh: a bad farmer value falls back to 30", 30d, TokenIssuer.RefreshDays(Config(("FarmerPortal:RefreshExpiryDays", "0")), AppRole.Farmer));
		Check.Equal("refresh: Jwt:RefreshExpiryDays does not change farmers", 30d, TokenIssuer.RefreshDays(Config(("Jwt:RefreshExpiryDays", "5")), AppRole.Farmer));
		Check.Equal("refresh: staff default unchanged (2 days)", 2d, TokenIssuer.RefreshDays(Config(("FarmerPortal:RefreshExpiryDays", "45")), AppRole.MDO));
		Check.Equal("refresh: staff keep Jwt:RefreshExpiryDays", 5d, TokenIssuer.RefreshDays(Config(("Jwt:RefreshExpiryDays", "5")), AppRole.Admin));
		Check.Equal("refresh: unknown role = staff rule", 2d, TokenIssuer.RefreshDays(Config(), null));
	}

	private static void Privacy()
	{
		Check.That("privacy: no recorded version needs acceptance", FarmerPrivacyRules.NeedsAcceptance(null));
		Check.That("privacy: an older version needs acceptance", FarmerPrivacyRules.NeedsAcceptance("2026-01-01"));
		Check.That("privacy: the current version does not", !FarmerPrivacyRules.NeedsAcceptance(FarmerPrivacyRules.Version));
		Check.That("privacy: the version fits FarmerProfiles.PrivacyVersion (20)", FarmerPrivacyRules.Version.Length <= 20);
		var removed = string.Join(" ", FarmerPrivacyRules.RemovedItems).ToLowerInvariant();
		foreach (var word in new[] { "phone", "gps", "crops", "fertilizer", "harvest", "leaf photos", "spic ai", "sign-in", "call-back", "community" })
			Check.That($"privacy: the deletion list names '{word}'", removed.Contains(word));
		Check.That("privacy: the deletion sheet says it cannot be undone", FarmerPrivacyRules.CannotUndo.StartsWith("This cannot be undone"));
		// Residual fix R2: usage counters and sign-in codes are kept (no name or number), so a delete does not reset limits.
		Check.That("R2: the tombstone is not a user id (never matches a per-farmer count)", !Guid.TryParse(FarmerPrivacyRules.DeletedUserTombstone, out _) && FarmerPrivacyRules.DeletedUserTombstone.Length <= 450);
		Check.That("R2: the deletion list no longer promises to remove sign-in codes", !FarmerPrivacyRules.RemovedItems.Any(i => i.Contains("sign-in codes", StringComparison.OrdinalIgnoreCase)));
		Check.That("R2: the kept list names the anonymous AI counts and the one-day code purge",
			FarmerPrivacyRules.KeptItems.Any(i => i.Contains("no name or number")) && FarmerPrivacyRules.KeptItems.Any(i => i.Contains("one day")));
		// Residual fix R3: Farmer telemetry gets the anonymous batch rate, per farmer; staff do not.
		Check.Equal("R3: a farmer's telemetry rate key is per farmer", "farmer:u1", Spic.Infrastructure.Services.Telemetry.TelemetryIngestService.LimitedUserKey(Principal("Farmer")));
		Check.That("R3: staff and anonymous callers have no farmer rate key",
			Spic.Infrastructure.Services.Telemetry.TelemetryIngestService.LimitedUserKey(Principal("MDO")) is null
			&& Spic.Infrastructure.Services.Telemetry.TelemetryIngestService.LimitedUserKey(Principal(null)) is null);
		// The privacy notice says phone numbers never reach the request / error logs: a farmer's user name is "f" + mobile.
		Check.Equal("logs: a farmer user name is masked", "f98xxxxxx21", Spic.Infrastructure.Services.Telemetry.TelemetryHttp.SafeUserName("f9876543221", "Farmer"));
		Check.Equal("logs: staff user names are unchanged", "qa.mdo", Spic.Infrastructure.Services.Telemetry.TelemetryHttp.SafeUserName("qa.mdo", "MDO"));
		Check.Equal("logs: a staff user name that looks like a farmer's is unchanged", "f9876543221", Spic.Infrastructure.Services.Telemetry.TelemetryHttp.SafeUserName("f9876543221", "Dealer"));
		Check.Equal("logs: other farmer user names are unchanged", "qa.farmer", Spic.Infrastructure.Services.Telemetry.TelemetryHttp.SafeUserName("qa.farmer", "Farmer"));

		var anon = new LoginState();
		var farmer = TestLogin.As(AppRole.Farmer, "");
		var mdo = TestLogin.As(AppRole.MDO, "Dashboard.View");
		foreach (var url in new[] { FarmerPrivacyRules.PrivacyPath, FarmerPrivacyRules.DeleteInfoPath })
		{
			Check.That($"route: anyone signed out opens {url}", RouteAccess.CanOpen(anon, url, null));
			Check.That($"route: a farmer opens {url}", RouteAccess.CanOpen(farmer, url, null));
			Check.That($"route: staff (MDO) open {url} too (public page)", RouteAccess.CanOpen(mdo, url, null));
		}
		Check.That("route: staff are still sent back from /farmer", !RouteAccess.CanOpen(mdo, "/farmer", null));
		Check.That("route: staff are still sent back from /farmer/profile", !RouteAccess.CanOpen(mdo, "/farmer/profile", null));
	}

	// ---------------------------------------------------------------- Group D: polish

	private static void Polish()
	{
		// #27: an omitted N / P / K / pH is a 400, never a silent 0.
		var norm = new FertilizerCropNorm { Id = 1, Crop = "Paddy", Variant = "x", IsActive = true };
		var omitted = System.Text.Json.JsonSerializer.Deserialize<FertilizerRecommendRequest>(
			"{\"normId\":1,\"soilType\":\"Red Soil\",\"areaAcres\":1}", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
		var errs = FertilizerAdvisor.Validate(omitted, norm);
		Check.That("#27: omitted n, p, k and ph are each an error", new[] { "n", "p", "k", "ph" }.All(errs.ContainsKey), string.Join(",", errs.Keys));
		var sent = System.Text.Json.JsonSerializer.Deserialize<FertilizerRecommendRequest>(
			"{\"normId\":1,\"soilType\":\"Red Soil\",\"areaAcres\":1,\"n\":0,\"p\":0,\"k\":0,\"ph\":7}", new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))!;
		Check.That("#27: a real 0 is still accepted", !FertilizerAdvisor.Validate(sent, norm).Keys.Any(k => k is "n" or "p" or "k" or "ph"));
		var wire = System.Text.Json.JsonSerializer.Serialize(new FertilizerRecommendRequest { N = 120, P = 10, K = 150, Ph = 6.8m }, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
		Check.That("#27: the client still sends n / p / k / ph", wire.Contains("\"n\":120") && wire.Contains("\"ph\":6.8") && !wire.Contains("NInput", StringComparison.OrdinalIgnoreCase), wire);

		// #29: the slider bubble never rounds across a zone edge.
		FpLevel Zone(double v) => FpUi.ZoneLevel(v, 11, 22);
		Check.Equal("#29: 10.99 next to 'Low below 11' shows 10.99", "10.99", FpUi.ZoneNumber(10.99, 1, Zone));
		Check.Equal("#29: 12.34 keeps one decimal", "12.3", FpUi.ZoneNumber(12.34, 1, Zone));
		Check.Equal("#29: 22.04 (Medium up to 22) shows two decimals", "22.04", FpUi.ZoneNumber(22.04, 1, Zone));

		// #35: the kill switch fails safe on a typo.
		Check.That("#35: empty / true / 1 / on / yes = on", new[] { null, "", "true", "TRUE", "1", "on", " yes " }.All(FarmerAiBudgetRules.Enabled));
		Check.That("#35: false / 0 / off / no = off", new[] { "false", "0", "off", "no" }.All(v => !FarmerAiBudgetRules.Enabled(v)));
		Check.That("#35: a typo is OFF and flagged unreadable", !FarmerAiBudgetRules.Enabled("flase") && FarmerAiBudgetRules.IsUnreadable("flase") && !FarmerAiBudgetRules.IsUnreadable("false"));

		// #50: busy is a server condition (503), not a photo error.
		Check.That("#50: the busy message is a constant the controller recognises", LeafImageProcessor.BusyMessage.Contains("Many farmers"));
	}
}
