using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using SPIC.Core.FarmerPortal;
using Spic.Infrastructure.Data;
using SpicAPI.Controllers;
using SpicAPI.Services;
using SPIC.MauiBlazorApp.Shared.Services;

namespace SPIC.RegressionHarness;

/// <summary>
/// Farmer portal recheck fixes (docs/farmer-portal.md "Recheck fixes", 2026-11-28).
/// Group 1: a farmer sees only their own samples of a shared SAS collection (B1), the shared file routes serve a
/// Farmer token only the Community / Library subtrees (B2), and account deletion removes the Community attachments,
/// page-view identity and kept call-back messages (B7, text checks; the end-to-end run is in smoke_final.py).
/// </summary>
internal static class FarmerPortalRecheckTests
{
	public static void Run()
	{
		Console.WriteLine("Farmer portal recheck: SAS sample scope, file subtrees, deletion text");
		SasSampleScope();
		FileSubtrees();
		DeletionText();
		PhoneSpelling();
		FarmerSessionRules();
		FarmerRestore();
		ShellSources();
		MinorSources();
		WeatherData();
		WeatherCap();
		CropGroups();
	}

	// ------------------------------------------------------------------ Group 3 UI (source)

	private static void MinorSources()
	{
		string Page(string name) => Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "FarmerPortal", name);
		foreach (var page in new[] { "FarmerLeafCheck.razor", "FarmerFertilizer.razor", "FarmerYield.razor" })
		{
			var src = Page(page);
			var body = src[src.IndexOf("private async Task SubmitAsync()", StringComparison.Ordinal)..];
			var firstStatement = body.Split('\n').Skip(2).First(l => l.Trim().Length > 0 && !l.Trim().StartsWith("//")).Trim();
			Check.Equal($"U4: {page} SubmitAsync ignores a second tap first", "if (_submitting) return;", firstStatement);
		}

		var assistant = Page("FarmerAssistant.razor");
		Check.That("U7: the assistant marks itself disposed before cancelling", assistant.Contains("public async ValueTask DisposeAsync()\n    {\n        _disposed = true;\n        _cts.Cancel();"));
		var delete = assistant[assistant.IndexOf("private async Task DeleteAsync(", StringComparison.Ordinal)..];
		delete = delete[..delete.IndexOf("protected override", StringComparison.Ordinal)];
		Check.That("U7: delete-chat checks the page is still there after the confirm and never reads _cts.Token",
			delete.Contains("if (_disposed) return;") && delete.Contains("Api.DeleteAsync(c.Id, LoadToken)") && !delete.Contains("_cts.Token"));

		Check.That("U8: the phone box takes a pasted '+91 98765 43210' / '0091 98765 43210'", Page("FarmerLogin.razor").Contains("maxlength=\"20\""));
		Check.That("U8: both pasted forms fit 20 characters and normalise to a valid number",
			new[] { "+91 98765 43210", "+91-98765-43210", "0091 98765 43210" }.All(p => p.Length <= 20 && FarmerPhone.IsValid(FarmerPhone.Normalise(p))));

		foreach (var page in new[] { "FarmerFertilizerHistory.razor", "FarmerYieldHistory.razor", "FarmerLeafCheckHistory.razor" })
		{
			var src = Page(page);
			var load = src[src.IndexOf("private async Task LoadAsync()", StringComparison.Ordinal)..];
			Check.That($"U9: {page} starts each load with a new version", load.Contains("var version = ++_loadVersion;"));
			Check.That($"U9: {page} ignores a stale answer after every await",
				System.Text.RegularExpressions.Regex.Matches(load, @"if \(version != _loadVersion\) return;").Count >= 3);
		}

		var sidebar = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Components", "Shell", "MobileSidebar.razor");
		var navMenu = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Layout", "NavMenu.razor");
		var farmerBlock = navMenu[navMenu.IndexOf("@if (loginState.UserRole == AppRole.Farmer)", StringComparison.Ordinal)..];
		farmerBlock = farmerBlock[..farmerBlock.IndexOf("My Samples", StringComparison.Ordinal)];
		var navHrefs = System.Text.RegularExpressions.Regex.Matches(farmerBlock, "href=\"(/farmer[^\"]*)\"").Select(m => m.Groups[1].Value).ToList();
		var drawer = SPIC.MauiBlazorApp.Shared.Components.Shell.MobileSidebar.FarmerMenu.Select(m => m.Href).ToList();
		Check.That("U6: the drawer lists every farmer tool of the desktop menu", navHrefs.Count >= 10 && navHrefs.All(drawer.Contains), string.Join(",", navHrefs.Except(drawer)));
		Check.That("U6: the drawer lists My call-back requests", drawer.Contains("/farmer/requests")
			&& SPIC.MauiBlazorApp.Shared.Components.Shell.MobileSidebar.FarmerMenu.Any(m => m.Label == "My call-back requests"));
		Check.That("U6: the drawer's farmer block is role-gated like the NavMenu", sidebar.Contains("@if (LoginState.UserRole == AppRole.Farmer)\n            {\n                @foreach (var item in FarmerMenu)"));
	}

	// ------------------------------------------------------------------ B5

	private static FarmerWeatherDayDto Wx(double? rain = 0, int? chance = 0, double? wind = 10, double? max = 31) =>
		new() { Date = "2026-11-28", RainMm = rain, RainChancePct = chance, WindKmh = wind, MaxTempC = max, MinTempC = 22, WeatherCode = 1 };

	private static string Codes(IEnumerable<FarmerAdvisoryDto> a) => string.Join(",", a.Select(x => x.Code));

	private static void WeatherData()
	{
		var dry = Enumerable.Range(0, 7).Select(_ => Wx(rain: 0.2, chance: 10)).ToList();
		Check.Equal("B5: a full dry, calm week still gives dry week + good days", "dry-week,spray-window", Codes(FarmerWeatherRules.Advisories(dry)));
		var noWind = Enumerable.Range(0, 7).Select(_ => Wx(rain: 0.2, chance: 10, wind: null)).ToList();
		Check.Equal("B5: no wind values: no 'good days for spraying'", "dry-week", Codes(FarmerWeatherRules.Advisories(noWind)));
		var noChance = Enumerable.Range(0, 7).Select(_ => Wx(rain: 0.2, chance: null)).ToList();
		Check.Equal("B5: no rain-chance values: neither dry week nor good days", "", Codes(FarmerWeatherRules.Advisories(noChance)));
		var oneGap = Enumerable.Range(0, 7).Select(i => Wx(rain: i == 5 ? null : 0.2, chance: 10)).ToList();
		Check.Equal("B5: one missing rain amount later in the week: no dry week, good days still (days 1-2 complete)", "spray-window", Codes(FarmerWeatherRules.Advisories(oneGap)));
		var tomorrowGap = Enumerable.Range(0, 7).Select(i => Wx(rain: 0.2, chance: i == 1 ? null : 10)).ToList();
		Check.Equal("B5: tomorrow's chance missing: no good days", "", Codes(FarmerWeatherRules.Advisories(tomorrowGap)));
		var heavyNoChance = new List<FarmerWeatherDayDto> { Wx(rain: 30, chance: null), Wx(rain: 30, chance: null), Wx(rain: 0, chance: null) };
		Check.That("B5: heavy rain is still warned about when the chance is missing", Codes(FarmerWeatherRules.Advisories(heavyNoChance)).StartsWith("heavy-rain"));
		Check.That("B5: a forecast with no rain amount or chance has no rain data",
			!FarmerWeatherRules.HasRainData(Enumerable.Range(0, 7).Select(_ => Wx(rain: null, chance: null)).ToList()));
		Check.That("B5: one rain chance is rain data", FarmerWeatherRules.HasRainData(new List<FarmerWeatherDayDto> { Wx(rain: null, chance: 40) }));
	}

	// ------------------------------------------------------------------ B5 / B6 (service)

	private sealed class OpenMeteoStub : HttpMessageHandler
	{
		public int Calls;
		public string Body = "";
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Calls++;
			return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(Body) });
		}
	}

	private sealed class StubFactory : IHttpClientFactory
	{
		private readonly HttpMessageHandler _handler;
		public StubFactory(HttpMessageHandler handler) => _handler = handler;
		public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
	}

	private const string FullWeek = """
		{"daily":{"time":["2026-11-28","2026-11-29","2026-11-30","2026-12-01","2026-12-02","2026-12-03","2026-12-04"],
		"weather_code":[1,1,1,1,1,1,1],"temperature_2m_max":[31,31,31,31,31,31,31],"temperature_2m_min":[22,22,22,22,22,22,22],
		"precipitation_sum":[0,0,0,0,0,0,0],"precipitation_probability_max":[5,5,5,5,5,5,5],"wind_speed_10m_max":[8,8,8,8,8,8,8]}}
		""";

	private const string NoRainValues = """
		{"daily":{"time":["2026-11-28","2026-11-29","2026-11-30"],"weather_code":[1,1,1],"temperature_2m_max":[31,31,31],
		"temperature_2m_min":[22,22,22],"precipitation_sum":[null,null,null],"precipitation_probability_max":[null,null,null],
		"wind_speed_10m_max":[null,null,null]}}
		""";

	private static void WeatherCap()
	{
		var stub = new OpenMeteoStub { Body = NoRainValues };
		var config = new ConfigurationBuilder().Build();
		var svc = new FarmerWeatherService(new StubFactory(stub), config, NullLogger<FarmerWeatherService>.Instance);
		var none = svc.ForecastAsync(9.17, 77.87, "farmer-x", CancellationToken.None).GetAwaiter().GetResult();
		Check.Equal("B5: an Open-Meteo answer with no rain values at all is Unavailable", FarmerWeatherStatus.Unavailable, none.Status);
		Check.That("B5: and gives no advice", none.Advisories.Count == 0);

		stub = new OpenMeteoStub { Body = FullWeek };
		svc = new FarmerWeatherService(new StubFactory(stub), config, NullLogger<FarmerWeatherService>.Instance);
		var statuses = Enumerable.Range(0, FarmerWeatherRules.FetchesPerFarmerPerHour + 1)
			.Select(i => svc.ForecastAsync(9.0 + i * 0.1, 77.8, "farmer-a", CancellationToken.None).GetAwaiter().GetResult().Status).ToList();
		Check.That("B6: a farmer's first 10 new points are fetched", statuses.Take(FarmerWeatherRules.FetchesPerFarmerPerHour).All(s => s == FarmerWeatherStatus.Ok));
		Check.Equal("B6: the 11th new point in the hour is refused without a call", FarmerWeatherStatus.Unavailable, statuses.Last());
		Check.Equal("B6: exactly 10 Open-Meteo calls were made", FarmerWeatherRules.FetchesPerFarmerPerHour, stub.Calls);
		Check.Equal("B6: a cached point still answers the capped farmer", FarmerWeatherStatus.Ok,
			svc.ForecastAsync(9.0, 77.8, "farmer-a", CancellationToken.None).GetAwaiter().GetResult().Status);
		Check.Equal("B6: the refused point was not cached as a failure for other farmers", FarmerWeatherStatus.Ok,
			svc.ForecastAsync(9.0 + FarmerWeatherRules.FetchesPerFarmerPerHour * 0.1, 77.8, "farmer-b", CancellationToken.None).GetAwaiter().GetResult().Status);
		Check.Equal("B6: cached answers make no call", FarmerWeatherRules.FetchesPerFarmerPerHour + 1, stub.Calls);
		Check.That("B6: the cache has a size limit", FarmerWeatherRules.CacheEntryLimit is > 0 and <= 100_000);
	}

	// ------------------------------------------------------------------ B8

	private static void CropGroups()
	{
		var rng = new Random(42);
		string[] crops = { "Paddy", "paddy ", "Banana", "Maize", "Cotton", "Groundnut", "" };
		var profiles = Enumerable.Range(0, 400).Select(i => (User: "p" + i, District: rng.Next(1, 4),
			Crops: string.Join(",", Enumerable.Range(0, rng.Next(0, 3)).Select(_ => crops[rng.Next(crops.Length)])))).ToList();
		// 30 of them also have SAS samples in the window (some in another district), plus 20 unlinked SAS farmers.
		var linked = profiles.Take(30).Select(p => new TrendCropFarmer("u:" + p.User, rng.Next(1, 4), new[] { crops[rng.Next(crops.Length)], "" })).ToList();
		var unlinked = Enumerable.Range(0, 20).Select(i => new TrendCropFarmer("s:" + i, rng.Next(1, 4), new[] { crops[rng.Next(crops.Length)] })).ToList();

		TrendCropFarmer Row((string User, int District, string Crops) p) =>
			new("u:" + p.User, p.District, p.Crops.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

		// Old path: every profile one by one, then the SAS rows.
		var old = profiles.Select(Row).Concat(linked).Concat(unlinked).ToList();
		// New path: linked profiles one by one, the rest as SQL-style groups.
		var linkedIds = linked.Select(l => l.FarmerKey).ToHashSet();
		var individuals = profiles.Where(p => linkedIds.Contains("u:" + p.User)).Select(Row).Concat(linked).Concat(unlinked).ToList();
		var groups = profiles.Where(p => !linkedIds.Contains("u:" + p.User))
			.GroupBy(p => (p.District, p.Crops)).Select(g => new TrendCropGroup(g.Key.District, g.Key.Crops, g.Count())).ToList();

		foreach (var district in new[] { 1, 2, 0 })
		{
			var a = FarmerTrendRules.Crops(old, district, "D", "S");
			var b = FarmerTrendRules.Crops(individuals, groups, district, "D", "S");
			string Show(FarmerCropTrendDto d) => $"{d.Scope}|{d.FarmerCount}|" + string.Join(";", d.Items.Select(x => $"{x.Crop}={x.Farmers}/{x.SharePct}"));
			Check.Equal($"B8: SQL-grouped profiles give the same crop card (district {district})", Show(a), Show(b));
		}
		Check.That("B8: the groups are far fewer rows than the profiles", groups.Count < profiles.Count / 3, $"{groups.Count} groups");
		var src = Source("SpicAPI", "Controllers", "FarmerTrendsController.cs");
		Check.That("B8: the trends controller counts profiles in SQL (GroupBy + Count), not one row per profile",
			src.Contains(".GroupBy(p => new { p.DistrictId, p.PrimaryCrops })") && src.Contains("Farmers = g.Count()"));
	}

	// ------------------------------------------------------------------ B3

	private static void PhoneSpelling()
	{
		Check.That("B3: '+919876543210' is another spelling of the verified number", FarmerPhone.IsOtherSpelling("+919876543210", "9876543210"));
		Check.That("B3: '+91 9876543210' too", FarmerPhone.IsOtherSpelling("+91 9876543210", "9876543210"));
		Check.That("B3: '09876543210' too", FarmerPhone.IsOtherSpelling("09876543210", "9876543210"));
		Check.That("B3: the plain 10 digits are not 'another spelling' (nothing to rewrite)", !FarmerPhone.IsOtherSpelling("9876543210", "9876543210"));
		Check.That("B3: a different number is not another spelling", !FarmerPhone.IsOtherSpelling("+919876543211", "9876543210"));
		Check.That("B3: no stored number is not another spelling", !FarmerPhone.IsOtherSpelling(null, "9876543210"));
		Check.Equal("B3: the profile phone is the 10 digits of '+91-9876543210'", "9876543210", FarmerPhone.ProfilePhone("+91-9876543210", null));
		Check.Equal("B3: the profile phone is the 10 digits of '+91 98765 43210'", "9876543210", FarmerPhone.ProfilePhone("+91 98765 43210", "x"));
		Check.That("B3: the profile phone always fits varchar(10)", new[] { "+919876543210", "0091 98765 43210", "09876543210", "9876543210" }
			.All(p => FarmerPhone.ProfilePhone(p, null).Length <= 10));
		Check.Equal("B3: an invalid account phone keeps the profile's current value", "9876543210", FarmerPhone.ProfilePhone("12345", "9876543210"));
		var unique = new DbUpdateException("x", new Npgsql.PostgresException("dup", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.UniqueViolation));
		var tooLong = new DbUpdateException("x", new Npgsql.PostgresException("long", "ERROR", "ERROR", Npgsql.PostgresErrorCodes.StringDataRightTruncation));
		Check.That("B3: a unique violation (23505) is the 'saved twice' race (409)", FarmerProfileController.IsUniqueViolation(unique));
		Check.That("B3: value too long (22001) is not a race (500, logged)", !FarmerProfileController.IsUniqueViolation(tooLong));
		Check.That("B3: a DbUpdateException without a Postgres error is not a race", !FarmerProfileController.IsUniqueViolation(new DbUpdateException("x")));
	}

	// ------------------------------------------------------------------ U1 / U5 rules

	private static string FakeJwt(string role, DateTime exp)
	{
		static string B64(string s) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
		var payload = "{\"http://schemas.microsoft.com/ws/2008/06/identity/claims/role\":\"" + role + "\",\"nameid\":\"u-" + role +
			"\",\"exp\":" + new DateTimeOffset(exp).ToUnixTimeSeconds() + "}";
		return B64("{\"alg\":\"none\"}") + "." + B64(payload) + ".sig";
	}

	private static void FarmerSessionRules()
	{
		var now = new DateTime(2026, 11, 28, 6, 0, 0, DateTimeKind.Utc);
		Check.Equal("U1: the role is read from a farmer JWT", AppRole.Farmer, SessionPersistence.RoleOf(FakeJwt("Farmer", now)) ?? AppRole.Admin);
		Check.Equal("U1: and from a staff JWT", AppRole.MDO, SessionPersistence.RoleOf(FakeJwt("MDO", now)) ?? AppRole.Admin);
		Check.That("U1: no token has no role", SessionPersistence.RoleOf(null) is null && SessionPersistence.RoleOf("junk") is null);
		Check.That("U1: an expired farmer JWT needs a refresh", SessionPersistence.NeedsFarmerRefresh(AppRole.Farmer, now.AddMinutes(-5), now));
		Check.That("U1: a farmer JWT with under a minute left needs a refresh", SessionPersistence.NeedsFarmerRefresh(AppRole.Farmer, now.AddSeconds(30), now));
		Check.That("U1: a farmer JWT with time left does not", !SessionPersistence.NeedsFarmerRefresh(AppRole.Farmer, now.AddMinutes(30), now));
		Check.That("U1: staff are never refreshed on restore (owner decides)", !SessionPersistence.NeedsFarmerRefresh(AppRole.MDO, now.AddMinutes(-5), now));
		Check.That("U1: the silent refresh runs before the 2-minute warning", SessionPersistence.FarmerRefreshAhead > TimeSpan.FromMinutes(2));
		Check.Equal("U5: a farmer signs out to the farmer sign-in", "/farmer/login", SessionPersistence.LoginPathFor(AppRole.Farmer));
		Check.Equal("U5: staff sign out to /login (unchanged)", "/login", SessionPersistence.LoginPathFor(AppRole.MDO));
		Check.Equal("U5: no role signs out to /login", "/login", SessionPersistence.LoginPathFor(null));
		Check.Equal("U5: a signed-out /farmer/fertilizer link opens the farmer sign-in", "/farmer/login", SessionPersistence.SignedOutLoginPath("farmer/fertilizer", false));
		Check.Equal("U5: a signed-out /farmer link too", "/farmer/login", SessionPersistence.SignedOutLoginPath("farmer", false));
		Check.Equal("U5: a staff link on a staff device keeps /login", "/login", SessionPersistence.SignedOutLoginPath("Sas/Collections", false));
		Check.Equal("U5: any signed-out link on a device whose last sign-in was a farmer opens the farmer sign-in", "/farmer/login", SessionPersistence.SignedOutLoginPath("", true));
		Check.That("U5: a look-alike staff route is not a farmer link", SessionPersistence.SignedOutLoginPath("farmer-requests", false) == "/login");
		Check.That("U5: the 'last sign-in was a farmer' flag survives sign-out (not in SessionKeys.All)", !SessionKeys.All.Contains(SessionKeys.LastSignInFarmer));
	}

	private sealed class MemoryStore : ISessionStore
	{
		public readonly Dictionary<string, string> Session = new();
		public readonly Dictionary<string, string> Persistent = new();
		public Task<string?> GetAsync(string key) =>
			Task.FromResult<string?>(Session.TryGetValue(key, out var v) && v.Length > 0 ? v : Persistent.TryGetValue(key, out var p) ? p : null);
		public Task SetAsync(string key, string value) { Session[key] = value; return Task.CompletedTask; }
		public Task RemoveAsync(string key) { Session.Remove(key); return Task.CompletedTask; }
		public Task SetPersistentAsync(string key, string value) { Persistent[key] = value; return Task.CompletedTask; }
		public Task RemovePersistentAsync(string key) { Persistent.Remove(key); return Task.CompletedTask; }
	}

	private sealed class RefreshStub : HttpMessageHandler
	{
		private readonly Func<HttpResponseMessage> _answer;
		public int Calls;
		public string? LastPath;
		public RefreshStub(Func<HttpResponseMessage> answer) => _answer = answer;
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			Calls++;
			LastPath = request.RequestUri?.AbsolutePath;
			return Task.FromResult(_answer());
		}
	}

	private sealed class StallStub : HttpMessageHandler
	{
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
			return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
		}
	}

	private static void FarmerRestore()
	{
		var expired = DateTime.UtcNow.AddMinutes(-10);
		var fresh = FakeJwt("Farmer", DateTime.UtcNow.AddHours(1));
		var freshExp = DateTime.UtcNow.AddHours(1);

		// 1. Expired farmer JWT + stored refresh token: refreshed before the form is shown.
		var store = new MemoryStore();
		store.Session[SessionKeys.Token] = FakeJwt("Farmer", expired);
		store.Session[SessionKeys.Expiration] = expired.ToString("O");
		store.Session[SessionKeys.RefreshToken] = "old-refresh";
		var ok = new RefreshStub(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
		{
			Content = System.Net.Http.Json.JsonContent.Create(new RefreshTokenResponseDto { Token = fresh, Expiration = freshExp, RefreshToken = "new-refresh" })
		});
		var login = new LoginState();
		var restored = SessionPersistence.TryRestoreAsync(store, login, new HttpClient(ok) { BaseAddress = new Uri("http://localhost/") }).GetAwaiter().GetResult();
		Check.That("U1: an expired farmer session is restored through the refresh token", restored && login.Token == fresh);
		Check.Equal("U1: the refresh endpoint was called once", "1|/api/Authentication/refresh", ok.Calls + "|" + ok.LastPath);
		Check.Equal("U1: the new refresh token is stored", "new-refresh", store.Session.GetValueOrDefault(SessionKeys.RefreshToken));
		Check.Equal("U1: the new JWT is stored", fresh, store.Session.GetValueOrDefault(SessionKeys.Token));

		// 2. The refresh fails: the form is shown and only then the refresh token is forgotten.
		var store2 = new MemoryStore();
		store2.Session[SessionKeys.Token] = FakeJwt("Farmer", expired);
		store2.Session[SessionKeys.Expiration] = expired.ToString("O");
		store2.Session[SessionKeys.RefreshToken] = "dead-refresh";
		store2.Persistent[SessionKeys.RefreshToken] = "dead-refresh";
		var refused = new RefreshStub(() => new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized));
		var login2 = new LoginState();
		var restored2 = SessionPersistence.TryRestoreAsync(store2, login2, new HttpClient(refused) { BaseAddress = new Uri("http://localhost/") }).GetAwaiter().GetResult();
		Check.That("U1: a refused refresh falls back to the sign-in form", !restored2 && refused.Calls == 1);
		Check.That("U1: the refused refresh token is cleared from both tiers after the attempt",
			!store2.Session.ContainsKey(SessionKeys.RefreshToken) && !store2.Persistent.ContainsKey(SessionKeys.RefreshToken));

		// 3. Staff: unchanged (no refresh call on restore).
		var store3 = new MemoryStore();
		store3.Session[SessionKeys.Token] = FakeJwt("MDO", expired);
		store3.Session[SessionKeys.Expiration] = expired.ToString("O");
		store3.Session[SessionKeys.RefreshToken] = "staff-refresh";
		var never = new RefreshStub(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
		var restored3 = SessionPersistence.TryRestoreAsync(store3, new LoginState(), new HttpClient(never) { BaseAddress = new Uri("http://localhost/") }).GetAwaiter().GetResult();
		Check.That("U1: an expired staff session is not refreshed on restore and keeps its refresh token",
			!restored3 && never.Calls == 0 && store3.Session.ContainsKey(SessionKeys.RefreshToken));

		// 4. A farmer with time left: no refresh call, restored as before.
		var store4 = new MemoryStore();
		store4.Session[SessionKeys.Token] = fresh;
		store4.Session[SessionKeys.Expiration] = freshExp.ToString("O");
		var never4 = new RefreshStub(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK));
		var restored4 = SessionPersistence.TryRestoreAsync(store4, new LoginState(), new HttpClient(never4) { BaseAddress = new Uri("http://localhost/") }).GetAwaiter().GetResult();
		Check.That("U1: a valid farmer session restores without a refresh call", restored4 && never4.Calls == 0);

		// N3: a transient failure keeps the stored session; only a refusal forgets it.
		foreach (var (label, answer) in new (string, Func<HttpResponseMessage>)[]
		{
			("a 503", () => new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)),
			("a 500", () => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError)),
			("no connection", () => throw new HttpRequestException("offline")),
		})
		{
			var st = new MemoryStore();
			st.Session[SessionKeys.Token] = FakeJwt("Farmer", expired);
			st.Session[SessionKeys.Expiration] = expired.ToString("O");
			st.Session[SessionKeys.RefreshToken] = "kept-refresh";
			st.Persistent[SessionKeys.RefreshToken] = "kept-refresh";
			var stub = new RefreshStub(answer);
			var r = SessionPersistence.TryRestoreAsync(st, new LoginState(), new HttpClient(stub) { BaseAddress = new Uri("http://localhost/") }).GetAwaiter().GetResult();
			Check.That($"N3: {label} on a cold-start refresh shows the form and keeps the refresh token (both tiers)",
				!r && stub.Calls == 1 && st.Session.GetValueOrDefault(SessionKeys.RefreshToken) == "kept-refresh"
				&& st.Persistent.GetValueOrDefault(SessionKeys.RefreshToken) == "kept-refresh" && st.Session.ContainsKey(SessionKeys.Token));
		}
		foreach (var code in new[] { System.Net.HttpStatusCode.BadRequest, System.Net.HttpStatusCode.Unauthorized, System.Net.HttpStatusCode.Forbidden })
			Check.That($"N3: {(int)code} from the refresh endpoint is a refusal", SessionPersistence.IsRefusal(code));
		Check.That("N3: 5xx / 429 / 404 are not refusals", !SessionPersistence.IsRefusal(System.Net.HttpStatusCode.InternalServerError)
			&& !SessionPersistence.IsRefusal(System.Net.HttpStatusCode.ServiceUnavailable) && !SessionPersistence.IsRefusal(System.Net.HttpStatusCode.TooManyRequests)
			&& !SessionPersistence.IsRefusal(System.Net.HttpStatusCode.NotFound));
		Check.Equal("N3: no stored refresh token counts as refused", RefreshOutcome.Refused,
			SessionPersistence.TryRefreshAsync(new HttpClient(never4) { BaseAddress = new Uri("http://localhost/") }, new MemoryStore(), new LoginState()).GetAwaiter().GetResult());

		// M4: a stalled refresh gives up after its own timeout and counts as Unreachable.
		var stall = new StallStub();
		var sw = System.Diagnostics.Stopwatch.StartNew();
		var stalled = SessionPersistence.TryRefreshAsync(new HttpClient(stall) { BaseAddress = new Uri("http://localhost/") }, new MemoryStore(), new LoginState(),
			"r", timeout: TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult();
		Check.That("M4: a stalled refresh ends at its timeout as Unreachable", stalled == RefreshOutcome.Unreachable && sw.Elapsed < TimeSpan.FromSeconds(5), $"{stalled} {sw.Elapsed}");
		Check.That("M4: the cold-start refresh timeout is short (10 s)", SessionPersistence.ColdStartRefreshTimeout == TimeSpan.FromSeconds(10));

		// N2: a refreshed farmer session goes only where the sign-in put it.
		HttpClient Ok() => new(new RefreshStub(() => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
		{
			Content = System.Net.Http.Json.JsonContent.Create(new RefreshTokenResponseDto { Token = fresh, Expiration = freshExp, RefreshToken = "new-refresh" })
		})) { BaseAddress = new Uri("http://localhost/") };
		var notKept = new MemoryStore();
		var nkLogin = new LoginState();
		SessionPersistence.StartAsync(Ok(), notKept, nkLogin, FakeJwt("Farmer", DateTime.UtcNow.AddMinutes(2)), DateTime.UtcNow.AddMinutes(2), "", "", "first-refresh", remember: false).GetAwaiter().GetResult();
		var nk = SessionPersistence.TryRefreshAsync(Ok(), notKept, nkLogin).GetAwaiter().GetResult();
		Check.That("N2: an unticked farmer's refresh writes no token or refresh token to persistent storage",
			nk == RefreshOutcome.Refreshed && !notKept.Persistent.ContainsKey(SessionKeys.Token) && !notKept.Persistent.ContainsKey(SessionKeys.RefreshToken)
			&& notKept.Session.GetValueOrDefault(SessionKeys.RefreshToken) == "new-refresh");
		var kept = new MemoryStore();
		var kLogin = new LoginState();
		SessionPersistence.StartAsync(Ok(), kept, kLogin, FakeJwt("Farmer", DateTime.UtcNow.AddMinutes(2)), DateTime.UtcNow.AddMinutes(2), "", "", "first-refresh", remember: true).GetAwaiter().GetResult();
		SessionPersistence.TryRefreshAsync(Ok(), kept, kLogin).GetAwaiter().GetResult();
		Check.That("N2: a ticked farmer's refresh updates persistent storage",
			kept.Persistent.GetValueOrDefault(SessionKeys.RefreshToken) == "new-refresh" && kept.Persistent.GetValueOrDefault(SessionKeys.Token) == fresh);
		Check.That("N2: staff refreshes are stored as before (both tiers)",
			SessionPersistence.PersistRefreshedAsync(new MemoryStore(), AppRole.MDO).GetAwaiter().GetResult());
		Check.That("N2: the 'remembered' marker is cleared with the session", SessionKeys.All.Contains(SessionKeys.Remembered));
		var coldNotKept = new MemoryStore();
		foreach (var kv in notKept.Persistent) coldNotKept.Persistent[kv.Key] = kv.Value; // a new browser session: localStorage only
		Check.That("N2: a cold start after an unticked sign-in finds no token to restore",
			!SessionPersistence.TryRestoreAsync(coldNotKept, new LoginState(), Ok()).GetAwaiter().GetResult()
			&& coldNotKept.GetAsync(SessionKeys.Token).GetAwaiter().GetResult() is null);

		// U5: the sign-in remembers a farmer device; a staff sign-in clears it.
		var store5 = new MemoryStore();
		SessionPersistence.StartAsync(new HttpClient(never4), store5, new LoginState(), fresh, freshExp, "", "", "r", remember: false).GetAwaiter().GetResult();
		Check.That("U5: a farmer sign-in remembers 'last sign-in was a farmer'", SessionPersistence.LastSignInWasFarmerAsync(store5).GetAwaiter().GetResult());
		SessionPersistence.StartAsync(new HttpClient(never4), store5, new LoginState(), FakeJwt("MDO", DateTime.UtcNow.AddHours(1)), freshExp, "", "", "r", remember: false).GetAwaiter().GetResult();
		Check.That("U5: a staff sign-in on the same device clears it", !SessionPersistence.LastSignInWasFarmerAsync(store5).GetAwaiter().GetResult());
	}

	// ------------------------------------------------------------------ U1 / U2 / U3 / U5 wiring (source)

	private static string Source(params string[] parts)
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SpicOne.sln"))) dir = dir.Parent;
		return File.ReadAllText(Path.Combine(new[] { dir?.FullName ?? throw new InvalidOperationException("SpicOne.sln not found") }.Concat(parts).ToArray()))
			.Replace("\r\n", "\n");
	}

	private static void ShellSources()
	{
		var layout = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Layout", "MainLayout.razor");
		Check.That("U1: the layout's expiry tick tries a silent farmer refresh before AutoLogout",
			layout.Contains("var outcome = await SessionPersistence.TryRefreshAsync(Http, SessionStore, LoginState);\n                    _farmerRefreshUnreachable = outcome == RefreshOutcome.Unreachable;")
			&& layout.Contains("await InvokeAsync(ExpiryTickAsync)"));
		var tick = layout[layout.IndexOf("private async Task ExpiryTickAsync()", StringComparison.Ordinal)..];
		tick = tick[..tick.IndexOf("private async Task AutoLogout()", StringComparison.Ordinal)];
		Check.That("N3: the expiry tick never AutoLogouts after an Unreachable farmer refresh (retries every 30 s)",
			tick.IndexOf("if (_farmerRefreshUnreachable) return;", StringComparison.Ordinal) is > 0 and var i
			&& i < tick.IndexOf("await AutoLogout();", StringComparison.Ordinal)
			&& tick.Contains("_farmerNextTryUtc = DateTime.UtcNow + FarmerRetryAfterUnreachable;"));
		Check.That("N3: a page refresh forgets the refresh token only when refused",
			layout.Contains("else if (outcome == RefreshOutcome.Refused)\n                        await SessionPersistence.ForgetRefreshTokenAsync(SessionStore);"));
		Check.That("N3: the expiry dialog does not leave a farmer's page at expiry (the layout decides)",
			layout.Contains("LeaveOnExpiry=\"@(LoginState.UserRole != AppRole.Farmer)\""));
		var dialog = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Components", "Feedback", "SessionExpiryDialog.razor");
		Check.That("N3: the dialog's Continue signs a farmer out only on a refused refresh",
			dialog.Contains("if (outcome == RefreshOutcome.Refused)\n            {\n                await SignOutNowAsync();") && dialog.Contains("if (LeaveOnExpiry) _ = HandleExpiredAsync();"));
		var handler = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Services", "AuthHttpMessageHandler.cs");
		Check.That("N2: the 401 handler stores a refreshed farmer session where the sign-in put it",
			handler.Contains("SessionPersistence.PersistRefreshedAsync(") && !handler.Contains("SetPersistentAsync(SessionKeys.RefreshToken"));
		var camera = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp", "Services", "MauiCamera.cs");
		Check.That("N1: the camera asks for StorageWrite below Android 13 before capture",
			camera.IndexOf("Permissions.RequestAsync<Permissions.StorageWrite>()", StringComparison.Ordinal) is > 0 and var s
			&& camera.Contains("if (!OperatingSystem.IsAndroidVersionAtLeast(33))")
			&& s < camera.IndexOf("MediaPicker.Default.CapturePhotoAsync", StringComparison.Ordinal));
		Check.That("N1: a storage refusal, a camera refusal and other failures have different messages",
			camera.Contains("return NativePhoto.Failed(StorageDenied);") && camera.Contains("return NativePhoto.Failed(CameraDenied);")
			&& camera.Contains("catch (PermissionException)\n            {") && System.Text.RegularExpressions.Regex.IsMatch(camera, @"catch \(PermissionException\)[^}]*Failed\(CouldNotOpen\)"));
		Check.That("M1: the capture asks for a downscaled, upright photo without EXIF",
			camera.Contains("MaximumWidth = CaptureSide,") && camera.Contains("MaximumHeight = CaptureSide,") && camera.Contains("public const int CaptureSide = 2048;")
			&& camera.Contains("CompressionQuality = CaptureQuality,") && camera.Contains("RotateImage = true,") && camera.Contains("PreserveMetaData = false"));
		Check.That("U5: the layout signs out to the role's sign-in page", layout.Contains("var loginPath = SessionPersistence.LoginPathFor(LoginState.UserRole);")
			&& !layout.Contains("Navigation.NavigateTo(\n            \"/login\"\n        );"));
		Check.That("U5: the expiry dialog gets the role's sign-in page", layout.Contains("<SessionExpiryDialog LoginPath=\"@SessionPersistence.LoginPathFor(LoginState.UserRole)\""));
		Check.That("U5: a 401 sends a farmer to the farmer sign-in", Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Services", "AuthHttpMessageHandler.cs")
			.Contains("_navigation.NavigateTo(loginPath, forceLoad: true);"));

		var assistant = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "FarmerPortal", "FarmerAssistant.razor");
		Check.That("U3: New chat ends any thread load (version bump and flag cleared)",
			assistant.Contains("private void NewChat()\n    {\n        _threadLoadVersion++;\n        _threadLoading = false;"));
		Check.That("U3: a stale thread load leaves the flag to the newest load",
			assistant.Contains("if (version != _threadLoadVersion || ConversationId != id) return;"));

		var manifest = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp", "Platforms", "Android", "AndroidManifest.xml");
		Check.That("U2: the Android manifest asks for the camera", manifest.Contains("android.permission.CAMERA"));
		Check.That("N1: the manifest declares WRITE_EXTERNAL_STORAGE up to API 32 (MAUI capture below Android 13)",
			manifest.Contains("<uses-permission android:name=\"android.permission.WRITE_EXTERNAL_STORAGE\" android:maxSdkVersion=\"32\" />"));
		Check.That("N1: and READ_EXTERNAL_STORAGE up to API 32",
			manifest.Contains("<uses-permission android:name=\"android.permission.READ_EXTERNAL_STORAGE\" android:maxSdkVersion=\"32\" />"));
		Check.That("U2: the Android manifest declares the IMAGE_CAPTURE query", manifest.Contains("android.media.action.IMAGE_CAPTURE"));
		Check.That("U2: the camera is optional on the Play Store", manifest.Contains("android.hardware.camera\" android:required=\"false\""));
		Check.That("U2: the MAUI host registers its camera", Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp", "MauiProgram.cs").Contains("AddSingleton<INativeCamera, SPIC.MauiBlazorApp.Services.MauiCamera>()"));
		Check.That("U2: the web host keeps the file input camera", Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Web", "Program.cs").Contains("AddSingleton<INativeCamera, NoNativeCamera>()"));
		Check.That("U2: the web host's camera is not 'available'", !new NoNativeCamera().IsAvailable && new NoNativeCamera().CaptureAsync().GetAwaiter().GetResult() is null);
		var leaf = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "FarmerPortal", "FarmerLeafCheck.razor");
		Check.That("U2: the leaf check keeps capture=\"environment\" for the web and iOS", leaf.Contains("capture=\"environment\""));
		Check.That("U2: the leaf check uses the host camera when it has one", leaf.Contains("@if (Camera.IsAvailable)") && leaf.Contains("@onclick=\"CaptureNativeAsync\""));
	}

	// ------------------------------------------------------------------ B1

	private const string FarmerA = "u-farmer-a";
	private const string Mdo = "u-mdo";

	private static void SasSampleScope()
	{
		using var db = NewDb();
		db.Users.Add(new UserInfo { Id = FarmerA, UserName = "f9000000001", PhoneNumber = "9000000001", Name = "Farmer A", Password = "x", UpdatedBy = "test", Role = AppRole.Farmer });
		db.Users.Add(new UserInfo { Id = Mdo, UserName = "qa.mdo", Name = "MDO", Password = "x", UpdatedBy = "test", Role = AppRole.MDO });
		db.SasFarmers.Add(new SasFarmer { Id = 1, Name = "Farmer A", Mobile = "9000000001", UserId = FarmerA, Village = "A village", SurveyNumber = "A-1" });
		db.SasFarmers.Add(new SasFarmer { Id = 2, Name = "Farmer B", Mobile = "9000000002", Village = "B village", SurveyNumber = "B-7", Latitude = 11.1, Longitude = 78.1 });
		db.SampleConsignments.Add(new SampleConsignment { Id = 30, Code = "CONS-T-1", CourierService = "x", TrackingNumber = "t" });
		// B's sample first, so the old "first item's farmer" summary name would have been B's.
		db.SampleCollections.Add(new SampleCollection { Id = 10, Code = "SMP-T-10", CollectedByUserId = Mdo, CollectedByName = "MDO", ConsignmentId = 30, Status = SampleCollectionStatus.Completed });
		db.SampleItems.Add(new SampleItem { Id = 101, CollectionId = 10, Code = "SMP-T-10-1", FarmerId = 2, SampleType = SampleType.Water });
		db.SampleItems.Add(new SampleItem { Id = 102, CollectionId = 10, Code = "SMP-T-10-2", FarmerId = 1, SampleType = SampleType.Soil });
		db.SampleLabResults.Add(new SampleLabResult { Id = 1001, SampleItemId = 101, Parameter = "pH", EnteredValue = "8.9" });
		db.SampleLabResults.Add(new SampleLabResult { Id = 1002, SampleItemId = 102, Parameter = "pH", EnteredValue = "6.5" });
		db.SaveChanges();

		var farmer = Sas(db, FarmerA, AppRole.Farmer);
		var detail = OkValue<SampleCollectionDetailDto>(farmer.GetCollection(10).GetAwaiter().GetResult());
		Check.That("B1: a farmer opens a shared collection that holds their sample", detail is not null);
		Check.Equal("B1: the farmer sees only their own sample", "102", string.Join(",", detail?.Items.Select(i => i.Id) ?? Array.Empty<int>()));
		Check.That("B1: no other farmer's name, mobile, village or survey number in the detail",
			detail is not null && detail.Items.All(i => i.Farmer.Id == 1));
		Check.Equal("B1: the summary farmer name is the caller's own", "Farmer A", detail?.FarmerName);
		Check.Equal("B1: the summary counts the caller's own samples", 1, detail?.SampleCount ?? -1);
		Check.Equal("B1: the summary sample types are the caller's own", "Soil", detail?.SampleTypes);

		Check.That("B1: another farmer's results in the same collection are 404",
			Sas(db, FarmerA, AppRole.Farmer).GetResults(101).GetAwaiter().GetResult() is NotFoundObjectResult);
		Check.That("B1: the farmer's own results still answer",
			Sas(db, FarmerA, AppRole.Farmer).GetResults(102).GetAwaiter().GetResult() is OkObjectResult);

		var cons = OkValue<ConsignmentDetailDto>(Sas(db, FarmerA, AppRole.Farmer).GetConsignment(30).GetAwaiter().GetResult());
		Check.Equal("B1: the consignment detail lists only the farmer's own sample", "102",
			string.Join(",", cons?.Samples.Select(i => i.Id) ?? Array.Empty<int>()));
		Check.Equal("B1: the consignment's collection summary carries the caller's name", "Farmer A",
			cons?.Collections.FirstOrDefault()?.FarmerName);

		var page = OkValue<PageResult<SampleCollectionSummaryDto>>(Sas(db, FarmerA, AppRole.Farmer)
			.GetCollections(null, null, null, null, null, null, "Farmer B", null).GetAwaiter().GetResult());
		Check.Equal("B1: a farmer's search cannot find a collection by another farmer's name", 0, page?.Total ?? -1);
		var mobilePage = OkValue<PageResult<SampleCollectionSummaryDto>>(Sas(db, FarmerA, AppRole.Farmer)
			.GetCollections(null, null, null, null, null, null, "9000000002", null).GetAwaiter().GetResult());
		Check.Equal("B1: nor by another farmer's mobile", 0, mobilePage?.Total ?? -1);
		var ownPage = OkValue<PageResult<SampleCollectionSummaryDto>>(Sas(db, FarmerA, AppRole.Farmer)
			.GetCollections(null, null, null, null, null, null, "Farmer A", null).GetAwaiter().GetResult());
		Check.Equal("B1: a farmer's search still finds their own collection", 1, ownPage?.Total ?? -1);

		// Staff unchanged.
		var staff = OkValue<SampleCollectionDetailDto>(Sas(db, Mdo, AppRole.MDO).GetCollection(10).GetAwaiter().GetResult());
		Check.Equal("B1: staff still see every sample of the collection", 2, staff?.Items.Count ?? -1);
		Check.Equal("B1: staff summary still counts every sample", 2, staff?.SampleCount ?? -1);
		Check.That("B1: staff still read any sample's results", Sas(db, Mdo, AppRole.MDO).GetResults(101).GetAwaiter().GetResult() is OkObjectResult);
		var staffCons = OkValue<ConsignmentDetailDto>(Sas(db, Mdo, AppRole.MDO).GetConsignment(30).GetAwaiter().GetResult());
		Check.Equal("B1: staff consignment detail still lists every sample", 2, staffCons?.Samples.Count ?? -1);
	}

	// ------------------------------------------------------------------ B2

	private static void FileSubtrees()
	{
		var root = Path.Combine(Path.GetTempPath(), "fp-recheck-" + Guid.NewGuid().ToString("N"));
		var uploads = Path.Combine(root, "Uploads");
		try
		{
			Write(uploads, "Community/5/20261128_photo.jpg");
			Write(uploads, "Community/avatars/u1.jpg");
			Write(uploads, "Library/3/cover.png");
			Write(uploads, "DealerRegistration/812/PAN_20261101101010.pdf");
			Write(uploads, "GuestHouseBookingDocuments/4/idproof_1.jpg");
			Write(uploads, "CommunityX/1/a.jpg");

			string Full(string rel) => Path.GetFullPath(Path.Combine(uploads, rel.Replace('/', Path.DirectorySeparatorChar)));
			Check.That("B2: a Community attachment is inside the Community subtree", FarmerFileScope.IsInside(uploads, Full("Community/5/20261128_photo.jpg"), FarmerFileScope.CommunitySubtree));
			Check.That("B2: a Community avatar is inside the Community subtree", FarmerFileScope.IsInside(uploads, Full("Community/avatars/u1.jpg"), FarmerFileScope.CommunitySubtree));
			Check.That("B2: dealer KYC is outside the Community subtree", !FarmerFileScope.IsInside(uploads, Full("DealerRegistration/812/PAN_20261101101010.pdf"), FarmerFileScope.CommunitySubtree));
			Check.That("B2: a look-alike folder (CommunityX) is outside", !FarmerFileScope.IsInside(uploads, Full("CommunityX/1/a.jpg"), FarmerFileScope.CommunitySubtree));
			Check.That("B2: the Community folder itself is not a file inside it", !FarmerFileScope.IsInside(uploads, Full("Community"), FarmerFileScope.CommunitySubtree));
			Check.That("B2: a library file is inside the Library subtree", FarmerFileScope.IsInside(uploads, Full("Library/3/cover.png"), FarmerFileScope.LibrarySubtree));
			Check.That("B2: a Community file is outside the Library subtree", !FarmerFileScope.IsInside(uploads, Full("Community/5/20261128_photo.jpg"), FarmerFileScope.LibrarySubtree));

			var env = new Env(root);
			using var db = NewDb();
			var config = new ConfigurationBuilder().Build();
			CommunityController Community(string? role) => new(db, env, new CommunityNearResolver(db), config) { ControllerContext = Ctx(role) };
			LibraryController Library(string? role) => new(db, env, NullLogger<LibraryController>.Instance) { ControllerContext = Ctx(role) };

			Check.That("B2: Community/file gives a farmer a Community attachment", Community("Farmer").ViewFile("Community/5/20261128_photo.jpg") is PhysicalFileResult);
			Check.That("B2: Community/file gives a farmer an avatar", Community("Farmer").ViewFile("Community/avatars/u1.jpg") is PhysicalFileResult);
			Check.That("B2: Community/file refuses a farmer dealer KYC (404)", Community("Farmer").ViewFile("DealerRegistration/812/PAN_20261101101010.pdf") is NotFoundObjectResult);
			Check.That("B2: Community/file refuses a farmer a guest-house ID proof (404)", Community("Farmer").ViewFile("GuestHouseBookingDocuments/4/idproof_1.jpg") is NotFoundObjectResult);
			Check.That("B2: Community/file refuses a farmer a library file (404)", Community("Farmer").ViewFile("Library/3/cover.png") is NotFoundObjectResult);
			Check.That("B2: Community/file refuses a farmer a look-alike folder (404)", Community("Farmer").ViewFile("CommunityX/1/a.jpg") is NotFoundObjectResult);
			Check.That("B2: Community/file staff behaviour unchanged (pre-existing, owner item)", Community("MDO").ViewFile("DealerRegistration/812/PAN_20261101101010.pdf") is PhysicalFileResult);

			Check.That("B2: Library/file gives a farmer a library file", Library("Farmer").GetFile("Library/3/cover.png") is PhysicalFileResult);
			Check.That("B2: Library/file refuses a farmer dealer KYC (404)", Library("Farmer").GetFile("DealerRegistration/812/PAN_20261101101010.pdf") is NotFoundObjectResult);
			Check.That("B2: Library/file refuses a farmer a Community file (404)", Library("Farmer").GetFile("Community/5/20261128_photo.jpg") is NotFoundObjectResult);
			Check.That("B2: Library/file staff behaviour unchanged (pre-existing, owner item)", Library("MDO").GetFile("DealerRegistration/812/PAN_20261101101010.pdf") is PhysicalFileResult);
		}
		finally
		{
			try { Directory.Delete(root, true); } catch { /* temp */ }
		}
	}

	// ------------------------------------------------------------------ B7

	private static void DeletionText()
	{
		var removed = string.Join(" ", FarmerPrivacyRules.RemovedItems).ToLowerInvariant();
		Check.That("B7: the deletion list names the Community attachments", removed.Contains("attached"));
		Check.That("B7: the deletion list names the page and error log link", removed.Contains("pages you opened"));
		Check.That("B7: the kept call-backs lose the message too",
			FarmerPrivacyRules.KeptItems.Any(i => i.Contains("call-back", StringComparison.OrdinalIgnoreCase) && i.Contains("message")));
		Check.That("B7: the deletion result counts the attachments",
			typeof(FarmerDeletionResult).GetProperty(nameof(FarmerDeletionResult.CommunityAttachments)) is not null);
	}

	// ------------------------------------------------------------------ helpers

	private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
		.UseInMemoryDatabase(Guid.NewGuid().ToString())
		.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
		.Options);

	private static SasController Sas(AppDbContext db, string userId, AppRole role) =>
		new(db, new Env(Path.GetTempPath()), new ConfigurationBuilder().Build()) { ControllerContext = Ctx(role.ToString(), userId) };

	private static ControllerContext Ctx(string? role, string userId = "u1")
	{
		var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId), new(ClaimTypes.Name, userId) };
		if (role is not null) claims.Add(new Claim(ClaimTypes.Role, role));
		return new ControllerContext
		{
			HttpContext = new DefaultHttpContext
			{
				User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role))
			}
		};
	}

	private static T? OkValue<T>(IActionResult result) where T : class => (result as OkObjectResult)?.Value as T;

	private static void Write(string uploads, string rel)
	{
		var full = Path.Combine(uploads, rel.Replace('/', Path.DirectorySeparatorChar));
		Directory.CreateDirectory(Path.GetDirectoryName(full)!);
		File.WriteAllBytes(full, new byte[] { 1, 2, 3 });
	}

	private sealed class Env : IWebHostEnvironment
	{
		public Env(string root) { ContentRootPath = root; WebRootPath = Path.Combine(root, "wwwroot"); }
		public string ApplicationName { get; set; } = "test";
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
		public string ContentRootPath { get; set; }
		public string EnvironmentName { get; set; } = "Test";
		public string WebRootPath { get; set; }
		public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
	}
}
