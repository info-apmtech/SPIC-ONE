using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Globalization;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
	/// <summary>
	/// SAS Payment Approval (admin), Payment Verification / History (Finance) and the farmer's own
	/// payments (docs/sas-lab-portal-plan.md, screens 23-30 and 34-35). Contract and route list:
	/// SPIC.Core/DTOs/SasPaymentDtos.cs.
	///
	/// Mode (resolved per request, like LibraryController):
	///   Farmer   : role Farmer -> payments of collections carrying a farmer whose SasFarmer.UserId is theirs
	///   Admin    : Admin / CorporateAdmin, or a designation granting SasPaymentApproval -> everything
	///   Finance  : a designation granting SasPaymentVerification -> payments forwarded to Finance
	///   ReadOnly : v1 write roles (MDO / JMDO) -> the payments they submitted
	///   anyone else -> 403
	///
	/// Approve keeps the v1 cascade (collection -> ReadyForConsignment) so the field flow is
	/// unchanged; Finance verification is a back-office confirmation on top of it.
	/// </summary>
	[Authorize]
	[ApiController]
	[Route("api/Sas/payments")]
	public class SasPaymentsController : ControllerBase
	{
		private readonly AppDbContext _db;
		private readonly IWebHostEnvironment _env;

		public SasPaymentsController(AppDbContext db, IWebHostEnvironment env)
		{
			_db = db;
			_env = env;
		}

		private const int DefaultPageSize = 16;
		private const int MaxPageSize = 50;
		private const int CodeBackfillBatch = 500;

		private static readonly AppRole[] FieldWriteRoles = { AppRole.MDO, AppRole.JMDO };

		// ---------------------------------------------------------------- me / stats

		// GET api/Sas/payments/me
		[HttpGet("me")]
		public async Task<IActionResult> GetMe()
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed) return Forbid403();

			return Ok(new SasPaymentModeDto
			{
				Mode = access.Mode,
				CanApprove = access.CanApprove,
				CanVerify = access.CanVerify
			});
		}

		// GET api/Sas/payments/stats
		[HttpGet("stats")]
		public async Task<IActionResult> GetStats()
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed) return Forbid403();

			var rows = await Scoped(access)
				.Select(p => new
				{
					p.Status,
					p.FinanceStatus,
					Short = p.VerifiedAmount != null && p.VerifiedAmount < p.Amount
				})
				.ToListAsync();

			var mismatchAll = rows.Count(r => r.FinanceStatus == SampleFinanceStatus.Mismatch);
			var mismatchShort = rows.Count(r => r.FinanceStatus == SampleFinanceStatus.Mismatch && r.Short);

			return Ok(new SasPaymentStatsDto
			{
				PendingAdminApproval = rows.Count(r => r.Status == SamplePaymentStatus.Pending),
				ForwardedToFinance = rows.Count(r => r.Status == SamplePaymentStatus.Approved &&
													 r.FinanceStatus == SampleFinanceStatus.AwaitingVerification),
				ReturnedOrRejected = rows.Count(r => r.Status == SamplePaymentStatus.Rejected),
				FinanceVerified = rows.Count(r => r.FinanceStatus == SampleFinanceStatus.Verified),

				AllPayments = rows.Count,
				PendingVerification = rows.Count(r => r.FinanceStatus == SampleFinanceStatus.AwaitingVerification),
				VerifiedPayments = rows.Count(r => r.FinanceStatus == SampleFinanceStatus.Verified),
				FailedOrMismatch = mismatchAll,
				Mismatch = mismatchShort,
				Failed = mismatchAll - mismatchShort,

				TotalPaidSamples = rows.Count,
				ApprovalPending = rows.Count(r => r.Status == SamplePaymentStatus.Pending ||
												  r.FinanceStatus == SampleFinanceStatus.AwaitingVerification),
				PaymentIssues = rows.Count(r => r.Status == SamplePaymentStatus.Rejected ||
												r.FinanceStatus == SampleFinanceStatus.Mismatch)
			});
		}

		// ---------------------------------------------------------------- list

		// GET api/Sas/payments?tab=&stateId=&regionId=&mode=&adminStatus=&financeStatus=&crop=&q=&from=&to=&page=&pageSize=
		[HttpGet]
		public async Task<IActionResult> GetPayments(
			[FromQuery] string? tab,
			[FromQuery] int? stateId,
			[FromQuery] int? regionId,
			[FromQuery] string? mode,
			[FromQuery] string? adminStatus,
			[FromQuery] string? financeStatus,
			[FromQuery] string? crop,
			[FromQuery] string? q,
			[FromQuery] DateTime? from,
			[FromQuery] DateTime? to,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = DefaultPageSize)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed) return Forbid403();

			if (page < 1) page = 1;
			if (pageSize < 1) pageSize = DefaultPageSize;
			if (pageSize > MaxPageSize) pageSize = MaxPageSize;

			// v1 payments were created without a reference; give them one before searching on it.
			await BackfillCodesAsync();

			var query = ApplyTab(Scoped(access), tab);

			if (TryParseEnum<SamplePaymentMode>(mode, out var modeValue))
				query = query.Where(p => p.PaymentMode == modeValue);

			if (TryParseEnum<SamplePaymentStatus>(adminStatus, out var adminValue))
				query = query.Where(p => p.Status == adminValue);

			if (TryParseEnum<SampleFinanceStatus>(financeStatus, out var financeValue))
				query = query.Where(p => p.FinanceStatus == financeValue);

			if (regionId is > 0)
			{
				var rid = regionId.Value;
				var hqIds = await _db.Headquarters.AsNoTracking().Where(h => h.RegionId == rid).Select(h => h.Id).ToListAsync();
				query = query.Where(p => p.Collection!.HeadquarterId != null && hqIds.Contains(p.Collection.HeadquarterId.Value));
			}
			else if (stateId is > 0)
			{
				var sid = stateId.Value;
				var hqIds = await _db.Headquarters.AsNoTracking()
					.Where(h => h.Region != null && h.Region.StateId == sid)
					.Select(h => h.Id)
					.ToListAsync();
				query = query.Where(p =>
					(p.Collection!.HeadquarterId != null && hqIds.Contains(p.Collection.HeadquarterId.Value)) ||
					p.Collection.Items.Any(i => !i.IsDeleted && i.Farmer != null && i.Farmer.StateId == sid));
			}

			if (!string.IsNullOrWhiteSpace(crop))
			{
				var c = crop.Trim().ToLower();
				query = query.Where(p => p.Collection!.Items.Any(i => !i.IsDeleted &&
					((i.Crop1 != null && i.Crop1.ToLower() == c) || (i.Crop2 != null && i.Crop2.ToLower() == c))));
			}

			if (from.HasValue)
				query = query.Where(p => p.CreatedAt >= from.Value.Date);
			if (to.HasValue)
			{
				var end = to.Value.Date.AddDays(1);
				query = query.Where(p => p.CreatedAt < end);
			}

			if (!string.IsNullOrWhiteSpace(q))
			{
				var term = q.Trim().ToLower();
				query = query.Where(p =>
					(p.Code != null && p.Code.ToLower().Contains(term)) ||
					p.TransactionId.ToLower().Contains(term) ||
					p.Collection!.Code.ToLower().Contains(term) ||
					p.Collection.CollectedByName.ToLower().Contains(term) ||
					(p.Collection.Consignment != null && p.Collection.Consignment.Code.ToLower().Contains(term)) ||
					p.Collection.Items.Any(i => !i.IsDeleted && i.Farmer != null &&
						(i.Farmer.Name.ToLower().Contains(term) || i.Farmer.Mobile.Contains(term))));
			}

			var total = await query.CountAsync();

			var ids = await query
				.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id)
				.Skip((page - 1) * pageSize).Take(pageSize)
				.Select(p => p.Id)
				.ToListAsync();

			return Ok(new PageResult<SasPaymentRowDto>
			{
				Items = await BuildRowsAsync(ids),
				Total = total,
				Page = page,
				PageSize = pageSize
			});
		}

		// ---------------------------------------------------------------- detail

		// GET api/Sas/payments/{id}
		[HttpGet("{id:int}")]
		public async Task<IActionResult> GetPayment(int id)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed) return Forbid403();

			if (!await Scoped(access).AnyAsync(p => p.Id == id))
				return NotFound(new { Success = false, Message = "Payment not found." });

			await EnsureCodeAsync(id);
			return Ok(await BuildDetailAsync(id, access));
		}

		// GET api/Sas/payments/{id}/proof   (also ?access_token=, see Program.cs)
		[HttpGet("{id:int}/proof")]
		public async Task<IActionResult> GetProof(int id)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed) return Forbid403();

			var path = await Scoped(access).Where(p => p.Id == id).Select(p => p.ProofPath).FirstOrDefaultAsync();
			var fullPath = ResolveUpload(path);

			if (fullPath == null)
				return NotFound(new { Success = false, Message = "No payment proof was uploaded for this payment." });

			return PhysicalFile(fullPath, ContentTypeFor(fullPath));
		}

		// ---------------------------------------------------------------- admin

		// POST api/Sas/payments/{id}/approve
		[HttpPost("{id:int}/approve")]
		public async Task<IActionResult> Approve(int id, [FromBody] SasPaymentApproveDto dto)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed || !access.CanApprove)
				return Forbid403("Only Payment Approval users can approve payments.");

			if (dto == null || !dto.Confirmed)
				return BadRequest(new { Success = false, Message = "Confirm that the payment proof, transaction details and amount are verified." });
			if (dto.VerifiedAmount <= 0)
				return BadRequest(new { Success = false, Message = "Enter the verified amount." });

			var payment = await _db.SamplePayments.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
				return NotFound(new { Success = false, Message = "Payment not found." });
			if (payment.Status != SamplePaymentStatus.Pending)
				return BadRequest(new { Success = false, Message = "This payment has already been reviewed." });

			var collection = await _db.SampleCollections.FirstOrDefaultAsync(c => c.Id == payment.CollectionId && !c.IsDeleted);
			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			var name = await ResolveDisplayNameAsync();
			var now = DateTime.Now;

			payment.Status = SamplePaymentStatus.Approved;
			payment.ReviewedByName = name;
			payment.ReviewedAt = now;
			payment.RejectReason = null;
			payment.VerifiedAmount = dto.VerifiedAmount;
			payment.ApprovedDate = (dto.ApprovedDate ?? now).Date;
			payment.AdminRemarks = Clean(dto.Remarks);
			payment.ForwardedToName = Clean(dto.ForwardTo) ?? "Finance Team";
			payment.ForwardedAt = now;
			payment.FinanceStatus = SampleFinanceStatus.AwaitingVerification;

			// v1 cascade, exactly as PATCH api/Sas/payments/{id}/status does it.
			collection.Status = SampleCollectionStatus.ReadyForConsignment;
			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = now;
			AddEvent(collection.Id, nameof(SampleCollectionStatus.ReadyForConsignment),
				$"Payment {payment.TransactionId} approved and forwarded to {payment.ForwardedToName}.", name);

			await _db.SaveChangesAsync();
			await EnsureCodeAsync(id);

			return Ok(await BuildDetailAsync(id, access));
		}

		// POST api/Sas/payments/{id}/reject
		[HttpPost("{id:int}/reject")]
		public async Task<IActionResult> Reject(int id, [FromBody] SasPaymentRejectDto dto)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed || !access.CanApprove)
				return Forbid403("Only Payment Approval users can reject payments.");

			var reason = Clean(dto?.Reason);
			if (reason == null)
				return BadRequest(new { Success = false, Message = "A rejection needs a reason." });

			var payment = await _db.SamplePayments.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
				return NotFound(new { Success = false, Message = "Payment not found." });
			if (payment.Status != SamplePaymentStatus.Pending)
				return BadRequest(new { Success = false, Message = "This payment has already been reviewed." });

			var collection = await _db.SampleCollections.FirstOrDefaultAsync(c => c.Id == payment.CollectionId && !c.IsDeleted);
			if (collection == null)
				return NotFound(new { Success = false, Message = "Sample collection not found." });

			var name = await ResolveDisplayNameAsync();
			var now = DateTime.Now;

			payment.Status = SamplePaymentStatus.Rejected;
			payment.ReviewedByName = name;
			payment.ReviewedAt = now;
			payment.RejectReason = reason;
			payment.AdminRemarks = reason;
			payment.FinanceStatus = SampleFinanceStatus.NotForwarded;

			collection.Status = SampleCollectionStatus.PendingPayment;
			collection.UpdatedBy = User.Identity?.Name;
			collection.UpdatedAt = now;
			AddEvent(collection.Id, nameof(SampleCollectionStatus.PendingPayment),
				$"Payment {payment.TransactionId} rejected: {reason}", name);

			await _db.SaveChangesAsync();
			await EnsureCodeAsync(id);

			return Ok(await BuildDetailAsync(id, access));
		}

		// ---------------------------------------------------------------- finance

		// POST api/Sas/payments/{id}/verify
		[HttpPost("{id:int}/verify")]
		public async Task<IActionResult> Verify(int id, [FromBody] SasPaymentVerifyDto dto)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed || !access.CanVerify)
				return Forbid403("Only Payment Verification users can verify payments.");

			if (dto == null || !dto.Confirmed)
				return BadRequest(new { Success = false, Message = "Confirm that you have verified the payment proof, transaction id and amount." });
			if (dto.VerifiedAmount < 0)
				return BadRequest(new { Success = false, Message = "The verified amount cannot be negative." });

			var payment = await _db.SamplePayments.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
				return NotFound(new { Success = false, Message = "Payment not found." });
			if (payment.FinanceStatus != SampleFinanceStatus.AwaitingVerification)
				return BadRequest(new { Success = false, Message = "This payment is not awaiting Finance verification." });

			var name = await ResolveDisplayNameAsync();
			var now = DateTime.Now;
			var remarks = Clean(dto.Remarks);

			payment.VerifiedAmount = dto.VerifiedAmount;
			if (dto.VerifiedAmount < payment.Amount)
			{
				payment.FinanceStatus = SampleFinanceStatus.Mismatch;
				payment.FinanceRemarks = remarks ?? $"₹{(payment.Amount - dto.VerifiedAmount).ToString("0.##", CultureInfo.InvariantCulture)} short";
			}
			else
			{
				payment.FinanceStatus = SampleFinanceStatus.Verified;
				payment.FinanceRemarks = remarks;
			}

			// The entity has no separate "received" column: the verification stamp carries the
			// received date Finance entered (with the time it was confirmed).
			payment.FinanceVerifiedAt = dto.ReceivedDate.HasValue ? dto.ReceivedDate.Value.Date + now.TimeOfDay : now;
			payment.FinanceVerifiedByName = name;

			await _db.SaveChangesAsync();
			await EnsureCodeAsync(id);

			return Ok(await BuildDetailAsync(id, access));
		}

		// POST api/Sas/payments/{id}/mismatch
		[HttpPost("{id:int}/mismatch")]
		public async Task<IActionResult> MarkMismatch(int id, [FromBody] SasPaymentRejectDto dto)
		{
			var access = await ResolveAccessAsync();
			if (!access.Allowed || !access.CanVerify)
				return Forbid403("Only Payment Verification users can mark a mismatch.");

			var reason = Clean(dto?.Reason);
			if (reason == null)
				return BadRequest(new { Success = false, Message = "Enter the reason for the mismatch." });

			var payment = await _db.SamplePayments.FirstOrDefaultAsync(p => p.Id == id);
			if (payment == null)
				return NotFound(new { Success = false, Message = "Payment not found." });
			if (payment.FinanceStatus != SampleFinanceStatus.AwaitingVerification)
				return BadRequest(new { Success = false, Message = "This payment is not awaiting Finance verification." });

			payment.FinanceStatus = SampleFinanceStatus.Mismatch;
			payment.FinanceRemarks = reason;
			payment.FinanceVerifiedAt = DateTime.Now;
			payment.FinanceVerifiedByName = await ResolveDisplayNameAsync();

			await _db.SaveChangesAsync();
			await EnsureCodeAsync(id);

			return Ok(await BuildDetailAsync(id, access));
		}

		// ---------------------------------------------------------------- access

		private sealed record Access(SasPaymentPageMode Mode, bool CanApprove, bool CanVerify, string UserId, bool Allowed);

		private Access? _access;

		private async Task<Access> ResolveAccessAsync()
		{
			if (_access != null) return _access;

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
			var role = CurrentRole();

			if (role == AppRole.Farmer)
				return _access = new Access(SasPaymentPageMode.Farmer, false, false, userId, userId != "");

			var roleAccess = await CurrentRoleAccessAsync(userId);
			var approval = role is AppRole.Admin or AppRole.CorporateAdmin ||
						   RoleAccessPermissions.HasPage(roleAccess, PagePermission.SasPaymentApproval);
			var verify = RoleAccessPermissions.HasPage(roleAccess, PagePermission.SasPaymentVerification);

			if (approval)
				return _access = new Access(SasPaymentPageMode.Admin, true, verify, userId, true);
			if (verify)
				return _access = new Access(SasPaymentPageMode.Finance, false, true, userId, true);

			var fieldWriter = role.HasValue && FieldWriteRoles.Contains(role.Value);
			return _access = new Access(SasPaymentPageMode.ReadOnly, false, false, userId, fieldWriter && userId != "");
		}

		private async Task<string?> CurrentRoleAccessAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId)) return null;

			var designationId = await _db.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => u.DesignationId)
				.FirstOrDefaultAsync();

			if (!designationId.HasValue || designationId.Value <= 0) return null;

			return await _db.Designations.AsNoTracking()
				.Where(d => d.Id == designationId.Value && d.IsActive)
				.Select(d => d.RoleAccess)
				.FirstOrDefaultAsync();
		}

		/// <summary>The payments the caller may see (not paged, not filtered).</summary>
		private IQueryable<SamplePayment> Scoped(Access access)
		{
			var query = _db.SamplePayments.AsNoTracking()
				.Where(p => p.Collection != null && !p.Collection.IsDeleted);

			var userId = access.UserId;

			return access.Mode switch
			{
				SasPaymentPageMode.Admin => query,
				SasPaymentPageMode.Finance => query.Where(p => p.FinanceStatus != SampleFinanceStatus.NotForwarded),
				SasPaymentPageMode.Farmer => query.Where(p => p.Collection!.Items.Any(i =>
					!i.IsDeleted && i.Farmer != null && !i.Farmer.IsDeleted && i.Farmer.UserId == userId)),
				_ => query.Where(p => p.Collection!.CollectedByUserId == userId)
			};
		}

		private static IQueryable<SamplePayment> ApplyTab(IQueryable<SamplePayment> query, string? tab)
		{
			switch ((tab ?? "all").Trim().ToLowerInvariant())
			{
				case "pendingadmin":
					return query.Where(p => p.Status == SamplePaymentStatus.Pending);
				case "forwarded":
					return query.Where(p => p.Status == SamplePaymentStatus.Approved &&
											p.FinanceStatus == SampleFinanceStatus.AwaitingVerification);
				case "returned":
					return query.Where(p => p.Status == SamplePaymentStatus.Rejected);
				case "financestatus":
				case "history":
					return query.Where(p => p.FinanceStatus == SampleFinanceStatus.Verified ||
											p.FinanceStatus == SampleFinanceStatus.Mismatch);
				case "pendingverification":
					return query.Where(p => p.FinanceStatus == SampleFinanceStatus.AwaitingVerification);
				case "verified":
					return query.Where(p => p.FinanceStatus == SampleFinanceStatus.Verified);
				case "failed":
					// Finance list: every payment Finance could not confirm (failed or short).
					return query.Where(p => p.FinanceStatus == SampleFinanceStatus.Mismatch);
				case "failedonly":
					// History "Failed": marked as a mismatch without a short amount.
					return query.Where(p => p.FinanceStatus == SampleFinanceStatus.Mismatch &&
											!(p.VerifiedAmount != null && p.VerifiedAmount < p.Amount));
				case "mismatch":
					return query.Where(p => p.FinanceStatus == SampleFinanceStatus.Mismatch &&
											p.VerifiedAmount != null && p.VerifiedAmount < p.Amount);
				case "issues":
					return query.Where(p => p.Status == SamplePaymentStatus.Rejected ||
											p.FinanceStatus == SampleFinanceStatus.Mismatch);
				case "approvalpending":
					return query.Where(p => p.Status == SamplePaymentStatus.Pending ||
											p.FinanceStatus == SampleFinanceStatus.AwaitingVerification);
				default:
					return query;
			}
		}

		// ---------------------------------------------------------------- projections

		private async Task<List<SasPaymentRowDto>> BuildRowsAsync(List<int> ids)
		{
			if (ids.Count == 0) return new List<SasPaymentRowDto>();

			var rows = await _db.SamplePayments.AsNoTracking()
				.Where(p => ids.Contains(p.Id))
				.Select(p => new
				{
					p.Id,
					p.Code,
					p.CollectionId,
					CollectionCode = p.Collection!.Code,
					p.Collection.ConsignmentId,
					ConsignmentCode = p.Collection.Consignment != null ? p.Collection.Consignment.Code : null,
					p.Collection.CollectedByName,
					p.Collection.CollectedByRole,
					p.Collection.HeadquarterId,
					p.Collection.TotalAmount,
					Items = p.Collection.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Id)
						.Select(i => new
						{
							FarmerName = i.Farmer != null ? i.Farmer.Name : "",
							FarmerState = i.Farmer != null ? i.Farmer.StateName : null,
							i.Crop1,
							i.Crop2
						})
						.ToList(),
					p.PaymentMode,
					p.TransactionId,
					p.Amount,
					p.Status,
					p.FinanceStatus,
					p.CreatedAt,
					p.FinanceVerifiedByName,
					p.FinanceVerifiedAt,
					p.FinanceRemarks
				})
				.ToListAsync();

			var locations = await LocationsAsync(rows.Select(r => r.HeadquarterId));
			var byId = rows.ToDictionary(r => r.Id);

			return ids.Where(byId.ContainsKey).Select(id =>
			{
				var r = byId[id];
				locations.TryGetValue(r.HeadquarterId ?? 0, out var loc);

				return new SasPaymentRowDto
				{
					Id = r.Id,
					Code = r.Code ?? "",
					CollectionId = r.CollectionId,
					CollectionCode = r.CollectionCode,
					ConsignmentId = r.ConsignmentId,
					ConsignmentCode = r.ConsignmentCode,
					SubmittedByName = r.CollectedByName,
					SubmittedByRole = r.CollectedByRole,
					FarmerName = r.Items.Select(i => i.FarmerName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "",
					Crop = CropOf(r.Items.Select(i => (i.Crop1, i.Crop2))),
					PaymentMode = r.PaymentMode,
					TransactionId = r.TransactionId,
					ExpectedAmount = r.TotalAmount,
					PaidAmount = r.Amount,
					AdminStatus = r.Status,
					FinanceStatus = r.FinanceStatus,
					SubmittedAt = r.CreatedAt,
					FinanceVerifiedByName = r.FinanceVerifiedByName,
					FinanceVerifiedAt = r.FinanceVerifiedAt,
					FinanceRemarks = r.FinanceRemarks,
					StateName = loc?.State ?? r.Items.Select(i => i.FarmerState).FirstOrDefault(s => !string.IsNullOrWhiteSpace(s)),
					RegionName = loc?.Region
				};
			}).ToList();
		}

		private async Task<SasPaymentDetailDto> BuildDetailAsync(int id, Access access)
		{
			var summary = (await BuildRowsAsync(new List<int> { id })).First();

			var payment = await _db.SamplePayments.AsNoTracking().FirstAsync(p => p.Id == id);
			var collection = await _db.SampleCollections.AsNoTracking()
				.Include(c => c.Consignment)
				.FirstAsync(c => c.Id == payment.CollectionId);

			var items = await _db.SampleItems.AsNoTracking()
				.Where(i => i.CollectionId == collection.Id && !i.IsDeleted)
				.Include(i => i.Farmer)
				.OrderBy(i => i.Id)
				.ToListAsync();

			// The farmer card shows the caller's own farmer when a farmer is reading.
			var farmer = access.Mode == SasPaymentPageMode.Farmer
				? items.Select(i => i.Farmer).FirstOrDefault(f => f != null && f.UserId == access.UserId)
				: null;
			farmer ??= items.Select(i => i.Farmer).FirstOrDefault(f => f != null);

			string? hqName = null;
			if (collection.HeadquarterId is > 0)
			{
				hqName = await _db.Headquarters.AsNoTracking()
					.Where(h => h.Id == collection.HeadquarterId.Value)
					.Select(h => h.HeadquarterName)
					.FirstOrDefaultAsync();
			}

			var amounts = items.Select(i => i.Amount).Distinct().ToList();
			var rate = items.Count == 0 ? 0m
				: amounts.Count == 1 ? amounts[0]
				: Math.Round(collection.TotalAmount / items.Count, 2);

			if (farmer != null)
				summary.FarmerName = farmer.Name;

			var proofPath = ResolveUpload(payment.ProofPath);

			return new SasPaymentDetailDto
			{
				Summary = summary,
				FarmerMobile = farmer?.Mobile,
				FarmerDistrict = farmer?.DistrictName,
				FarmerVillage = farmer?.Village,
				FarmerAddress = farmer == null ? null : JoinNonEmpty(", ",
					farmer.Address1, farmer.Address2, farmer.Village, farmer.Taluk, farmer.DistrictName, farmer.StateName, farmer.PinCode),
				HeadquarterName = hqName,

				TotalSamples = items.Count,
				SoilSamples = items.Count(i => i.SampleType is SampleType.Soil or SampleType.SoilAndWater),
				WaterSamples = items.Count(i => i.SampleType is SampleType.Water or SampleType.SoilAndWater),
				RatePerSample = rate,
				CollectedByName = collection.CollectedByName,
				CollectedByRole = collection.CollectedByRole,
				CollectionDate = collection.CollectionDate,
				ConsignmentCreatedAt = collection.Consignment?.CreatedAt,
				TrackingNumber = collection.Consignment?.TrackingNumber,

				TransactionDate = payment.TransactionDate,
				BankName = payment.BankName ?? payment.BankGateway,
				UtrNumber = payment.UtrNumber,
				MoRemarks = payment.MoRemarks,
				ProofUrl = proofPath == null ? null : $"api/Sas/payments/{id}/proof",
				ProofContentType = proofPath == null ? null : ContentTypeFor(proofPath),

				ReviewedByName = payment.ReviewedByName,
				ReviewedAt = payment.ReviewedAt,
				AdminRemarks = payment.AdminRemarks,
				RejectReason = payment.RejectReason,
				VerifiedAmount = payment.VerifiedAmount,
				ApprovedDate = payment.ApprovedDate,
				ForwardedToName = payment.ForwardedToName,
				ForwardedAt = payment.ForwardedAt,

				FinanceReceivedDate = payment.FinanceStatus is SampleFinanceStatus.Verified or SampleFinanceStatus.Mismatch
					? payment.FinanceVerifiedAt?.Date
					: null,
				Timeline = access.Mode == SasPaymentPageMode.Farmer
					? FarmerTimeline(payment, collection.CollectedByName)
					: StaffTimeline(payment, collection.CollectedByName, collection.CollectedByRole),
				Mode = access.Mode,
				CanApprove = access.CanApprove && payment.Status == SamplePaymentStatus.Pending,
				CanVerify = access.CanVerify && payment.FinanceStatus == SampleFinanceStatus.AwaitingVerification
			};
		}

		/// <summary>Admin / Finance wording (screens 24 and 28).</summary>
		private static List<SasPaymentTimelineStepDto> StaffTimeline(SamplePayment p, string moName, string? moRole)
		{
			var steps = new List<SasPaymentTimelineStepDto>
			{
				new()
				{
					Title = "MO Submitted Payment",
					Description = $"Payment of {Money(p.Amount)} submitted with transaction {p.TransactionId}.",
					At = p.CreatedAt,
					ByName = string.IsNullOrWhiteSpace(moRole) ? moName : $"{moName} ({moRole})",
					IsDone = true
				}
			};

			switch (p.Status)
			{
				case SamplePaymentStatus.Pending:
					steps.Add(new() { Title = "Admin Review Pending", Description = "Waiting for the admin to check the proof and the amount.", IsCurrent = true });
					steps.Add(new() { Title = "Finance Verification Pending", Description = "Forwarded to Finance after admin approval." });
					steps.Add(new() { Title = "Finance — Payment Verified", Description = "Finance confirms the amount was received." });
					return steps;

				case SamplePaymentStatus.Rejected:
					steps.Add(new()
					{
						Title = "Returned to MO",
						Description = string.IsNullOrWhiteSpace(p.RejectReason) ? "The admin returned the payment." : $"Reason: {p.RejectReason}",
						At = p.ReviewedAt,
						ByName = p.ReviewedByName,
						IsDone = true,
						IsCurrent = true
					});
					return steps;
			}

			steps.Add(new()
			{
				Title = "Admin Reviewed Payment",
				Description = "Payment proof, transaction details and amount checked.",
				At = p.ReviewedAt,
				ByName = p.ReviewedByName,
				IsDone = true
			});
			steps.Add(new()
			{
				Title = $"Admin Approved & Forwarded to {p.ForwardedToName ?? "Finance"}",
				// VerifiedAmount holds Finance's figure once Finance has processed the payment.
				Description = p.VerifiedAmount.HasValue && p.FinanceStatus is SampleFinanceStatus.AwaitingVerification or SampleFinanceStatus.NotForwarded
					? $"Approved amount {Money(p.VerifiedAmount.Value)}."
					: "Payment approved by the admin.",
				At = p.ForwardedAt ?? p.ReviewedAt,
				ByName = p.ReviewedByName,
				IsDone = true
			});

			var awaiting = p.FinanceStatus == SampleFinanceStatus.AwaitingVerification;
			steps.Add(new()
			{
				Title = "Finance Verification Pending",
				Description = awaiting ? "Waiting for Finance to confirm the amount received." : "Picked up by Finance.",
				At = p.ForwardedAt,
				IsDone = !awaiting,
				IsCurrent = awaiting
			});

			steps.Add(p.FinanceStatus switch
			{
				SampleFinanceStatus.Verified => new SasPaymentTimelineStepDto
				{
					Title = "Finance — Payment Verified",
					Description = string.IsNullOrWhiteSpace(p.FinanceRemarks) ? "Amount received and verified." : p.FinanceRemarks,
					At = p.FinanceVerifiedAt,
					ByName = p.FinanceVerifiedByName,
					IsDone = true,
					IsCurrent = true
				},
				SampleFinanceStatus.Mismatch => new SasPaymentTimelineStepDto
				{
					Title = p.VerifiedAmount.HasValue && p.VerifiedAmount < p.Amount ? "Finance — Amount Mismatch" : "Finance — Payment Failed",
					Description = p.VerifiedAmount.HasValue && p.VerifiedAmount < p.Amount
						? $"Received {Money(p.VerifiedAmount.Value)} against {Money(p.Amount)}: {p.FinanceRemarks}"
						: p.FinanceRemarks,
					At = p.FinanceVerifiedAt,
					ByName = p.FinanceVerifiedByName,
					IsDone = true,
					IsCurrent = true
				},
				_ => new SasPaymentTimelineStepDto
				{
					Title = "Finance — Payment Verified",
					Description = "Finance confirms the amount was received."
				}
			});

			return steps;
		}

		/// <summary>Farmer wording (screen 35, "Verification Journey").</summary>
		private static List<SasPaymentTimelineStepDto> FarmerTimeline(SamplePayment p, string moName)
		{
			var steps = new List<SasPaymentTimelineStepDto>
			{
				new()
				{
					Title = "Payment Submitted",
					Description = $"Your payment of {Money(p.Amount)} was submitted by {moName}.",
					At = p.CreatedAt,
					IsDone = true
				}
			};

			if (p.Status == SamplePaymentStatus.Rejected)
			{
				steps.Add(new()
				{
					Title = "Payment Returned",
					Description = "The admin sent the payment back to your MO officer for correction." +
								  (string.IsNullOrWhiteSpace(p.RejectReason) ? "" : $" Reason: {p.RejectReason}"),
					At = p.ReviewedAt,
					IsDone = true,
					IsCurrent = true
				});
				return steps;
			}

			var approved = p.Status == SamplePaymentStatus.Approved;
			steps.Add(new()
			{
				Title = "Admin Approved",
				Description = approved ? "Your payment proof was checked and approved." : "Your payment is waiting for admin approval.",
				At = approved ? p.ReviewedAt : null,
				IsDone = approved,
				IsCurrent = !approved
			});

			var financeDone = p.FinanceStatus is SampleFinanceStatus.Verified or SampleFinanceStatus.Mismatch;
			steps.Add(new()
			{
				Title = "Finance Verification",
				Description = financeDone ? "The finance team checked your payment." : "The finance team is verifying your payment.",
				At = approved ? p.ForwardedAt : null,
				IsDone = financeDone,
				IsCurrent = approved && !financeDone
			});

			steps.Add(p.FinanceStatus switch
			{
				SampleFinanceStatus.Verified => new SasPaymentTimelineStepDto
				{
					Title = "Payment Confirmed",
					Description = "Your payment has been confirmed.",
					At = p.FinanceVerifiedAt,
					IsDone = true,
					IsCurrent = true
				},
				SampleFinanceStatus.Mismatch => new SasPaymentTimelineStepDto
				{
					Title = "Payment Issue",
					Description = "There is an issue with this payment; your MO officer will contact you." +
								  (string.IsNullOrWhiteSpace(p.FinanceRemarks) ? "" : $" ({p.FinanceRemarks})"),
					At = p.FinanceVerifiedAt,
					IsDone = true,
					IsCurrent = true
				},
				_ => new SasPaymentTimelineStepDto
				{
					Title = "Payment Confirmed",
					Description = "You will see the confirmation here once Finance verifies the payment."
				}
			});

			return steps;
		}

		private sealed record Location(string? Region, string? State);

		private async Task<Dictionary<int, Location>> LocationsAsync(IEnumerable<int?> hqIds)
		{
			var ids = hqIds.Where(h => h is > 0).Select(h => h!.Value).Distinct().ToList();
			if (ids.Count == 0) return new Dictionary<int, Location>();

			var rows = await _db.Headquarters.AsNoTracking()
				.Where(h => ids.Contains(h.Id))
				.Select(h => new
				{
					h.Id,
					Region = h.Region != null ? h.Region.RegionName : null,
					State = h.Region != null && h.Region.State != null ? h.Region.State.StateName : null
				})
				.ToListAsync();

			return rows.ToDictionary(r => r.Id, r => new Location(r.Region, r.State));
		}

		private static string? CropOf(IEnumerable<(string? Crop1, string? Crop2)> items)
		{
			var crops = items
				.SelectMany(i => new[] { i.Crop1, i.Crop2 })
				.Where(c => !string.IsNullOrWhiteSpace(c))
				.Select(c => c!.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			return crops.Count == 0 ? null : string.Join(", ", crops);
		}

		// ---------------------------------------------------------------- codes

		/// <summary>Gives every payment without a reference one (oldest first), a batch at a time.</summary>
		private async Task BackfillCodesAsync()
		{
			var missing = await _db.SamplePayments.AsNoTracking()
				.Where(p => p.Code == null)
				.OrderBy(p => p.Id)
				.Select(p => p.Id)
				.Take(CodeBackfillBatch)
				.ToListAsync();

			foreach (var id in missing)
				await EnsureCodeAsync(id);
		}

		/// <summary>Assigns PAY-{yyyy}-{00001} to one payment when it has none; retries on a unique clash.</summary>
		private async Task EnsureCodeAsync(int id)
		{
			for (var attempt = 0; attempt < 5; attempt++)
			{
				var payment = await _db.SamplePayments.FirstOrDefaultAsync(p => p.Id == id);
				if (payment == null || !string.IsNullOrWhiteSpace(payment.Code)) return;

				payment.Code = await SasPaymentCodes.NextAsync(_db, payment.CreatedAt.Year);

				try
				{
					await _db.SaveChangesAsync();
					return;
				}
				catch (DbUpdateException ex) when (SasPaymentCodes.IsUniqueViolation(ex))
				{
					_db.Entry(payment).State = EntityState.Detached;
				}
			}
		}

		// ---------------------------------------------------------------- helpers

		private void AddEvent(int collectionId, string status, string? note, string? byName)
		{
			_db.SasStatusEvents.Add(new SasStatusEvent
			{
				CollectionId = collectionId,
				Status = status,
				Note = note,
				ByName = byName,
				At = DateTime.Now
			});
		}

		private AppRole? CurrentRole()
		{
			var raw = User.FindFirst(ClaimTypes.Role)?.Value;
			return Enum.TryParse<AppRole>(raw, true, out var role) ? role : null;
		}

		private async Task<string> ResolveDisplayNameAsync()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

			var profile = await _db.Users.AsNoTracking()
				.Where(u => u.Id == userId)
				.Select(u => new { u.Name, u.UserName })
				.FirstOrDefaultAsync();

			var name = profile?.Name;
			if (string.IsNullOrWhiteSpace(name)) name = profile?.UserName;
			if (string.IsNullOrWhiteSpace(name)) name = User.Identity?.Name;

			return name ?? "";
		}

		private ObjectResult Forbid403(string message = "You are not allowed to view SAS payments.") =>
			StatusCode(403, new { Success = false, Message = message });

		/// <summary>Full path of a stored upload inside Uploads/, or null when it is missing or unsafe.</summary>
		private string? ResolveUpload(string? relative)
		{
			if (string.IsNullOrWhiteSpace(relative) ||
				relative.Contains("..", StringComparison.Ordinal) ||
				relative.IndexOf(':') >= 0)
			{
				return null;
			}

			var root = Path.Combine(_env.ContentRootPath, "Uploads");
			var normalized = relative.TrimStart('\\', '/').Replace('/', Path.DirectorySeparatorChar);
			var fullPath = Path.GetFullPath(Path.Combine(root, normalized));
			var rootWithSep = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;

			return fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath)
				? fullPath
				: null;
		}

		private static string ContentTypeFor(string path) => Path.GetExtension(path).ToLowerInvariant() switch
		{
			".jpg" or ".jpeg" => "image/jpeg",
			".png" => "image/png",
			".webp" => "image/webp",
			".pdf" => "application/pdf",
			_ => "application/octet-stream"
		};

		private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.GetCultureInfo("en-IN"));

		private static string? Clean(string? value) =>
			string.IsNullOrWhiteSpace(value) ? null : value.Trim();

		private static string? JoinNonEmpty(string separator, params string?[] parts)
		{
			var list = parts.Where(p => !string.IsNullOrWhiteSpace(p)).Select(p => p!.Trim())
				.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
			return list.Count == 0 ? null : string.Join(separator, list);
		}

		private static bool TryParseEnum<TEnum>(string? raw, out TEnum value) where TEnum : struct, Enum
		{
			value = default;
			if (string.IsNullOrWhiteSpace(raw)) return false;

			if (Enum.TryParse(raw.Trim(), true, out value) && Enum.IsDefined(typeof(TEnum), value))
				return true;

			value = default;
			return false;
		}
	}

	/// <summary>PAY-{yyyy}-{00001} references (shared with the v1 payment create in SasController).</summary>
	public static class SasPaymentCodes
	{
		public static async Task<string> NextAsync(AppDbContext db, int year)
		{
			var head = $"PAY-{year}-";

			var codes = await db.SamplePayments.AsNoTracking()
				.Where(p => p.Code != null && p.Code.StartsWith(head))
				.Select(p => p.Code!)
				.ToListAsync();

			var max = 0;
			foreach (var code in codes)
			{
				var tail = code.Length > head.Length ? code[head.Length..] : "";
				if (int.TryParse(tail, out var value) && value > max) max = value;
			}

			return head + (max + 1).ToString("00000");
		}

		public static bool IsUniqueViolation(DbUpdateException ex) =>
			ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
	}
}
