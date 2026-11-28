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
