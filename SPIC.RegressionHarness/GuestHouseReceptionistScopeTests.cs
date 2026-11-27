using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services.Payments;
using SPIC.Core.Entities;
using SpicAPI.Controllers;

namespace SPIC.RegressionHarness;

/// <summary>
/// Receptionist state scope on the customer-facing booking API (houses / availability / rooms/{id} / book).
/// Calls the REAL GuestHouseBookingController against a private InMemory database. A Receptionist sees and
/// books only guest houses of their own state (spic:state_id == GuestHouse.StateId); every other role is
/// unrestricted. Out-of-scope requests are 404, like the Front Office.
/// </summary>
internal static class GuestHouseReceptionistScopeTests
{
	private const int TN = 10, AP = 20;
	private const int TnHouse = 1, ApHouse = 2, TnHouse2 = 3;
	private const int TnRoom = 1, ApRoom = 2;

	public static void Run()
	{
		Console.WriteLine("GuestHouseReceptionistScope");
		Step("1. Tamil Nadu Receptionist sees only Tamil Nadu houses", () => Houses("u-rec-tn", AppRole.RM, TN, new[] { TnHouse, TnHouse2 }));
		Step("2. Andhra Pradesh Receptionist sees only Andhra Pradesh houses", () => Houses("u-rec-ap", AppRole.RM, AP, new[] { ApHouse }));
		Step("3. Receptionist with missing / invalid StateId sees none", MissingState);
		Step("4. Receptionist cannot reach another state's availability / room / book", OtherState);
		Step("5. Receptionist still reaches own state's endpoints", OwnState);
		Step("6. Admin / SuperAdmin / Dealer / Employee / other designation see all", Unrestricted);
	}

	private static void Step(string name, Func<Task> body)
	{
		Console.WriteLine($" {name}");
		try { body().GetAwaiter().GetResult(); }
		catch (Exception ex) { Check.That($"{name} ran to completion", false, $"{ex.GetType().Name}: {ex.Message}"); }
	}

	private static async Task<List<int>> Ids(GuestHouseBookingController c)
	{
		var result = await c.GetHouses();
		var list = (List<GuestHouseCardDto>)((OkObjectResult)result.Result!).Value!;
		return list.Select(h => h.Id).OrderBy(i => i).ToList();
	}

	private static async Task Houses(string user, AppRole role, int state, int[] expected)
	{
		using var fx = new Fx();
		var ids = await Ids(fx.Controller(user, role, state));
		Check.That("only own-state houses", ids.SequenceEqual(expected), string.Join(",", ids));
	}

	private static async Task MissingState()
	{
		using var fx = new Fx();
		foreach (var state in new int?[] { null, 0, -3, 999 })
		{
			var ids = await Ids(fx.Controller("u-rec-tn", AppRole.RM, state));
			Check.That($"StateId {(state?.ToString() ?? "absent")} sees none", ids.Count == 0, string.Join(",", ids));
		}
	}

	private static async Task OtherState()
	{
		using var fx = new Fx();
		var c = fx.Controller("u-rec-tn", AppRole.RM, TN);
		var from = DateTime.Today.AddDays(1);

		var avail = await c.GetAvailability(ApHouse, from, from.AddDays(2));
		Check.That("availability of other state is 404", avail is NotFoundObjectResult);
		var room = await c.GetRoom(ApRoom);
		Check.That("room of other state is 404", room is NotFoundObjectResult);
		var book = await c.CreateBooking(new CreateBookingRequest
		{
			GuestHouseId = ApHouse, RoomTypeId = ApRoom, CheckInDate = from, CheckOutDate = from.AddDays(1)
		});
		Check.That("booking in other state is 404", book is NotFoundObjectResult);
		Check.That("no booking was created", !await fx.Db.GuestHouseBookings.AnyAsync());
	}

	private static async Task OwnState()
	{
		using var fx = new Fx();
		var c = fx.Controller("u-rec-tn", AppRole.RM, TN);
		var from = DateTime.Today.AddDays(1);
		Check.That("own availability is 200", await c.GetAvailability(TnHouse, from, from.AddDays(2)) is OkObjectResult);
		Check.That("own room is 200", await c.GetRoom(TnRoom) is OkObjectResult);
		var book = await c.CreateBooking(new CreateBookingRequest
		{
			GuestHouseId = TnHouse, RoomTypeId = TnRoom, CheckInDate = from, CheckOutDate = from.AddDays(1)
		});
		Check.That("own booking is not rejected as out of scope",
			!(book is NotFoundObjectResult nf && nf.Value?.ToString()?.Contains("Guest House not found") == true));
	}

	private static async Task Unrestricted()
	{
		using var fx = new Fx();
		var all = new[] { TnHouse, ApHouse, TnHouse2 };
		foreach (var (user, role) in new[]
		{
			("u-admin", AppRole.Admin), ("u-super", AppRole.SuperAdmin), ("u-dealer", AppRole.Dealer),
			("u-emp", AppRole.RM), ("u-gm", AppRole.RM), ("u-ghadmin", AppRole.RM)
		})
		{
			var c = fx.Controller(user, role, TN);
			Check.That($"{user} sees all houses", (await Ids(c)).SequenceEqual(all));
			var from = DateTime.Today.AddDays(1);
			Check.That($"{user} reaches other-state availability", await c.GetAvailability(ApHouse, from, from.AddDays(2)) is OkObjectResult);
			Check.That($"{user} reaches other-state room", await c.GetRoom(ApRoom) is OkObjectResult);
		}
	}

	private sealed class StubRazorpay : IRazorpayService
	{
		public Task<RazorpayOrderResult> CreateOrderAsync(long amountInPaise, string receipt, CancellationToken cancellationToken = default) =>
			Task.FromResult(new RazorpayOrderResult { Success = true, OrderId = "order_test", AmountInPaise = amountInPaise, RawResponse = "{}" });
		public bool VerifySignature(string orderId, string paymentId, string signature) => false;
		public Task<RazorpayOrderPaymentsResult> GetOrderPaymentsAsync(string orderId, CancellationToken cancellationToken = default) =>
			Task.FromResult(new RazorpayOrderPaymentsResult { Success = false });
	}

	private sealed class StubEnv : IWebHostEnvironment
	{
		public string ApplicationName { get; set; } = "test";
		public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
		public string ContentRootPath { get; set; } = Path.GetTempPath();
		public string EnvironmentName { get; set; } = "Test";
		public string WebRootPath { get; set; } = Path.GetTempPath();
		public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
	}

	private sealed class Fx : IDisposable
	{
		public readonly AppDbContext Db;

		public Fx()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
				.Options;
			Db = new AppDbContext(options);

			Db.GuestHouses.Add(new GuestHouse { Id = TnHouse, Name = "TN House", IsActive = true, StateId = TN });
			Db.GuestHouses.Add(new GuestHouse { Id = ApHouse, Name = "AP House", IsActive = true, StateId = AP });
			Db.GuestHouses.Add(new GuestHouse { Id = TnHouse2, Name = "TN House 2", IsActive = true, StateId = TN });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = TnRoom, GuestHouseId = TnHouse, RoomType = "AC", RoomNumber = "101", PricePerNight = 1000m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = 3, IsActive = true });
			Db.GuestHouseRooms.Add(new GuestHouseRoom { Id = ApRoom, GuestHouseId = ApHouse, RoomType = "AC", RoomNumber = "101", PricePerNight = 1000m, Capacity = 2, NumberOfAdults = 2, AvailableQuantity = 3, IsActive = true });

			Db.Designations.Add(new Designation { Id = 1, Name = "Receptionist", RoleAccess = "GuestHouseBooking.View,FrontOffice.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 2, Name = "GM", RoleAccess = "GuestHouseBooking.View", IsActive = true });
			Db.Designations.Add(new Designation { Id = 3, Name = "GH Manager", RoleAccess = "GHAdmin.View,GuestHouseBooking.View", IsActive = true });
			Db.Users.Add(User("u-rec-tn", 1));
			Db.Users.Add(User("u-rec-ap", 1));
			Db.Users.Add(User("u-admin", 1));   // Admin role bypasses the designation
			Db.Users.Add(User("u-super", 1));   // SuperAdmin role bypasses the designation
			Db.Users.Add(User("u-dealer", null));
			Db.Users.Add(User("u-emp", null));
			Db.Users.Add(User("u-gm", 2));
			Db.Users.Add(User("u-ghadmin", 3));
			Db.SaveChanges();
		}

		private static UserInfo User(string id, int? designationId) =>
			new() { Id = id, UserName = id, DesignationId = designationId, Name = id, Password = "x", UpdatedBy = "test" };

		public GuestHouseBookingController Controller(string userId, AppRole role, int? stateId)
		{
			var claims = new List<Claim>
			{
				new(ClaimTypes.NameIdentifier, userId),
				new(ClaimTypes.Name, userId),
				new(ClaimTypes.Role, role.ToString())
			};
			if (stateId.HasValue) claims.Add(new Claim("spic:state_id", stateId.Value.ToString()));
			return new GuestHouseBookingController(Db, new StubEnv(), new StubRazorpay(), Options.Create(new RazorpayOptions()),
				Options.Create(new GuestHouseBookingOptions()), NullLogger<GuestHouseBookingController>.Instance)
			{
				ControllerContext = new ControllerContext
				{
					HttpContext = new DefaultHttpContext
					{
						User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role))
					}
				}
			};
		}

		public void Dispose() => Db.Dispose();
	}
}
