using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;
using System.Text;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// SAS Portal: soil / water sample collection, payment, consignment and lab results.
	///
	/// Contracts are fixed in SPIC.Core/DTOs/SasDtos.cs (route list in its header) and
	/// SPIC.Core/Entities/SampleCollection.cs. Conventions follow CommunityController /
	/// LibraryController: AsNoTracking projections, { Success = false, Message } error
	/// envelopes, audit from User.Identity?.Name, uploads under Uploads/Sas/...
	///
	/// Roles
	///   WRITE  (create / edit / submit / pay / consign) : MDO, JMDO, Admin, CorporateAdmin
	///   REVIEW (approve payments, consignment status, lab results) : Admin, CorporateAdmin
	///   Farmer : reads only the collections that carry their own farmer record
	///   everyone else holding the page : reads everything, writes nothing
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class SasController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly IWebHostEnvironment _env;
		private readonly IConfiguration _config;

		public SasController(AppDbContext db, IWebHostEnvironment env, IConfiguration config)
		{
			_db = db;
			_env = env;
			_config = config;
		}

		// ---------------------------------------------------------------- constants

		private const int DefaultPageSize = 16;
		private const int MaxPageSize = 50;
		private const int FarmerPageSize = 20;
		private const long MaxImageBytes = 5L * 1024 * 1024;
		private const int MaxPhotosPerUpload = 10;

		private static readonly string[] AllowedImageExtensions =
			{ ".jpg", ".jpeg", ".png", ".webp" };

		private static readonly AppRole[] WriteRoles =
			{ AppRole.MDO, AppRole.JMDO, AppRole.Admin, AppRole.CorporateAdmin };

		private static readonly string[] PackagingTypes =
			{ "Ice Box", "Carton Box", "Thermocol Box", "Cover" };

		private static readonly string[] FallbackCrops =
		{
			"Paddy", "Wheat", "Tomato", "Sugarcane", "Cotton", "Groundnut", "Banana", "Maize"
		};

		// Defaults the lab-result form starts from (the Figma "Result Comparison" table).
		private static readonly LabParameterDto[] DefaultLabParameters =
		{
			new() { Parameter = "pH",             NormalRange = "6.5 - 7.5",  Unit = "-" },
			new() { Parameter = "EC",             NormalRange = "0 - 1.0",    Unit = "dS/m" },
			new() { Parameter = "Organic Carbon", NormalRange = "0.5 - 0.75", Unit = "%" },
			new() { Parameter = "Nitrogen",       NormalRange = "280 - 560",  Unit = "kg/ha" },
			new() { Parameter = "Phosphorus",     NormalRange = "22 - 56",    Unit = "kg/ha" },
			new() { Parameter = "Potassium",      NormalRange = "110 - 280",  Unit = "kg/ha" },
			new() { Parameter = "Zinc",           NormalRange = "0.6 - 1.2",  Unit = "ppm" }
		};

		// ---------------------------------------------------------------- lookups

		// GET /api/Sas/lookups
		[HttpGet("lookups")]
		public async Task<IActionResult> GetLookups()
		{
			var couriers = await _db.SasCouriers.AsNoTracking()
				.Where(c => c.IsActive)
				.OrderBy(c => c.Name)
				.Select(c => new SasCourierDto { Id = c.Id, Name = c.Name, TrackingUrlTemplate = c.TrackingUrlTemplate })
				.ToListAsync();

			var charges = await _db.SasSampleCharges.AsNoTracking()
				.Where(c => c.IsActive)
				.OrderBy(c => c.SampleType).ThenBy(c => c.Category)
				.Select(c => new SasChargeDto
				{
					SampleType = c.SampleType,
					Category = c.Category,
					AmountPerSample = c.AmountPerSample,
					NoOfTests = c.NoOfTests
				})
				.ToListAsync();

			var crops = await _db.Crops.AsNoTracking()
				.Where(c => c.IsActive)
				.OrderBy(c => c.Name)
				.Select(c => c.Name)
				.ToListAsync();

			if (crops.Count == 0)
				crops = FallbackCrops.ToList();

			var states = await _db.States.AsNoTracking()
				.Where(s => s.IsActive)
				.OrderBy(s => s.StateName)
				.Select(s => new SasStateDto { Id = s.Id, Name = s.StateName })
				.ToListAsync();

			var stateIds = states.Select(s => s.Id).ToList();
			var districts = await _db.Districts.AsNoTracking()
				.Where(d => d.IsActive && stateIds.Contains(d.StateId))
				.OrderBy(d => d.DistrictName)
				.Select(d => new { d.Id, d.DistrictName, d.StateId })
				.ToListAsync();

			foreach (var state in states)
			{
				state.Districts = districts
					.Where(d => d.StateId == state.Id)
					.Select(d => new SasDistrictDto { Id = d.Id, Name = d.DistrictName })
					.ToList();
			}

			return Ok(new SasLookupsDto
			{
				Couriers = couriers,
				Charges = charges,
				Crops = crops,
				States = states,
				PackagingTypes = PackagingTypes.ToList(),
				UpiId = _config["Sas:UpiId"] ?? "",
				MerchantName = _config["Sas:MerchantName"] ?? "",
				LabParameters = DefaultLabParameters
					.Select(p => new LabParameterDto { Parameter = p.Parameter, NormalRange = p.NormalRange, Unit = p.Unit })
					.ToList()
			});
		}

		// ---------------------------------------------------------------- farmers

		// GET /api/Sas/farmers?q=&page=&pageSize=
		[HttpGet("farmers")]
		public async Task<IActionResult> GetFarmers(
			[FromQuery] string? q,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = FarmerPageSize)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = FarmerPageSize;
			if (pageSize > MaxPageSize) pageSize = MaxPageSize;

			var query = _db.SasFarmers.AsNoTracking().Where(f => !f.IsDeleted);

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(f => f.Name.ToLower().Contains(term) || f.Mobile.ToLower().Contains(term));
			}

			var total = await query.CountAsync();
			var rows = await query
				.OrderBy(f => f.Name).ThenBy(f => f.Id)
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			return Ok(new PageResult<SasFarmerDto>
			{
				Items = rows.Select(MapFarmer).ToList(),
				Total = total,
				Page = page,
				PageSize = pageSize
			});
		}

		// GET /api/Sas/farmers/{id}
		[HttpGet("farmers/{id:int}")]
		public async Task<IActionResult> GetFarmer(int id)
		{
			var farmer = await _db.SasFarmers.AsNoTracking()
				.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

			if (farmer == null)
				return NotFound(new { Success = false, Message = "Farmer not found." });

			return Ok(MapFarmer(farmer));
		}

		// POST /api/Sas/farmers
		[HttpPost("farmers")]
		public async Task<IActionResult> CreateFarmer([FromBody] SasFarmerUpsertDto dto)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to add farmers.");

			if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
				return BadRequest(new { Success = false, Message = "The farmer's name is required." });
			if (string.IsNullOrWhiteSpace(dto.Mobile))
				return BadRequest(new { Success = false, Message = "The farmer's mobile number is required." });

			var mobile = dto.Mobile.Trim();
			var existing = await _db.SasFarmers.AsNoTracking()
				.FirstOrDefaultAsync(f => f.Mobile == mobile && !f.IsDeleted);

			if (existing != null)
			{
				return Conflict(new
				{
					Success = false,
					Message = $"A farmer with mobile {mobile} already exists (id {existing.Id}: {existing.Name}).",
					FarmerId = existing.Id
				});
			}

			var (districtName, stateName) = await ResolveLocationNamesAsync(dto.DistrictId, dto.StateId);
			var actor = User.Identity?.Name;

			var farmer = new SasFarmer
			{
				Name = dto.Name.Trim(),
				Mobile = mobile,
				Address1 = Clean(dto.Address1),
				Address2 = Clean(dto.Address2),
				Village = Clean(dto.Village),
				Taluk = Clean(dto.Taluk),
				DistrictId = dto.DistrictId,
				DistrictName = districtName,
				StateId = dto.StateId,
				StateName = stateName,
				PinCode = Clean(dto.PinCode),
				Latitude = dto.Latitude,
				Longitude = dto.Longitude,
				SurveyNumber = Clean(dto.SurveyNumber),
				// The login link decides what a Farmer account may read, so only review roles set it.
				UserId = IsReviewer() ? Clean(dto.UserId) : null,
				CreatedByUserId = CurrentUserId(),
				CreatedBy = actor,
				CreatedAt = DateTime.Now,
				UpdatedBy = actor,
				UpdatedAt = DateTime.Now
			};

			_db.SasFarmers.Add(farmer);
			await _db.SaveChangesAsync();

			return Ok(MapFarmer(farmer));
		}

		// PUT /api/Sas/farmers/{id}
		[HttpPut("farmers/{id:int}")]
		public async Task<IActionResult> UpdateFarmer(int id, [FromBody] SasFarmerUpsertDto dto)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to edit farmers.");

			var farmer = await _db.SasFarmers.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
			if (farmer == null)
				return NotFound(new { Success = false, Message = "Farmer not found." });

			if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
				return BadRequest(new { Success = false, Message = "The farmer's name is required." });
			if (string.IsNullOrWhiteSpace(dto.Mobile))
				return BadRequest(new { Success = false, Message = "The farmer's mobile number is required." });

			var mobile = dto.Mobile.Trim();
			var clash = await _db.SasFarmers.AsNoTracking()
				.FirstOrDefaultAsync(f => f.Mobile == mobile && f.Id != id && !f.IsDeleted);

			if (clash != null)
			{
				return Conflict(new
				{
					Success = false,
					Message = $"A farmer with mobile {mobile} already exists (id {clash.Id}: {clash.Name}).",
					FarmerId = clash.Id
				});
			}

			var (districtName, stateName) = await ResolveLocationNamesAsync(dto.DistrictId, dto.StateId);

			farmer.Name = dto.Name.Trim();
			farmer.Mobile = mobile;
			farmer.Address1 = Clean(dto.Address1);
			farmer.Address2 = Clean(dto.Address2);
			farmer.Village = Clean(dto.Village);
			farmer.Taluk = Clean(dto.Taluk);
			farmer.DistrictId = dto.DistrictId;
			farmer.DistrictName = districtName;
			farmer.StateId = dto.StateId;
			farmer.StateName = stateName;
			farmer.PinCode = Clean(dto.PinCode);
			farmer.Latitude = dto.Latitude;
			farmer.Longitude = dto.Longitude;
			farmer.SurveyNumber = Clean(dto.SurveyNumber);

			// A writer's payload never moves the login link (and never clears it).
			if (IsReviewer())
				farmer.UserId = Clean(dto.UserId);

			farmer.UpdatedBy = User.Identity?.Name;
			farmer.UpdatedAt = DateTime.Now;

			await _db.SaveChangesAsync();

			return Ok(MapFarmer(farmer));
		}

		// PATCH /api/Sas/farmers/{id}/link?userId=   (review roles; an empty userId unlinks)
		[HttpPatch("farmers/{id:int}/link")]
		public async Task<IActionResult> LinkFarmer(int id, [FromQuery] string? userId)
		{
			if (!IsReviewer())
				return Forbid403("Only Admin and Corporate Admin can link a farmer to a login.");

			var farmer = await _db.SasFarmers.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
			if (farmer == null)
				return NotFound(new { Success = false, Message = "Farmer not found." });

			var link = Clean(userId);

			if (link != null)
			{
				var account = await _db.Users.AsNoTracking()
					.Where(u => u.Id == link)
					.Select(u => new { u.Id, u.Role })
					.FirstOrDefaultAsync();

				if (account == null)
					return BadRequest(new { Success = false, Message = "No such user account." });

				if (account.Role != AppRole.Farmer)
					return BadRequest(new { Success = false, Message = "Only a Farmer account can be linked to a farmer record." });

				var taken = await _db.SasFarmers.AsNoTracking()
					.FirstOrDefaultAsync(f => f.UserId == link && f.Id != id && !f.IsDeleted);

				if (taken != null)
				{
					return Conflict(new
					{
						Success = false,
						Message = $"That login is already linked to farmer {taken.Id} ({taken.Name}).",
						FarmerId = taken.Id
					});
				}
			}

			farmer.UserId = link;
			farmer.UpdatedBy = User.Identity?.Name;
			farmer.UpdatedAt = DateTime.Now;
			await _db.SaveChangesAsync();

			return Ok(MapFarmer(farmer));
		}

		// ---------------------------------------------------------------- collection stats

		// GET /api/Sas/collections/stats
		[HttpGet("collections/stats")]
		public async Task<IActionResult> GetCollectionStats()
		{
			var query = await ScopedCollectionsAsync();

			var rows = await query
				.Select(c => new { c.Status, c.PaymentType })
				.ToListAsync();

			var ready = rows.Count(r => r.Status == SampleCollectionStatus.ReadyForConsignment);

			return Ok(new SampleCollectionStatsDto
			{
				Total = rows.Count,
				Free = rows.Count(r => r.PaymentType == SamplePaymentType.Free),
				Paid = rows.Count(r => r.PaymentType == SamplePaymentType.Paid),
				LabLevel = rows.Count(r => r.Status == SampleCollectionStatus.DeliveredToLab ||
										   r.Status == SampleCollectionStatus.TestInProgress),
				ReadyToSubmit = ready,
				Completed = rows.Count(r => r.Status == SampleCollectionStatus.Completed),
				PendingPayment = rows.Count(r => r.Status == SampleCollectionStatus.PendingPayment),
				PendingApproval = rows.Count(r => r.Status == SampleCollectionStatus.PendingApproval),
				ApprovedPayment = ready,
				Draft = rows.Count(r => r.Status == SampleCollectionStatus.Draft)
			});
		}

		// ---------------------------------------------------------------- collection list

		// GET /api/Sas/collections?status=&paymentType=&paymentStatus=&location=&from=&to=&q=&sort=&page=&pageSize=
		[HttpGet("collections")]
		public async Task<IActionResult> GetCollections(
			[FromQuery] string? status,
			[FromQuery] string? paymentType,
			[FromQuery] string? paymentStatus,
			[FromQuery] string? location,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to,
			[FromQuery] string? q,
			[FromQuery] string? sort,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = DefaultPageSize)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = DefaultPageSize;
			if (pageSize > MaxPageSize) pageSize = MaxPageSize;

			var query = await ScopedCollectionsAsync();

			if (!string.IsNullOrWhiteSpace(status) && TryParseEnum<SampleCollectionStatus>(status, out var statusValue))
				query = query.Where(c => c.Status == statusValue);

			if (!string.IsNullOrWhiteSpace(paymentType) && TryParseEnum<SamplePaymentType>(paymentType, out var typeValue))
				query = query.Where(c => c.PaymentType == typeValue);

			// paymentStatus filters on the LATEST payment of the collection.
			if (!string.IsNullOrWhiteSpace(paymentStatus) && TryParseEnum<SamplePaymentStatus>(paymentStatus, out var payStatus))
			{
				query = query.Where(c => c.Payments
					.OrderByDescending(p => p.Id)
					.Select(p => (SamplePaymentStatus?)p.Status)
					.FirstOrDefault() == payStatus);
			}

			if (!string.IsNullOrWhiteSpace(location))
			{
				var loc = location.Trim().ToLower();
				query = query.Where(c => c.LocationName != null && c.LocationName.ToLower().Contains(loc));
			}

			if (from.HasValue)
				query = query.Where(c => c.CollectionDate >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(c => c.CollectionDate < end);
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(c =>
					c.Code.ToLower().Contains(term) ||
					c.CollectedByName.ToLower().Contains(term) ||
					(c.LocationName != null && c.LocationName.ToLower().Contains(term)) ||
					c.Items.Any(i => !i.IsDeleted && i.Farmer != null &&
						(i.Farmer.Name.ToLower().Contains(term) || i.Farmer.Mobile.ToLower().Contains(term))));
			}

			query = (sort ?? "latest").ToLowerInvariant() switch
			{
				"oldest" => query.OrderBy(c => c.CollectionDate).ThenBy(c => c.Id),
				_ => query.OrderByDescending(c => c.CollectionDate).ThenByDescending(c => c.Id)
			};

			var total = await query.CountAsync();
			var ids = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(c => c.Id).ToListAsync();

			return Ok(new PageResult<SampleCollectionSummaryDto>
			{
				Items = await BuildSummariesAsync(ids),
				Total = total,
				Page = page,
				PageSize = pageSize
			});
		}

		// GET /api/Sas/collections/ready?paymentType=free|paid|both
		// The consignment form's picker. Field staff see their own collections; review roles
		// see every unconsigned one (they may bundle other people's collections).
		[HttpGet("collections/ready")]
		public async Task<IActionResult> GetReadyCollections([FromQuery] string? paymentType)
		{
			var query = (await ScopedCollectionsAsync())
				.Where(c => c.Status == SampleCollectionStatus.ReadyForConsignment && c.ConsignmentId == null);

			if (!IsReviewer())
			{
				var userId = CurrentUserId();
				query = query.Where(c => c.CollectedByUserId == userId);
			}

			switch ((paymentType ?? "both").ToLowerInvariant())
			{
				case "free":
					query = query.Where(c => c.PaymentType == SamplePaymentType.Free);
					break;
				case "paid":
					query = query.Where(c => c.PaymentType == SamplePaymentType.Paid);
					break;
			}

			var ids = await query
				.OrderByDescending(c => c.CollectionDate).ThenByDescending(c => c.Id)
				.Select(c => c.Id)
				.ToListAsync();

			return Ok(await BuildSummariesAsync(ids));
		}

		// GET /api/Sas/collections/{id}
		[HttpGet("collections/{id:int}")]
		public async Task<IActionResult> GetCollection(int id)
		{
			var collection = await _db.SampleCollections.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (collection == null || !await CanReadAsync(collection))
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			return Ok(await BuildDetailAsync(collection));
		}

		// POST /api/Sas/collections
		[HttpPost("collections")]
		public async Task<IActionResult> CreateCollection([FromBody] SampleCollectionUpsertDto dto)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to create sample collections.");

			if (dto == null)
				return BadRequest(new { Success = false, Message = "No collection supplied." });

			var validation = await ValidateItemsAsync(dto);
			if (validation.Error != null)
				return BadRequest(new { Success = false, Message = validation.Error });

			var actor = User.Identity?.Name;
			var name = await ResolveDisplayNameAsync();
			var locationName = await ResolveLocationAsync();

			await using var tx = await _db.Database.BeginTransactionAsync();

			var collection = new SampleCollection
			{
				Code = await NextCodeAsync("SMP"),
				CollectionDate = dto.CollectionDate == default ? DateTime.Now : dto.CollectionDate,
				CollectedByUserId = CurrentUserId(),
				CollectedByName = name,
				CollectedByRole = CurrentRole()?.ToString(),
				HeadquarterId = ClaimInt("spic:hq_id") > 0 ? ClaimInt("spic:hq_id") : null,
				LocationName = locationName,
				PaymentType = dto.PaymentType,
				PaidCategory = dto.PaymentType == SamplePaymentType.Paid ? dto.PaidCategory : null,
				Status = SampleCollectionStatus.Draft,
				Remarks = Clean(dto.Remarks),
				CreatedBy = actor,
				CreatedAt = DateTime.Now,
				UpdatedBy = actor,
				UpdatedAt = DateTime.Now
			};

			_db.SampleCollections.Add(collection);
			await _db.SaveChangesAsync();

			var index = 1;
			foreach (var item in dto.Items)
			{
				var (amount, tests) = validation.Price(item.SampleType);
				_db.SampleItems.Add(new SampleItem
				{
					CollectionId = collection.Id,
					Code = $"{collection.Code}-{index++}",
					FarmerId = item.FarmerId,
					SampleType = item.SampleType,
					Crop1 = Clean(item.Crop1),
					Crop2 = Clean(item.Crop2),
					Remarks = Clean(item.Remarks),
					NoOfTests = tests,
					Amount = amount,
					CreatedAt = DateTime.Now
				});
			}

			collection.TotalAmount = validation.Total;

			// Only a real draft gets a Draft row; a create-and-submit starts at Collected.
			if (dto.SaveAsDraft)
				AddEvent(collection.Id, null, nameof(SampleCollectionStatus.Draft), "Sample collection saved as draft.", name);

			await _db.SaveChangesAsync();

			if (!dto.SaveAsDraft)
				await ApplySubmitAsync(collection, name);

			await tx.CommitAsync();

			return Ok(await BuildDetailAsync(collection));
		}

		// PUT /api/Sas/collections/{id}   (Draft only)
		[HttpPut("collections/{id:int}")]
		public async Task<IActionResult> UpdateCollection(int id, [FromBody] SampleCollectionUpsertDto dto)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to edit sample collections.");

			var collection = await _db.SampleCollections
				.Include(c => c.Items)
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			if (!IsMine(collection))
				return Forbid403("You can only edit your own sample collections.");

			if (collection.Status != SampleCollectionStatus.Draft)
				return BadRequest(new { Success = false, Message = "Only a draft collection can be edited." });

			if (dto == null)
				return BadRequest(new { Success = false, Message = "No collection supplied." });

			var validation = await ValidateItemsAsync(dto);
			if (validation.Error != null)
				return BadRequest(new { Success = false, Message = validation.Error });

			var name = await ResolveDisplayNameAsync();

			collection.CollectionDate = dto.CollectionDate == default ? collection.CollectionDate : dto.CollectionDate;
			collection.PaymentType = dto.PaymentType;
			collection.PaidCategory = dto.PaymentType == SamplePaymentType.Paid ? dto.PaidCategory : null;
			collection.Remarks = Clean(dto.Remarks);
			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = DateTime.Now;

			// A draft's items are replaced wholesale; the codes are renumbered from 1.
			var keepIds = dto.Items.Where(i => i.Id.HasValue && i.Id.Value > 0).Select(i => i.Id!.Value).ToHashSet();
			foreach (var existing in collection.Items.Where(i => !i.IsDeleted && !keepIds.Contains(i.Id)))
				existing.IsDeleted = true;

			var index = 1;
			foreach (var item in dto.Items)
			{
				var (amount, tests) = validation.Price(item.SampleType);
				var row = item.Id.HasValue ? collection.Items.FirstOrDefault(i => i.Id == item.Id.Value) : null;

				if (row == null)
				{
					row = new SampleItem { CollectionId = collection.Id, CreatedAt = DateTime.Now };
					collection.Items.Add(row);
				}

				row.IsDeleted = false;
				row.Code = $"{collection.Code}-{index++}";
				row.FarmerId = item.FarmerId;
				row.SampleType = item.SampleType;
				row.Crop1 = Clean(item.Crop1);
				row.Crop2 = Clean(item.Crop2);
				row.Remarks = Clean(item.Remarks);
				row.NoOfTests = tests;
				row.Amount = amount;
			}

			collection.TotalAmount = validation.Total;
			await _db.SaveChangesAsync();

			if (!dto.SaveAsDraft)
				await ApplySubmitAsync(collection, name);

			return Ok(await BuildDetailAsync(collection));
		}

		// DELETE /api/Sas/collections/{id}   (Draft only, soft)
		[HttpDelete("collections/{id:int}")]
		public async Task<IActionResult> DeleteCollection(int id)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to delete sample collections.");

			var collection = await _db.SampleCollections.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			if (!IsMine(collection))
				return Forbid403("You can only delete your own sample collections.");

			if (collection.Status != SampleCollectionStatus.Draft)
				return BadRequest(new { Success = false, Message = "Only a draft collection can be deleted." });

			collection.IsDeleted = true;
			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = DateTime.Now;
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Draft collection deleted." });
		}

		// POST /api/Sas/collections/{id}/submit
		[HttpPost("collections/{id:int}/submit")]
		public async Task<IActionResult> SubmitCollection(int id)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to submit sample collections.");

			var collection = await _db.SampleCollections
				.Include(c => c.Items)
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			if (!IsMine(collection))
				return Forbid403("You can only submit your own sample collections.");

			if (collection.Status != SampleCollectionStatus.Draft)
				return BadRequest(new { Success = false, Message = "This collection has already been submitted." });

			if (!collection.Items.Any(i => !i.IsDeleted))
				return BadRequest(new { Success = false, Message = "Add at least one sample before submitting." });

			if (collection.PaymentType == SamplePaymentType.Paid && collection.PaidCategory == null)
				return BadRequest(new { Success = false, Message = "A paid collection needs a category (Farmer or NGO)." });

			await ApplySubmitAsync(collection, await ResolveDisplayNameAsync());

			return Ok(await BuildDetailAsync(collection));
		}

		// ---------------------------------------------------------------- payments

		// POST /api/Sas/collections/{id}/payment
		[HttpPost("collections/{id:int}/payment")]
		public async Task<IActionResult> CreatePayment(int id, [FromBody] SamplePaymentUpsertDto dto)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to record payments.");

			var collection = await _db.SampleCollections
				.Include(c => c.Payments)
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			if (!IsMine(collection))
				return Forbid403("You can only pay for your own sample collections.");

			if (collection.PaymentType != SamplePaymentType.Paid)
				return BadRequest(new { Success = false, Message = "This is a free collection; no payment is required." });

			if (collection.Status != SampleCollectionStatus.PendingPayment)
				return BadRequest(new { Success = false, Message = "This collection is not awaiting a payment." });

			if (collection.Payments.Any(p => p.Status == SamplePaymentStatus.Pending))
				return BadRequest(new { Success = false, Message = "A payment is already awaiting approval for this collection." });

			if (dto == null || string.IsNullOrWhiteSpace(dto.TransactionId))
				return BadRequest(new { Success = false, Message = "The transaction id is required." });
			if (string.IsNullOrWhiteSpace(dto.PaidByName))
				return BadRequest(new { Success = false, Message = "The payer's name is required." });

			var name = await ResolveDisplayNameAsync();

			var payment = new SamplePayment
			{
				CollectionId = collection.Id,
				TransactionId = dto.TransactionId.Trim(),
				BankGateway = Clean(dto.BankGateway),
				UtrNumber = Clean(dto.UtrNumber),
				PaidByName = dto.PaidByName.Trim(),
				ContactNumber = Clean(dto.ContactNumber),
				Email = Clean(dto.Email),
				Amount = collection.TotalAmount,
				Status = SamplePaymentStatus.Pending,
				CreatedBy = User.Identity?.Name,
				CreatedAt = DateTime.Now
			};

			_db.SamplePayments.Add(payment);

			collection.Status = SampleCollectionStatus.PendingApproval;
			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = DateTime.Now;
			AddEvent(collection.Id, null, nameof(SampleCollectionStatus.PendingApproval),
				$"Payment {payment.TransactionId} recorded, awaiting approval.", name);

			await _db.SaveChangesAsync();

			return Ok(MapPayment(payment));
		}

		// POST /api/Sas/collections/{id}/payment/proof   (multipart, field name "file")
		[HttpPost("collections/{id:int}/payment/proof")]
		[RequestSizeLimit(16 * 1024 * 1024)]
		public async Task<IActionResult> UploadPaymentProof(int id, [FromForm] IFormFile? file)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to upload payment proof.");

			var collection = await _db.SampleCollections.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			if (!IsMine(collection))
				return Forbid403("You can only upload proof for your own sample collections.");

			var problem = ValidateImage(file);
			if (problem != null)
				return BadRequest(new { Success = false, Message = problem });

			var folder = Path.Combine(GetUploadsRoot(), "Sas", "collections", id.ToString());
			Directory.CreateDirectory(folder);

			var storedName = $"payment_{DateTime.Now:yyyyMMddHHmmssfff}_{MakeSafeFileName(file!.FileName)}";
			var physicalPath = Path.Combine(folder, storedName);

			try
			{
				await using var stream = new FileStream(physicalPath, FileMode.Create);
				await file.CopyToAsync(stream);
			}
			catch (Exception)
			{
				return StatusCode(500, new { Success = false, Message = "The file could not be uploaded. Please try again." });
			}

			var relative = $"Sas/collections/{id}/{storedName}";

			// Attach it to the latest payment when one exists (the proof may also be
			// uploaded before the payment row is posted; the client sends it after).
			var payment = await _db.SamplePayments
				.Where(p => p.CollectionId == id)
				.OrderByDescending(p => p.Id)
				.FirstOrDefaultAsync();

			if (payment != null)
			{
				payment.ProofPath = relative;
				await _db.SaveChangesAsync();
			}

			return Ok(new SasFileDto
			{
				Path = relative,
				FileName = Path.GetFileName(file.FileName),
				Size = file.Length,
				ContentType = ResolveContentType(Path.GetExtension(storedName).ToLowerInvariant())
			});
		}

		// PATCH /api/Sas/payments/{id}/status?status=Approved|Rejected&reason=
		[HttpPatch("payments/{id:int}/status")]
		public async Task<IActionResult> SetPaymentStatus(int id, [FromQuery] string? status, [FromQuery] string? reason)
		{
			if (!IsReviewer())
				return Forbid403("Only Admin and Corporate Admin can review payments.");

			if (!TryParseEnum<SamplePaymentStatus>(status, out var newStatus) ||
				(newStatus != SamplePaymentStatus.Approved && newStatus != SamplePaymentStatus.Rejected))
			{
				return BadRequest(new { Success = false, Message = "Status must be Approved or Rejected." });
			}

			var payment = await _db.SamplePayments.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
				return NotFound(new { Success = false, Message = "Payment not found." });

			if (payment.Status != SamplePaymentStatus.Pending)
				return BadRequest(new { Success = false, Message = "This payment has already been reviewed." });

			var collection = await _db.SampleCollections.FirstOrDefaultAsync(c => c.Id == payment.CollectionId && !c.IsDeleted);
			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			if (newStatus == SamplePaymentStatus.Rejected && string.IsNullOrWhiteSpace(reason))
				return BadRequest(new { Success = false, Message = "A rejection needs a reason." });

			var name = await ResolveDisplayNameAsync();

			payment.Status = newStatus;
			payment.ReviewedByName = name;
			payment.ReviewedAt = DateTime.Now;
			payment.RejectReason = newStatus == SamplePaymentStatus.Rejected ? Clean(reason) : null;

			if (newStatus == SamplePaymentStatus.Approved)
			{
				collection.Status = SampleCollectionStatus.ReadyForConsignment;
				AddEvent(collection.Id, null, nameof(SampleCollectionStatus.ReadyForConsignment),
					$"Payment {payment.TransactionId} approved.", name);
			}
			else
			{
				collection.Status = SampleCollectionStatus.PendingPayment;
				AddEvent(collection.Id, null, nameof(SampleCollectionStatus.PendingPayment),
					$"Payment {payment.TransactionId} rejected: {Clean(reason)}", name);
			}

			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = DateTime.Now;
			await _db.SaveChangesAsync();

			return Ok(MapPayment(payment));
		}

		// ---------------------------------------------------------------- consignments

		// GET /api/Sas/consignments/stats
		[HttpGet("consignments/stats")]
		public async Task<IActionResult> GetConsignmentStats()
		{
			var ids = await (await ScopedConsignmentsAsync()).Select(c => c.Id).ToListAsync();

			var statuses = await _db.SampleConsignments.AsNoTracking()
				.Where(c => ids.Contains(c.Id))
				.Select(c => c.Status)
				.ToListAsync();

			var collections = await _db.SampleCollections.AsNoTracking()
				.Where(c => !c.IsDeleted && c.ConsignmentId != null && ids.Contains(c.ConsignmentId.Value))
				.Select(c => new { ConsignmentId = c.ConsignmentId!.Value, Samples = c.Items.Count(i => !i.IsDeleted) })
				.ToListAsync();

			var delivered = statuses.Count(s => s == ConsignmentStatus.Delivered || s == ConsignmentStatus.Completed);

			return Ok(new ConsignmentStatsDto
			{
				Total = ids.Count,
				WithMultipleCollections = collections.GroupBy(c => c.ConsignmentId).Count(g => g.Count() > 1),
				TotalSent = ids.Count,
				TotalSamples = collections.Sum(c => c.Samples),
				Delivered = delivered,
				Pending = ids.Count - delivered
			});
		}

		// GET /api/Sas/consignments?status=&courier=&paymentType=&from=&to=&q=&page=&pageSize=
		[HttpGet("consignments")]
		public async Task<IActionResult> GetConsignments(
			[FromQuery] string? status,
			[FromQuery] string? courier,
			[FromQuery] string? paymentType,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to,
			[FromQuery] string? q,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = DefaultPageSize)
		{
			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = DefaultPageSize;
			if (pageSize > MaxPageSize) pageSize = MaxPageSize;

			var query = await ScopedConsignmentsAsync();

			if (!string.IsNullOrWhiteSpace(status) && TryParseEnum<ConsignmentStatus>(status, out var statusValue))
				query = query.Where(c => c.Status == statusValue);

			if (!string.IsNullOrWhiteSpace(courier))
			{
				var name = courier.Trim().ToLower();
				query = query.Where(c => c.CourierService.ToLower().Contains(name));
			}

			if (!string.IsNullOrWhiteSpace(paymentType) && TryParseEnum<SamplePaymentType>(paymentType, out var typeValue))
				query = query.Where(c => c.Collections.Any(x => !x.IsDeleted && x.PaymentType == typeValue));

			if (from.HasValue)
				query = query.Where(c => c.DispatchedAt >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(c => c.DispatchedAt < end);
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(c =>
					c.Code.ToLower().Contains(term) ||
					c.TrackingNumber.ToLower().Contains(term) ||
					c.CourierService.ToLower().Contains(term) ||
					c.DispatchedByName.ToLower().Contains(term) ||
					c.Collections.Any(x => !x.IsDeleted && x.Code.ToLower().Contains(term)));
			}

			query = query.OrderByDescending(c => c.DispatchedAt).ThenByDescending(c => c.Id);

			var total = await query.CountAsync();
			var ids = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(c => c.Id).ToListAsync();

			return Ok(new PageResult<ConsignmentSummaryDto>
			{
				Items = await BuildConsignmentSummariesAsync(ids),
				Total = total,
				Page = page,
				PageSize = pageSize
			});
		}

		// GET /api/Sas/consignments/{id}
		[HttpGet("consignments/{id:int}")]
		public async Task<IActionResult> GetConsignment(int id)
		{
			var consignment = await _db.SampleConsignments.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (consignment == null || !await CanReadConsignmentAsync(consignment))
				return NotFound(new { Success = false, Message = "Consignment not found." });

			return Ok(await BuildConsignmentDetailAsync(consignment));
		}

		// POST /api/Sas/consignments
		[HttpPost("consignments")]
		public async Task<IActionResult> CreateConsignment([FromBody] ConsignmentUpsertDto dto)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to dispatch consignments.");

			if (dto == null || dto.CollectionIds == null || dto.CollectionIds.Count == 0)
				return BadRequest(new { Success = false, Message = "Select at least one sample collection." });

			if (string.IsNullOrWhiteSpace(dto.CourierService))
				return BadRequest(new { Success = false, Message = "The courier service is required." });
			if (string.IsNullOrWhiteSpace(dto.TrackingNumber))
				return BadRequest(new { Success = false, Message = "The tracking / AWB number is required." });

			var wanted = dto.CollectionIds.Distinct().ToList();
			var collections = await _db.SampleCollections
				.Where(c => wanted.Contains(c.Id) && !c.IsDeleted)
				.ToListAsync();

			if (collections.Count != wanted.Count)
				return BadRequest(new { Success = false, Message = "One or more of the selected collections no longer exists." });

			var userId = CurrentUserId();
			foreach (var collection in collections)
			{
				if (collection.Status != SampleCollectionStatus.ReadyForConsignment || collection.ConsignmentId != null)
					return BadRequest(new { Success = false, Message = $"{collection.Code} is not ready for consignment." });

				if (!IsReviewer() && !string.Equals(collection.CollectedByUserId, userId, StringComparison.OrdinalIgnoreCase))
					return Forbid403($"{collection.Code} was collected by somebody else.");
			}

			var name = await ResolveDisplayNameAsync();

			await using var tx = await _db.Database.BeginTransactionAsync();

			var consignment = new SampleConsignment
			{
				Code = await NextCodeAsync("CONS"),
				CourierService = dto.CourierService.Trim(),
				TrackingNumber = dto.TrackingNumber.Trim(),
				TrackingLink = Clean(dto.TrackingLink),
				ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
				PackageCount = dto.PackageCount < 1 ? 1 : dto.PackageCount,
				PackageWeightKg = dto.PackageWeightKg,
				PackagingType = Clean(dto.PackagingType),
				ContactPerson = Clean(dto.ContactPerson),
				ContactMobile = Clean(dto.ContactMobile),
				ContactEmail = Clean(dto.ContactEmail),
				Notes = Clean(dto.Notes),
				Status = ConsignmentStatus.Dispatched,
				DispatchedAt = DateTime.Now,
				DispatchedByUserId = userId,
				DispatchedByName = name,
				CreatedBy = User.Identity?.Name,
				CreatedAt = DateTime.Now,
				UpdatedAt = DateTime.Now
			};

			// No tracking link supplied: build one from the courier master when it has a template.
			if (string.IsNullOrWhiteSpace(consignment.TrackingLink))
			{
				var template = await _db.SasCouriers.AsNoTracking()
					.Where(c => c.Name == consignment.CourierService)
					.Select(c => c.TrackingUrlTemplate)
					.FirstOrDefaultAsync();

				if (!string.IsNullOrWhiteSpace(template) && template.Contains("{0}", StringComparison.Ordinal))
					consignment.TrackingLink = string.Format(template, consignment.TrackingNumber);
				else if (!string.IsNullOrWhiteSpace(template))
					consignment.TrackingLink = template;
			}

			_db.SampleConsignments.Add(consignment);
			await _db.SaveChangesAsync();

			foreach (var collection in collections)
			{
				collection.ConsignmentId = consignment.Id;
				collection.Status = SampleCollectionStatus.Dispatched;
				collection.UpdatedBy = User.Identity?.Name;
				collection.UpdatedAt = DateTime.Now;
				AddEvent(collection.Id, null, nameof(SampleCollectionStatus.Dispatched),
					$"Dispatched in {consignment.Code} ({consignment.CourierService} {consignment.TrackingNumber}).", name);
			}

			AddEvent(null, consignment.Id, nameof(ConsignmentStatus.Dispatched),
				$"{collections.Count} collection(s) dispatched via {consignment.CourierService}.", name);

			await _db.SaveChangesAsync();
			await tx.CommitAsync();

			return Ok(await BuildConsignmentDetailAsync(consignment));
		}

		// POST /api/Sas/consignments/{id}/photos?kind=ShippingLabel|Package|Other&title=
		[HttpPost("consignments/{id:int}/photos")]
		[RequestSizeLimit(64 * 1024 * 1024)]
		public async Task<IActionResult> UploadConsignmentPhotos(
			int id,
			[FromQuery] string? kind,
			[FromQuery] string? title,
			[FromForm] List<IFormFile> files)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to upload consignment photos.");

			var consignment = await _db.SampleConsignments.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
			if (consignment == null)
				return NotFound(new { Success = false, Message = "Consignment not found." });

			if (!IsReviewer() && !string.Equals(consignment.DispatchedByUserId, CurrentUserId(), StringComparison.OrdinalIgnoreCase))
				return Forbid403("You can only add photos to your own consignment.");

			if (files == null || files.Count == 0)
				return BadRequest(new { Success = false, Message = "No files uploaded." });
			if (files.Count > MaxPhotosPerUpload)
				return BadRequest(new { Success = false, Message = $"At most {MaxPhotosPerUpload} photos per upload." });

			foreach (var file in files)
			{
				var problem = ValidateImage(file);
				if (problem != null)
					return BadRequest(new { Success = false, Message = problem });
			}

			if (!TryParseEnum<ConsignmentPhotoKind>(kind, out var photoKind))
				photoKind = ConsignmentPhotoKind.Other;

			var folder = Path.Combine(GetUploadsRoot(), "Sas", "consignments", id.ToString());
			Directory.CreateDirectory(folder);

			var saved = new List<ConsignmentPhoto>();
			var name = await ResolveDisplayNameAsync();

			try
			{
				foreach (var file in files)
				{
					var storedName = $"{photoKind}_{DateTime.Now:yyyyMMddHHmmssfff}_{MakeSafeFileName(file.FileName)}";
					var physicalPath = Path.Combine(folder, storedName);

					await using (var stream = new FileStream(physicalPath, FileMode.Create))
					{
						await file.CopyToAsync(stream);
					}

					var photo = new ConsignmentPhoto
					{
						ConsignmentId = id,
						Kind = photoKind,
						Title = Clean(title),
						FileName = Path.GetFileName(file.FileName),
						StoredPath = $"Sas/consignments/{id}/{storedName}",
						ContentType = ResolveContentType(Path.GetExtension(storedName).ToLowerInvariant()),
						Size = file.Length,
						UploadedByName = name,
						CreatedAt = DateTime.Now
					};

					_db.ConsignmentPhotos.Add(photo);
					saved.Add(photo);
				}

				consignment.UpdatedAt = DateTime.Now;
				await _db.SaveChangesAsync();
			}
			catch (Exception)
			{
				return StatusCode(500, new { Success = false, Message = "The files could not be uploaded. Please try again." });
			}

			return Ok(saved.Select(MapPhoto).ToList());
		}

		// DELETE /api/Sas/consignments/photos/{photoId}
		[HttpDelete("consignments/photos/{photoId:int}")]
		public async Task<IActionResult> DeleteConsignmentPhoto(int photoId)
		{
			if (!IsWriter())
				return Forbid403("You are not allowed to remove consignment photos.");

			var photo = await _db.ConsignmentPhotos.FirstOrDefaultAsync(p => p.Id == photoId);
			if (photo == null)
				return NotFound(new { Success = false, Message = "Photo not found." });

			var consignment = await _db.SampleConsignments.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == photo.ConsignmentId);

			if (consignment != null && !IsReviewer() &&
				!string.Equals(consignment.DispatchedByUserId, CurrentUserId(), StringComparison.OrdinalIgnoreCase))
			{
				return Forbid403("You can only remove photos from your own consignment.");
			}

			var root = GetUploadsRoot();
			var normalized = photo.StoredPath.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
			var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			if (fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath))
			{
				try { System.IO.File.Delete(fullPath); } catch (Exception) { /* best effort */ }
			}

			_db.ConsignmentPhotos.Remove(photo);
			await _db.SaveChangesAsync();

			return Ok(new { Success = true, Message = "Photo removed." });
		}

		// PATCH /api/Sas/consignments/{id}/status?status=InTransit|Delivered|Completed
		[HttpPatch("consignments/{id:int}/status")]
		public async Task<IActionResult> SetConsignmentStatus(int id, [FromQuery] string? status)
		{
			if (!IsReviewer())
				return Forbid403("Only Admin and Corporate Admin can move a consignment.");

			if (!TryParseEnum<ConsignmentStatus>(status, out var newStatus) ||
				(newStatus != ConsignmentStatus.InTransit &&
				 newStatus != ConsignmentStatus.Delivered &&
				 newStatus != ConsignmentStatus.Completed))
			{
				return BadRequest(new { Success = false, Message = "Status must be InTransit, Delivered or Completed." });
			}

			var consignment = await _db.SampleConsignments
				.Include(c => c.Collections)
				.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

			if (consignment == null)
				return NotFound(new { Success = false, Message = "Consignment not found." });

			var name = await ResolveDisplayNameAsync();

			consignment.Status = newStatus;
			consignment.UpdatedAt = DateTime.Now;
			if (newStatus == ConsignmentStatus.Delivered && consignment.DeliveredAt == null)
				consignment.DeliveredAt = DateTime.Now;

			// The collections follow: InTransit -> InTransit, Delivered -> DeliveredToLab,
			// Completed -> Completed.
			var cascade = newStatus switch
			{
				ConsignmentStatus.InTransit => SampleCollectionStatus.InTransit,
				ConsignmentStatus.Delivered => SampleCollectionStatus.DeliveredToLab,
				_ => SampleCollectionStatus.Completed
			};

			foreach (var collection in consignment.Collections.Where(c => !c.IsDeleted))
			{
				collection.Status = cascade;
				collection.UpdatedBy = User.Identity?.Name;
				collection.UpdatedAt = DateTime.Now;

				if (cascade == SampleCollectionStatus.Completed && collection.CompletedAt == null)
					collection.CompletedAt = DateTime.Now;

				AddEvent(collection.Id, null, cascade.ToString(), $"{consignment.Code} is {newStatus}.", name);
			}

			AddEvent(null, consignment.Id, newStatus.ToString(), null, name);
			await _db.SaveChangesAsync();

			return Ok(await BuildConsignmentDetailAsync(consignment));
		}

		// ---------------------------------------------------------------- lab results

		// GET /api/Sas/samples/{itemId}/results
		[HttpGet("samples/{itemId:int}/results")]
		public async Task<IActionResult> GetResults(int itemId)
		{
			var item = await _db.SampleItems.AsNoTracking()
				.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted);

			if (item == null)
				return NotFound(new { Success = false, Message = "Sample not found." });

			var collection = await _db.SampleCollections.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == item.CollectionId && !c.IsDeleted);

			if (collection == null || !await CanReadAsync(collection))
				return NotFound(new { Success = false, Message = "Sample not found." });

			var results = await _db.SampleLabResults.AsNoTracking()
				.Where(r => r.SampleItemId == itemId)
				.OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
				.ToListAsync();

			return Ok(results.Select(MapResult).ToList());
		}

		// PUT /api/Sas/samples/{itemId}/results   (review roles; replaces the sample's results)
		[HttpPut("samples/{itemId:int}/results")]
		public async Task<IActionResult> PutResults(int itemId, [FromBody] List<LabResultUpsertDto> body)
		{
			if (!IsReviewer())
				return Forbid403("Only Admin and Corporate Admin can enter lab results.");

			var item = await _db.SampleItems.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted);
			if (item == null)
				return NotFound(new { Success = false, Message = "Sample not found." });

			var collection = await _db.SampleCollections
				.Include(c => c.Items)
				.FirstOrDefaultAsync(c => c.Id == item.CollectionId && !c.IsDeleted);

			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			var rows = (body ?? new List<LabResultUpsertDto>())
				.Where(r => !string.IsNullOrWhiteSpace(r.Parameter))
				.ToList();

			var name = await ResolveDisplayNameAsync();

			var existing = await _db.SampleLabResults.Where(r => r.SampleItemId == itemId).ToListAsync();
			_db.SampleLabResults.RemoveRange(existing);

			foreach (var row in rows)
			{
				_db.SampleLabResults.Add(new SampleLabResult
				{
					SampleItemId = itemId,
					Parameter = row.Parameter.Trim(),
					NormalRange = Clean(row.NormalRange),
					EnteredValue = Clean(row.EnteredValue),
					Unit = Clean(row.Unit),
					ResultLabel = Clean(row.ResultLabel),
					Status = row.Status,
					Hint = Clean(row.Hint),
					SortOrder = row.SortOrder,
					EnteredByName = name,
					EnteredAt = DateTime.Now
				});
			}

			await _db.SaveChangesAsync();

			// The collection is Completed once every live sample carries at least one result.
			var itemIds = collection.Items.Where(i => !i.IsDeleted).Select(i => i.Id).ToList();
			var withResults = await _db.SampleLabResults.AsNoTracking()
				.Where(r => itemIds.Contains(r.SampleItemId))
				.Select(r => r.SampleItemId)
				.Distinct()
				.CountAsync();

			var allDone = itemIds.Count > 0 && withResults == itemIds.Count;
			var target = allDone ? SampleCollectionStatus.Completed : SampleCollectionStatus.TestInProgress;

			if (collection.Status != target)
			{
				collection.Status = target;
				collection.UpdatedBy = User.Identity?.Name;
				collection.UpdatedAt = DateTime.Now;

				if (allDone && collection.CompletedAt == null)
					collection.CompletedAt = DateTime.Now;
				else if (!allDone)
					collection.CompletedAt = null;   // a result was cleared again

				AddEvent(collection.Id, null, target.ToString(),
					allDone ? "All lab results entered." : $"Results entered for {item.Code}.", name);

				await _db.SaveChangesAsync();
			}

			var saved = await _db.SampleLabResults.AsNoTracking()
				.Where(r => r.SampleItemId == itemId)
				.OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
				.ToListAsync();

			return Ok(saved.Select(MapResult).ToList());
		}

		// ---------------------------------------------------------------- files

		// GET /api/Sas/file/{*path}   (also accepts ?access_token=, see Program.cs)
		[HttpGet("file/{*path}")]
		public IActionResult ViewFile(string path)
		{
			if (string.IsNullOrWhiteSpace(path) ||
				path.Contains("..", StringComparison.Ordinal) ||
				path.IndexOf(':') >= 0)
			{
				return NotFound(new { Success = false, Message = "File not found." });
			}

			var root = GetUploadsRoot();
			var normalized = path.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
			var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) || !System.IO.File.Exists(fullPath))
				return NotFound(new { Success = false, Message = "File not found." });

			return PhysicalFile(fullPath, ResolveContentType(Path.GetExtension(fullPath).ToLowerInvariant()));
		}

		// ---------------------------------------------------------------- status transitions

		// Draft -> Free: ReadyForConsignment (Collected + ReadyForConsignment)
		//       -> Paid: PendingPayment      (Collected + PendingPayment)
		private async Task ApplySubmitAsync(SampleCollection collection, string name)
		{
			collection.SubmittedAt = DateTime.Now;
			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = DateTime.Now;

			AddEvent(collection.Id, null, nameof(SampleCollectionStatus.Collected), "Samples collected and submitted.", name);

			if (collection.PaymentType == SamplePaymentType.Free)
			{
				collection.Status = SampleCollectionStatus.ReadyForConsignment;
				AddEvent(collection.Id, null, nameof(SampleCollectionStatus.ReadyForConsignment),
					"Free sample: ready for consignment.", name);
			}
			else
			{
				collection.Status = SampleCollectionStatus.PendingPayment;
				AddEvent(collection.Id, null, nameof(SampleCollectionStatus.PendingPayment),
					$"Payment of Rs. {collection.TotalAmount:0.##} pending.", name);
			}

			await _db.SaveChangesAsync();
		}

		private void AddEvent(int? collectionId, int? consignmentId, string status, string? note, string? byName)
		{
			_db.SasStatusEvents.Add(new SasStatusEvent
			{
				CollectionId = collectionId,
				ConsignmentId = consignmentId,
				Status = status,
				Note = note,
				ByName = byName,
				At = DateTime.Now
			});
		}

		// ---------------------------------------------------------------- pricing

		private sealed class ItemValidation
		{
			public string? Error { get; init; }
			public decimal Total { get; init; }
			public Dictionary<SampleType, (decimal Amount, int Tests)> Prices { get; init; } = new();

			public (decimal Amount, int Tests) Price(SampleType type) =>
				Prices.TryGetValue(type, out var value) ? value : (0m, 1);
		}

		/// <summary>Validates the items and prices them: Free -> every item 0, category forced
		/// to null; Paid -> the charge master decides the amount and the number of tests.</summary>
		private async Task<ItemValidation> ValidateItemsAsync(SampleCollectionUpsertDto dto)
		{
			if (dto.Items == null || dto.Items.Count == 0)
				return new ItemValidation { Error = "Add at least one sample." };

			var farmerIds = dto.Items.Select(i => i.FarmerId).Distinct().ToList();
			if (farmerIds.Any(id => id <= 0))
				return new ItemValidation { Error = "Every sample needs a farmer." };

			var known = await _db.SasFarmers.AsNoTracking()
				.Where(f => farmerIds.Contains(f.Id) && !f.IsDeleted)
				.Select(f => f.Id)
				.ToListAsync();

			if (known.Count != farmerIds.Count)
				return new ItemValidation { Error = "One or more of the selected farmers no longer exists." };

			if (dto.PaymentType == SamplePaymentType.Free)
			{
				return new ItemValidation
				{
					Total = 0m,
					Prices = dto.Items.Select(i => i.SampleType).Distinct()
						.ToDictionary(t => t, _ => (0m, 1))
				};
			}

			if (dto.PaidCategory == null)
				return new ItemValidation { Error = "A paid collection needs a category (Farmer or NGO)." };

			var category = dto.PaidCategory.Value;
			var types = dto.Items.Select(i => i.SampleType).Distinct().ToList();

			var charges = await _db.SasSampleCharges.AsNoTracking()
				.Where(c => c.IsActive && c.Category == category && types.Contains(c.SampleType))
				.ToListAsync();

			var prices = new Dictionary<SampleType, (decimal, int)>();
			foreach (var type in types)
			{
				var charge = charges.FirstOrDefault(c => c.SampleType == type);
				if (charge == null)
					return new ItemValidation { Error = $"No price is configured for {type} / {category}." };

				prices[type] = (charge.AmountPerSample, charge.NoOfTests < 1 ? 1 : charge.NoOfTests);
			}

			var total = dto.Items.Sum(i => prices[i.SampleType].Item1);

			return new ItemValidation { Total = total, Prices = prices };
		}

		// ---------------------------------------------------------------- codes

		/// <summary>"SMP-2026-000001" / "CONS-2026-000001", numbered from the highest existing
		/// code of the year. Callers run it inside a transaction so two collections saved at the
		/// same moment cannot share a code (the unique index is the final guard).</summary>
		private async Task<string> NextCodeAsync(string prefix)
		{
			var head = $"{prefix}-{DateTime.Now.Year}-";

			var codes = prefix == "CONS"
				? await _db.SampleConsignments.AsNoTracking().Where(c => c.Code.StartsWith(head)).Select(c => c.Code).ToListAsync()
				: await _db.SampleCollections.AsNoTracking().Where(c => c.Code.StartsWith(head)).Select(c => c.Code).ToListAsync();

			var max = 0;
			foreach (var code in codes)
			{
				var tail = code.Length > head.Length ? code[head.Length..] : "";
				if (int.TryParse(tail, out var value) && value > max) max = value;
			}

			return head + (max + 1).ToString("000000");
		}

		// ---------------------------------------------------------------- read scoping

		/// <summary>The collections the caller may see: everything, except a Farmer, who only
		/// sees collections carrying their own farmer record.</summary>
		private async Task<IQueryable<SampleCollection>> ScopedCollectionsAsync()
		{
			var query = _db.SampleCollections.AsNoTracking().Where(c => !c.IsDeleted);

			if (CurrentRole() != AppRole.Farmer)
				return query;

			var farmerIds = await MyFarmerIdsAsync();
			if (farmerIds.Count == 0)
				return query.Where(c => false);

			return query.Where(c => c.Items.Any(i => !i.IsDeleted && farmerIds.Contains(i.FarmerId)));
		}

		private async Task<IQueryable<SampleConsignment>> ScopedConsignmentsAsync()
		{
			var query = _db.SampleConsignments.AsNoTracking().Where(c => !c.IsDeleted);

			if (CurrentRole() != AppRole.Farmer)
				return query;

			var farmerIds = await MyFarmerIdsAsync();
			if (farmerIds.Count == 0)
				return query.Where(c => false);

			return query.Where(c => c.Collections.Any(x => !x.IsDeleted &&
				x.Items.Any(i => !i.IsDeleted && farmerIds.Contains(i.FarmerId))));
		}

		/// <summary>A signed-in Farmer's own farmer rows. Matched on SasFarmer.UserId (set when
		/// the farmer record is linked to a login) and, because farmers are expected to log in
		/// with their mobile number, on SasFarmer.Mobile == the account's user name / phone.</summary>
		private async Task<List<int>> MyFarmerIdsAsync()
		{
			var userId = CurrentUserId();
			if (string.IsNullOrWhiteSpace(userId)) return new List<int>();

			if (_myFarmerIds != null) return _myFarmerIds;

			var account = await _db.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => new { u.UserName, u.PhoneNumber })
				.FirstOrDefaultAsync();

			var userName = account?.UserName ?? "";
			var phone = account?.PhoneNumber ?? "";

			_myFarmerIds = await _db.SasFarmers.AsNoTracking()
				.Where(f => !f.IsDeleted && (
					f.UserId == userId ||
					(userName != "" && f.Mobile == userName) ||
					(phone != "" && f.Mobile == phone)))
				.Select(f => f.Id)
				.ToListAsync();

			return _myFarmerIds;
		}

		private List<int>? _myFarmerIds;

		private async Task<bool> CanReadAsync(SampleCollection collection)
		{
			if (CurrentRole() != AppRole.Farmer) return true;

			var farmerIds = await MyFarmerIdsAsync();
			if (farmerIds.Count == 0) return false;

			return await _db.SampleItems.AsNoTracking()
				.AnyAsync(i => i.CollectionId == collection.Id && !i.IsDeleted && farmerIds.Contains(i.FarmerId));
		}

		private async Task<bool> CanReadConsignmentAsync(SampleConsignment consignment)
		{
			if (CurrentRole() != AppRole.Farmer) return true;

			var farmerIds = await MyFarmerIdsAsync();
			if (farmerIds.Count == 0) return false;

			return await _db.SampleCollections.AsNoTracking()
				.AnyAsync(c => c.ConsignmentId == consignment.Id && !c.IsDeleted &&
					c.Items.Any(i => !i.IsDeleted && farmerIds.Contains(i.FarmerId)));
		}

		// ---------------------------------------------------------------- projections

		private async Task<List<SampleCollectionSummaryDto>> BuildSummariesAsync(List<int> ids)
		{
			if (ids.Count == 0) return new List<SampleCollectionSummaryDto>();

			var rows = await _db.SampleCollections.AsNoTracking()
				.Where(c => ids.Contains(c.Id))
				.Select(c => new
				{
					c.Id,
					c.Code,
					c.CollectionDate,
					c.CollectedByName,
					c.CollectedByUserId,
					c.LocationName,
					c.PaymentType,
					c.PaidCategory,
					c.Status,
					c.TotalAmount,
					c.ConsignmentId,
					ConsignmentCode = c.Consignment != null ? c.Consignment.Code : null,
					Items = c.Items.Where(i => !i.IsDeleted)
						.OrderBy(i => i.Id)
						.Select(i => new { i.SampleType, FarmerName = i.Farmer != null ? i.Farmer.Name : "" })
						.ToList(),
					Payment = c.Payments.OrderByDescending(p => p.Id)
						.Select(p => new { p.Status, p.ReviewedAt })
						.FirstOrDefault()
				})
				.ToListAsync();

			var userId = CurrentUserId();
			var byId = rows.ToDictionary(r => r.Id);

			return ids.Where(byId.ContainsKey).Select(id =>
			{
				var r = byId[id];
				return new SampleCollectionSummaryDto
				{
					Id = r.Id,
					Code = r.Code,
					CollectionDate = r.CollectionDate,
					CollectedByName = r.CollectedByName,
					LocationName = r.LocationName,
					PaymentType = r.PaymentType,
					PaidCategory = r.PaidCategory,
					Status = r.Status,
					TotalAmount = r.TotalAmount,
					SampleCount = r.Items.Count,
					FarmerName = r.Items.Select(i => i.FarmerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "",
					SampleTypes = string.Join(", ", r.Items.Select(i => SampleTypeLabel(i.SampleType)).Distinct()),
					PaymentStatus = r.Payment?.Status,
					PaymentReviewedAt = r.Payment?.ReviewedAt,
					ConsignmentId = r.ConsignmentId,
					ConsignmentCode = r.ConsignmentCode,
					IsMine = string.Equals(r.CollectedByUserId, userId, StringComparison.OrdinalIgnoreCase)
				};
			}).ToList();
		}

		private async Task<SampleCollectionDetailDto> BuildDetailAsync(SampleCollection source)
		{
			var summary = (await BuildSummariesAsync(new List<int> { source.Id })).FirstOrDefault()
				?? new SampleCollectionSummaryDto { Id = source.Id, Code = source.Code };

			var collection = await _db.SampleCollections.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == source.Id) ?? source;

			var items = await _db.SampleItems.AsNoTracking()
				.Where(i => i.CollectionId == collection.Id && !i.IsDeleted)
				.OrderBy(i => i.Id)
				.Include(i => i.Farmer)
				.ToListAsync();

			var itemIds = items.Select(i => i.Id).ToList();
			var results = await _db.SampleLabResults.AsNoTracking()
				.Where(r => itemIds.Contains(r.SampleItemId))
				.OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
				.ToListAsync();

			var payment = await _db.SamplePayments.AsNoTracking()
				.Where(p => p.CollectionId == collection.Id)
				.OrderByDescending(p => p.Id)
				.FirstOrDefaultAsync();

			var timeline = await _db.SasStatusEvents.AsNoTracking()
				.Where(e => e.CollectionId == collection.Id)
				.OrderBy(e => e.At).ThenBy(e => e.Id)
				.Select(e => new SasStatusEventDto { Status = e.Status, Note = e.Note, ByName = e.ByName, At = e.At })
				.ToListAsync();

			ConsignmentSummaryDto? consignment = null;
			if (collection.ConsignmentId.HasValue)
			{
				consignment = (await BuildConsignmentSummariesAsync(new List<int> { collection.ConsignmentId.Value }))
					.FirstOrDefault();
			}

			var mine = summary.IsMine;
			var writer = IsWriter();

			var detail = new SampleCollectionDetailDto
			{
				Id = summary.Id,
				Code = summary.Code,
				CollectionDate = summary.CollectionDate,
				CollectedByName = summary.CollectedByName,
				LocationName = summary.LocationName,
				PaymentType = summary.PaymentType,
				PaidCategory = summary.PaidCategory,
				Status = summary.Status,
				TotalAmount = summary.TotalAmount,
				SampleCount = summary.SampleCount,
				FarmerName = summary.FarmerName,
				SampleTypes = summary.SampleTypes,
				PaymentStatus = summary.PaymentStatus,
				PaymentReviewedAt = summary.PaymentReviewedAt,
				ConsignmentId = summary.ConsignmentId,
				ConsignmentCode = summary.ConsignmentCode,
				IsMine = mine,
				Remarks = collection.Remarks,
				CollectedByRole = collection.CollectedByRole,
				SubmittedAt = collection.SubmittedAt,
				CompletedAt = collection.CompletedAt,
				Items = items.Select(i => MapItem(i, results)).ToList(),
				Payment = payment == null ? null : MapPayment(payment),
				Consignment = consignment,
				Timeline = timeline,
				CanEdit = mine && writer && collection.Status == SampleCollectionStatus.Draft,
				CanPay = mine && writer && collection.Status == SampleCollectionStatus.PendingPayment,
				CanConsign = mine && writer && collection.Status == SampleCollectionStatus.ReadyForConsignment,
				CanReview = IsReviewer()
			};

			return detail;
		}

		private async Task<List<ConsignmentSummaryDto>> BuildConsignmentSummariesAsync(List<int> ids)
		{
			if (ids.Count == 0) return new List<ConsignmentSummaryDto>();

			var consignments = await _db.SampleConsignments.AsNoTracking()
				.Where(c => ids.Contains(c.Id))
				.ToListAsync();

			var collections = await _db.SampleCollections.AsNoTracking()
				.Where(c => !c.IsDeleted && c.ConsignmentId != null && ids.Contains(c.ConsignmentId.Value))
				.Select(c => new
				{
					c.Id,
					c.Code,
					ConsignmentId = c.ConsignmentId!.Value,
					c.PaymentType,
					c.Status,
					c.TotalAmount,
					Items = c.Items.Where(i => !i.IsDeleted).Select(i => new { i.Code, i.SampleType, i.Amount, i.NoOfTests }).ToList(),
					Payments = c.Payments.OrderByDescending(p => p.Id)
						.Select(p => new { p.Status, p.ReviewedAt, p.TransactionId })
						.ToList()
				})
				.ToListAsync();

			var photoCounts = await _db.ConsignmentPhotos.AsNoTracking()
				.Where(p => ids.Contains(p.ConsignmentId))
				.GroupBy(p => p.ConsignmentId)
				.Select(g => new { ConsignmentId = g.Key, Count = g.Count() })
				.ToListAsync();

			var result = new List<ConsignmentSummaryDto>();

			foreach (var id in ids)
			{
				var consignment = consignments.FirstOrDefault(c => c.Id == id);
				if (consignment == null) continue;

				var bundled = collections.Where(c => c.ConsignmentId == id).ToList();
				var paid = bundled.Where(c => c.PaymentType == SamplePaymentType.Paid).ToList();

				SamplePaymentStatus? paymentStatus = null;
				DateTime? reviewedAt = null;

				if (paid.Count > 0)
				{
					var latest = paid.Select(c => c.Payments.FirstOrDefault()).ToList();
					if (latest.All(p => p != null && p.Status == SamplePaymentStatus.Approved))
						paymentStatus = SamplePaymentStatus.Approved;
					else if (latest.Any(p => p != null && p.Status == SamplePaymentStatus.Rejected))
						paymentStatus = SamplePaymentStatus.Rejected;
					else
						paymentStatus = SamplePaymentStatus.Pending;

					reviewedAt = latest.Where(p => p?.ReviewedAt != null).Select(p => p!.ReviewedAt).Max();
				}

				var transactionIds = bundled
					.SelectMany(c => c.Payments.Where(p => p.Status == SamplePaymentStatus.Approved))
					.Select(p => p.TransactionId)
					.Where(t => !string.IsNullOrWhiteSpace(t))
					.Distinct()
					.ToList();

				// One expandable line per sample type across the whole consignment.
				var lines = bundled
					.SelectMany(c => c.Items.Select(i => new { c.Status, i.Code, i.SampleType, i.Amount, i.NoOfTests }))
					.GroupBy(x => x.SampleType)
					.OrderBy(g => g.Key)
					.Select(g =>
					{
						var codes = g.Select(x => x.Code).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
						return new ConsignmentSampleLineDto
						{
							SampleType = g.Key,
							SampleCount = g.Count(),
							NoOfTests = g.Sum(x => x.NoOfTests),
							SampleCodeRange = codes.Count <= 1 ? (codes.FirstOrDefault() ?? "") : $"{codes.First()} - {codes.Last()}",
							Amount = g.Sum(x => x.Amount),
							Status = g.Select(x => x.Status).Min()
						};
					})
					.ToList();

				result.Add(new ConsignmentSummaryDto
				{
					Id = consignment.Id,
					Code = consignment.Code,
					CourierService = consignment.CourierService,
					TrackingNumber = consignment.TrackingNumber,
					TrackingLink = consignment.TrackingLink,
					DispatchedAt = consignment.DispatchedAt,
					DispatchedByName = consignment.DispatchedByName,
					Status = consignment.Status,
					CollectionCount = bundled.Count,
					SampleCount = bundled.Sum(c => c.Items.Count),
					TotalAmount = bundled.Sum(c => c.TotalAmount),
					PaymentStatus = paymentStatus,
					PaymentReviewedAt = reviewedAt,
					TransactionIds = transactionIds.Count == 0 ? null : string.Join(", ", transactionIds),
					PackageCount = consignment.PackageCount,
					SampleLines = lines
				});
			}

			return result;
		}

		private async Task<ConsignmentDetailDto> BuildConsignmentDetailAsync(SampleConsignment source)
		{
			var summary = (await BuildConsignmentSummariesAsync(new List<int> { source.Id })).FirstOrDefault()
				?? new ConsignmentSummaryDto { Id = source.Id, Code = source.Code };

			var consignment = await _db.SampleConsignments.AsNoTracking()
				.FirstOrDefaultAsync(c => c.Id == source.Id) ?? source;

			// A Farmer only sees their own collections inside the consignment; the header
			// figures (counts, sample lines) stay the consignment's real totals.
			var collectionIds = await (await ScopedCollectionsAsync())
				.Where(c => c.ConsignmentId == consignment.Id)
				.OrderBy(c => c.Id)
				.Select(c => c.Id)
				.ToListAsync();

			var items = await _db.SampleItems.AsNoTracking()
				.Where(i => collectionIds.Contains(i.CollectionId) && !i.IsDeleted)
				.OrderBy(i => i.Id)
				.Include(i => i.Farmer)
				.ToListAsync();

			var itemIds = items.Select(i => i.Id).ToList();
			var results = await _db.SampleLabResults.AsNoTracking()
				.Where(r => itemIds.Contains(r.SampleItemId))
				.OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
				.ToListAsync();

			var photos = await _db.ConsignmentPhotos.AsNoTracking()
				.Where(p => p.ConsignmentId == consignment.Id)
				.OrderBy(p => p.Kind).ThenBy(p => p.Id)
				.ToListAsync();

			var timeline = await _db.SasStatusEvents.AsNoTracking()
				.Where(e => e.ConsignmentId == consignment.Id)
				.OrderBy(e => e.At).ThenBy(e => e.Id)
				.Select(e => new SasStatusEventDto { Status = e.Status, Note = e.Note, ByName = e.ByName, At = e.At })
				.ToListAsync();

			return new ConsignmentDetailDto
			{
				Id = summary.Id,
				Code = summary.Code,
				CourierService = summary.CourierService,
				TrackingNumber = summary.TrackingNumber,
				TrackingLink = summary.TrackingLink,
				DispatchedAt = summary.DispatchedAt,
				DispatchedByName = summary.DispatchedByName,
				Status = summary.Status,
				CollectionCount = summary.CollectionCount,
				SampleCount = summary.SampleCount,
				TotalAmount = summary.TotalAmount,
				PaymentStatus = summary.PaymentStatus,
				PaymentReviewedAt = summary.PaymentReviewedAt,
				TransactionIds = summary.TransactionIds,
				PackageCount = summary.PackageCount,
				SampleLines = summary.SampleLines,
				ExpectedDeliveryDate = consignment.ExpectedDeliveryDate,
				PackageWeightKg = consignment.PackageWeightKg,
				PackagingType = consignment.PackagingType,
				ContactPerson = consignment.ContactPerson,
				ContactMobile = consignment.ContactMobile,
				ContactEmail = consignment.ContactEmail,
				Notes = consignment.Notes,
				DeliveredAt = consignment.DeliveredAt,
				Photos = photos.Select(MapPhoto).ToList(),
				Collections = await BuildSummariesAsync(collectionIds),
				Samples = items.Select(i => MapItem(i, results)).ToList(),
				Timeline = timeline,
				CanReview = IsReviewer()
			};
		}

		private static SampleItemDto MapItem(SampleItem item, List<SampleLabResult> allResults)
		{
			var results = allResults.Where(r => r.SampleItemId == item.Id).Select(MapResult).ToList();

			return new SampleItemDto
			{
				Id = item.Id,
				CollectionId = item.CollectionId,
				Code = item.Code,
				Farmer = item.Farmer == null ? new SasFarmerDto() : MapFarmer(item.Farmer),
				SampleType = item.SampleType,
				Crop1 = item.Crop1,
				Crop2 = item.Crop2,
				Remarks = item.Remarks,
				NoOfTests = item.NoOfTests,
				Amount = item.Amount,
				HasResults = results.Count > 0,
				Results = results
			};
		}

		private static SasFarmerDto MapFarmer(SasFarmer farmer) => new()
		{
			Id = farmer.Id,
			Name = farmer.Name,
			Mobile = farmer.Mobile,
			Address1 = farmer.Address1,
			Address2 = farmer.Address2,
			Village = farmer.Village,
			Taluk = farmer.Taluk,
			DistrictId = farmer.DistrictId,
			DistrictName = farmer.DistrictName,
			StateId = farmer.StateId,
			StateName = farmer.StateName,
			PinCode = farmer.PinCode,
			Latitude = farmer.Latitude,
			Longitude = farmer.Longitude,
			SurveyNumber = farmer.SurveyNumber,
			UserId = farmer.UserId
		};

		private static SamplePaymentDto MapPayment(SamplePayment payment) => new()
		{
			Id = payment.Id,
			CollectionId = payment.CollectionId,
			TransactionId = payment.TransactionId,
			BankGateway = payment.BankGateway,
			UtrNumber = payment.UtrNumber,
			PaidByName = payment.PaidByName,
			ContactNumber = payment.ContactNumber,
			Email = payment.Email,
			ProofPath = payment.ProofPath,
			Amount = payment.Amount,
			Status = payment.Status,
			ReviewedByName = payment.ReviewedByName,
			ReviewedAt = payment.ReviewedAt,
			RejectReason = payment.RejectReason,
			CreatedAt = payment.CreatedAt
		};

		private static ConsignmentPhotoDto MapPhoto(ConsignmentPhoto photo) => new()
		{
			Id = photo.Id,
			Kind = photo.Kind,
			Title = photo.Title,
			FileName = photo.FileName,
			Path = photo.StoredPath,
			ContentType = photo.ContentType,
			Size = photo.Size,
			UploadedByName = photo.UploadedByName,
			CreatedAt = photo.CreatedAt
		};

		private static LabResultDto MapResult(SampleLabResult result) => new()
		{
			Id = result.Id,
			Parameter = result.Parameter,
			NormalRange = result.NormalRange,
			EnteredValue = result.EnteredValue,
			Unit = result.Unit,
			ResultLabel = result.ResultLabel,
			Status = result.Status,
			Hint = result.Hint,
			SortOrder = result.SortOrder,
			EnteredByName = result.EnteredByName,
			EnteredAt = result.EnteredAt
		};

		private static string SampleTypeLabel(SampleType type) => type switch
		{
			SampleType.Soil => "Soil",
			SampleType.Water => "Water",
			_ => "Soil & Water"
		};

		// ---------------------------------------------------------------- identity

		private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

		private AppRole? CurrentRole()
		{
			var raw = User.FindFirst(ClaimTypes.Role)?.Value;
			return Enum.TryParse<AppRole>(raw, true, out var role) ? role : null;
		}

		private bool IsWriter()
		{
			var role = CurrentRole();
			return role.HasValue && WriteRoles.Contains(role.Value);
		}

		private bool IsReviewer()
		{
			var role = CurrentRole();
			return role == AppRole.Admin || role == AppRole.CorporateAdmin;
		}

		private bool IsMine(SampleCollection collection) =>
			string.Equals(collection.CollectedByUserId, CurrentUserId(), StringComparison.OrdinalIgnoreCase);

		private ObjectResult Forbid403(string message) =>
			StatusCode(403, new { Success = false, Message = message });

		private async Task<string> ResolveDisplayNameAsync()
		{
			var userId = CurrentUserId();

			var profile = await _db.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => new { u.Name, u.UserName })
				.FirstOrDefaultAsync();

			var name = profile?.Name;
			if (string.IsNullOrWhiteSpace(name)) name = profile?.UserName;
			if (string.IsNullOrWhiteSpace(name)) name = User.Identity?.Name;

			return name ?? "";
		}

		// "Headquarter, State" from the JWT's location claims (same shape as CommunityController).
		private async Task<string?> ResolveLocationAsync()
		{
			var hqId = ClaimInt("spic:hq_id");
			var stateId = ClaimInt("spic:state_id");

			string? hqName = null;
			string? stateName = null;

			if (hqId > 0)
			{
				hqName = await _db.Headquarters.AsNoTracking()
					.Where(h => h.Id == hqId)
					.Select(h => h.HeadquarterName)
					.FirstOrDefaultAsync();
			}

			if (stateId > 0)
			{
				stateName = await _db.States.AsNoTracking()
					.Where(s => s.Id == stateId)
					.Select(s => s.StateName)
					.FirstOrDefaultAsync();
			}

			var parts = new[] { hqName, stateName }
				.Where(p => !string.IsNullOrWhiteSpace(p))
				.ToList();

			return parts.Count == 0 ? null : string.Join(", ", parts);
		}

		private async Task<(string? District, string? State)> ResolveLocationNamesAsync(int? districtId, int? stateId)
		{
			string? districtName = null;
			string? stateName = null;

			if (districtId.HasValue && districtId.Value > 0)
			{
				districtName = await _db.Districts.AsNoTracking()
					.Where(d => d.Id == districtId.Value)
					.Select(d => d.DistrictName)
					.FirstOrDefaultAsync();
			}

			if (stateId.HasValue && stateId.Value > 0)
			{
				stateName = await _db.States.AsNoTracking()
					.Where(s => s.Id == stateId.Value)
					.Select(s => s.StateName)
					.FirstOrDefaultAsync();
			}

			return (districtName, stateName);
		}

		private int ClaimInt(string type)
		{
			var raw = User.FindFirst(type)?.Value;
			return int.TryParse(raw, out var value) ? value : 0;
		}

		// ---------------------------------------------------------------- small helpers

		private string GetUploadsRoot() => Path.Combine(_env.ContentRootPath, "Uploads");

		private static string? Clean(string? value) =>
			string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private static bool TryParseEnum<TEnum>(string? raw, out TEnum value) where TEnum : struct, Enum
		{
			value = default;
			if (string.IsNullOrWhiteSpace(raw)) return false;

			// Accepts both the name ("Approved") and the integer the client may send.
			if (Enum.TryParse(raw.Trim(), true, out value) && Enum.IsDefined(typeof(TEnum), value))
				return true;

			value = default;
			return false;
		}

		private static string? ValidateImage(IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return "No file uploaded.";
			if (file.Length > MaxImageBytes)
				return $"\"{file.FileName}\" is larger than 5 MB.";

			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!AllowedImageExtensions.Contains(ext))
				return "Only JPG, PNG and WEBP images are allowed.";

			return null;
		}

		private static string MakeSafeFileName(string fileName)
		{
			var name = Path.GetFileName(fileName);
			var invalid = Path.GetInvalidFileNameChars();
			var sb = new StringBuilder();

			foreach (var ch in name)
				sb.Append(invalid.Contains(ch) || ch == ' ' ? '_' : ch);

			var safe = sb.ToString();
			return safe.Length > 80 ? safe[^80..] : safe;
		}

		private static string ResolveContentType(string extension) => extension switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".webp" => "image/webp",
			".pdf" => "application/pdf",
			_ => "application/octet-stream"
		};
	}
}
