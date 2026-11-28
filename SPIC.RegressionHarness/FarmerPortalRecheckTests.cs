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
			layout.Contains("if (await SessionPersistence.TryRefreshAsync(Http, SessionStore, LoginState))\n                    {\n                        _farmerRefreshedAtUtc = DateTime.UtcNow;\n                        return;") && layout.Contains("await InvokeAsync(ExpiryTickAsync)"));
		Check.That("U5: the layout signs out to the role's sign-in page", layout.Contains("var loginPath = SessionPersistence.LoginPathFor(LoginState.UserRole);")
			&& !layout.Contains("Navigation.NavigateTo(\n            \"/login\"\n        );"));
		Check.That("U5: the expiry dialog gets the role's sign-in page", layout.Contains("<SessionExpiryDialog LoginPath=\"@SessionPersistence.LoginPathFor(LoginState.UserRole)\" />"));
		Check.That("U5: a 401 sends a farmer to the farmer sign-in", Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Services", "AuthHttpMessageHandler.cs")
			.Contains("_navigation.NavigateTo(loginPath, forceLoad: true);"));

		var assistant = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp.Shared", "Pages", "FarmerPortal", "FarmerAssistant.razor");
		Check.That("U3: New chat ends any thread load (version bump and flag cleared)",
			assistant.Contains("private void NewChat()\n    {\n        _threadLoadVersion++;\n        _threadLoading = false;"));
		Check.That("U3: a stale thread load leaves the flag to the newest load",
			assistant.Contains("if (version != _threadLoadVersion || ConversationId != id) return;"));

		var manifest = Source("SPIC.MauiBlazorApp", "SPIC.MauiBlazorApp", "Platforms", "Android", "AndroidManifest.xml");
		Check.That("U2: the Android manifest asks for the camera", manifest.Contains("android.permission.CAMERA"));
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
