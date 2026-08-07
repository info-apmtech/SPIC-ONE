using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using Spic.Infrastructure.Data;
using System.Security.Claims;
using System.Collections.Generic;
using static System.Net.WebRequestMethods;
using Microsoft.AspNetCore.Identity;
namespace SpicAPI.Controllers
{

	[Route("api/[controller]")]
	public class DealerRegistrationController : GenericCrudController<DealerRegistration>
	{
		private readonly IGenericRepository<DealerApprovalHistory>? _historyRepo;
		private readonly IGenericRepository<DealerExperience>? _expRepo;
		private readonly IGenericRepository<AnnualSaleDataLastFYofDealerRegistration>? _annualRepo;
		private readonly IGenericRepository<DealerWarehouseFacilities>? _whRepo;
		private readonly IGenericRepository<DealerRailFacilities>? _railRepo;
		private readonly IGenericRepository<DealerPortFacilities>? _portRepo;
		private readonly IGenericRepository<DealerMarketDetail>? _marketRepo;
		private readonly IGenericRepository<DealerCompaniesOperatingInArea>? _compRepo;
		private readonly IGenericRepository<DealerOwnershipInfo>? _ownerRepo;
		private readonly IGenericRepository<SalesPlanningInDealerRegistration>? _salesPlanRepo;
		private readonly IGenericRepository<DealerAssetBank>? _bankRepo;
		private readonly IGenericRepository<DealerAssetLand>? _landRepo;
		private readonly IGenericRepository<DealerAssetBuilding>? _buildingRepo;
		private readonly IGenericRepository<DealerCreditLimitProposal>? _creditRepo;
		private readonly IGenericRepository<DealerRegistrationDocuments>? _docsRepo;
		private readonly IGenericRepository<Designation>? _designationRepo;
		private readonly IGenericRepository<PartnerFamilyDetails>? _partnerRepo;
		private readonly IGenericRepository<PartnerOccupation>? _occRepo;
		private readonly IGenericRepository<DealerLoanLiabilities>? _loanRepo;
		private readonly IGenericRepository<UserInfo> _userInfoRepo;
		private readonly UserManager<UserInfo> _userManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly AppDbContext _db;
		// Single constructor: optional repositories are injected when registered. Defaults to null to avoid breaking DI.
		public DealerRegistrationController(
			IGenericRepository<DealerRegistration> repo,
			IGenericRepository<UserInfo> userInfoRepo,
			UserManager<UserInfo> userManager,
			RoleManager<IdentityRole> roleManager,
			AppDbContext db,
			IGenericRepository<DealerApprovalHistory>? historyRepo = null,
			IGenericRepository<DealerExperience>? expRepo = null,
			IGenericRepository<AnnualSaleDataLastFYofDealerRegistration>? annualRepo = null,
			IGenericRepository<DealerWarehouseFacilities>? whRepo = null,
			IGenericRepository<DealerRailFacilities>? railRepo = null,
			IGenericRepository<DealerPortFacilities>? portRepo = null,
			IGenericRepository<DealerMarketDetail>? marketRepo = null,
			IGenericRepository<DealerCompaniesOperatingInArea>? compRepo = null,
			IGenericRepository<DealerOwnershipInfo>? ownerRepo = null,
			IGenericRepository<SalesPlanningInDealerRegistration>? salesPlanRepo = null,
			IGenericRepository<DealerAssetBank>? bankRepo = null,
			IGenericRepository<DealerAssetLand>? landRepo = null,
			IGenericRepository<DealerAssetBuilding>? buildingRepo = null,
			IGenericRepository<DealerCreditLimitProposal>? creditRepo = null,
			IGenericRepository<DealerRegistrationDocuments>? docsRepo = null,
			IGenericRepository<Designation>? designationRepo = null,
			IGenericRepository<PartnerFamilyDetails>? partnerRepo = null,
			IGenericRepository<PartnerOccupation>? occRepo = null,
			IGenericRepository<DealerLoanLiabilities>? loanRepo = null
			) : base(repo)
		{
			_historyRepo = historyRepo;
			_userInfoRepo = userInfoRepo;
			_userManager = userManager;
			_roleManager = roleManager;
			_db = db;
			_expRepo = expRepo;
			_annualRepo = annualRepo;
			_whRepo = whRepo;
			_railRepo = railRepo;
			_portRepo = portRepo;
			_marketRepo = marketRepo;
			_compRepo = compRepo;
			_ownerRepo = ownerRepo;
			_salesPlanRepo = salesPlanRepo;
			_bankRepo = bankRepo;
			_landRepo = landRepo;
			_buildingRepo = buildingRepo;
			_creditRepo = creditRepo;
			_docsRepo = docsRepo;
			_designationRepo = designationRepo;
			_partnerRepo = partnerRepo;
			_occRepo = occRepo;
			_loanRepo = loanRepo;
		}
		private static readonly HashSet<string> _writeRoles = new(StringComparer.OrdinalIgnoreCase)
		{
			"Admin", "CorporateAdmin", "Director", "AVP", "SMD", "SMM", "RM", "RMD", "MDO", "JMDO", "MO"
		};

		private static readonly HashSet<string> _adminRoles = new(StringComparer.OrdinalIgnoreCase)
		{
			"Admin", "CorporateAdmin"
		};

		private static readonly HashSet<string> _hqCreatorRoles = new(StringComparer.OrdinalIgnoreCase)
		{
			"MDO", "JMDO", "MO"
		};

		private bool IsWriteRole(string role) => _writeRoles.Contains(role);
		private bool IsAdminRole(string role) => _adminRoles.Contains(role);
		private bool IsHqCreatorRole(string role) => _hqCreatorRoles.Contains(role);

		[HttpPost]
		public override async Task<IActionResult> Create([FromBody] DealerRegistration entity)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			if (!IsWriteRole(role))
				return Forbid();

			return await base.Create(entity);
		}

		[HttpPut("{id}")]
		public override async Task<IActionResult> Update(int id, [FromBody] DealerRegistration entity)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			if (!IsWriteRole(role))
				return Forbid();

			return await base.Update(id, entity);
		}

		[HttpDelete("{id}")]
		public override async Task<IActionResult> Delete(int id)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			if (!IsAdminRole(role))
				return Forbid();

			return await base.Delete(id);
		}

		[HttpGet("all")]
		public override async Task<IActionResult> GetAllWithInactive()
		{
			var query = _repo.GetAllWithInactive();

			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var regionClaim = User.FindFirst("spic:region_id")?.Value;
			var stateClaim = User.FindFirst("spic:state_id")?.Value;
			var hqClaim = User.FindFirst("spic:hq_id")?.Value;

			// Admin / CorporateAdmin → full data
			if (role == "Admin" || role == "CorporateAdmin")
				return Ok(await query.ToListAsync());
			if (role == "RM" && int.TryParse(regionClaim, out var regionId))
				query = query.Where(x => x.Region == regionId);
			else if ((role == "SM") && int.TryParse(stateClaim, out var stateId))
				query = query.Where(x => x.StateId == stateId);
			else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(stateClaim, out var moStateId))
				query = query.Where(x => x.StateId == moStateId);
			else
				query = query.Where(x => x.CreatedBy == userId);
			return Ok(await query.ToListAsync());
		}
		[HttpGet("lookup")]
		public async Task<IActionResult> GetLookup()
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var regionClaim = User.FindFirst("spic:region_id")?.Value;
			var stateClaim = User.FindFirst("spic:state_id")?.Value;
			var hqClaim = User.FindFirst("spic:hq_id")?.Value;

			var query = _repo.GetAllWithInactive();

			if (role != "Admin" && role != "CorporateAdmin")
			{
				if (role == "RM" && int.TryParse(regionClaim, out var regionId))
					query = query.Where(x => x.Region == regionId);
				else if (role == "SM" && int.TryParse(stateClaim, out var stateId))
					query = query.Where(x => x.StateId == stateId);
				else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(stateClaim, out var moStateId))
					query = query.Where(x => x.StateId == moStateId);
				else
					query = query.Where(x => x.CreatedBy == userId);
			}

			var lookup = await query
				.Select(x => new { x.Id, x.DealerCode, x.FirmName, x.StateId })
				.ToListAsync();

			return Ok(lookup);
		}

		/// <summary>
		/// Returns a per-step completion summary for the given dealer.
		/// This consolidates multiple client calls into a single API.
		/// The response is an array of objects with StepNo (1-based) and IsComplete boolean.
		/// </summary>
		[HttpGet("{dealerId}/step-completion-summary")]
		public async Task<IActionResult> GetStepCompletionSummary(int dealerId)
		{
			var result = new List<object>();

			var dealer = await _repo.GetByIdAsync(dealerId);

			// Step 1: Dealer basic info (DealerRegistration exists).
			// PinCode is only collected on the full registration flow; Terminated /
			// Inactive-Terminated (restricted) flows skip Primary Location entirely,
			// so also treat those statuses as complete for Step 1.
			bool step1 = dealer != null &&
				(!string.IsNullOrWhiteSpace(dealer.PinCode) ||
				 dealer.Status == DealerStatus.Terminated ||
				 (dealer.Status == DealerStatus.InActive && dealer.InactiveProposal == FutureBusinessProposal.Terminated));
			result.Add(new { StepNo = 1, IsComplete = step1 });

			// Step 2: DealerExperience
			bool step2 = _expRepo != null && await _expRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			result.Add(new { StepNo = 2, IsComplete = step2 });

			// Step 3: AnnualSaleDataLastFYofDealerRegistration
			bool step3 = _annualRepo != null && await _annualRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			result.Add(new { StepNo = 3, IsComplete = step3 });

			// Step 4: Warehouse facilities (warehouse + rail + port combined)
			bool wh = _whRepo != null && await _whRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool rail = _railRepo != null && await _railRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool port = _portRepo != null && await _portRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool step4 = wh || rail || port;
			result.Add(new { StepNo = 4, IsComplete = step4 });

			// Step 5: Market detail
			bool step5 = _marketRepo != null && await _marketRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			result.Add(new { StepNo = 5, IsComplete = step5 });

			// Step 6: Companies operating in area
			bool step6 = _compRepo != null && await _compRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			result.Add(new { StepNo = 6, IsComplete = step6 });

			// Step 7: Ownership info
			bool step7 = _ownerRepo != null && await _ownerRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			result.Add(new { StepNo = 7, IsComplete = step7 });

			// Step 8: Sales planning
			bool step8 = _salesPlanRepo != null && await _salesPlanRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			result.Add(new { StepNo = 8, IsComplete = step8 });

			// Step 9: Assets (bank/land/building)
			bool bank = _bankRepo != null && await _bankRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool land = _landRepo != null && await _landRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool building = _buildingRepo != null && await _buildingRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool step9 = bank || land || building;
			result.Add(new { StepNo = 9, IsComplete = step9 });

			// Step 10: Credit limit proposal (SPIC). New-dealer flow instead records
			// the SPIC Trade Deposit Details (Dealership Application Fee + Trade Deposit DD).
			bool credit = _creditRepo != null && await _creditRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			bool step10 = credit;
			if (dealer?.IsNewDealerRegistration == true)
			{
				step10 = dealer.InSpic
					&& (!string.IsNullOrWhiteSpace(dealer.SpicTradeDepositDDNumber)
						|| dealer.SpicTradeDepositDDBankId > 0
						|| (dealer.SpicTradeDepositDDAmount ?? 0) > 0
						|| !string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeDDNumber)
						|| dealer.DealershipApplicationFeeBankId > 0
						|| (dealer.DealershipApplicationFeeAmount ?? 0) > 0);
			}
			result.Add(new { StepNo = 10, IsComplete = step10 });

			// Step 11: Credit limit for GreenStar (same check as step 10).
			// New-dealer flow instead records the GFL Trade Deposit Details.
			bool step11 = credit;
			if (dealer?.IsNewDealerRegistration == true)
			{
				step11 = dealer.InGreenStar
					&& (!string.IsNullOrWhiteSpace(dealer.GflTradeDepositDDNumber)
						|| dealer.GflTradeDepositDDBankId > 0
						|| (dealer.GflTradeDepositDDAmount ?? 0) > 0);
			}
			result.Add(new { StepNo = 11, IsComplete = step11 });

			// Step 12: Documents
			bool step12 = _docsRepo != null && await _docsRepo.ExistsAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
			if (dealer?.IsNewDealerRegistration == true && _docsRepo != null)
			{
				var docsRow = await _docsRepo.GetAll().FirstOrDefaultAsync(x => EF.Property<int>(x, "DealerId") == dealerId);
				step12 = docsRow != null && !string.IsNullOrWhiteSpace(docsRow.RequestLetterFilePath);
			}
			result.Add(new { StepNo = 12, IsComplete = step12 });

			// Step 13: Final submission (dealer record exists)
			bool step13 = dealer != null;
			result.Add(new { StepNo = 13, IsComplete = step13 });

			return Ok(result);
		}
		private async Task<int?> ResolveDealerDesignationIdAsync()
		{
			if (_designationRepo == null) return null;

			var designation = await _designationRepo.GetAll()
				.FirstOrDefaultAsync(d => d.Name != null && d.Name.ToUpper() == "DEALER");

			return designation?.Id;
		}
		private async Task EnsureRoleAndAssignAsync(UserInfo user, AppRole role)
		{
			var roleName = role.ToString();

			if (!await _roleManager.RoleExistsAsync(roleName))
			{
				var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

				if (!roleResult.Succeeded)
					throw new Exception(string.Join(", ", roleResult.Errors.Select(e => e.Description)));
			}

			if (!await _userManager.IsInRoleAsync(user, roleName))
			{
				var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);

				if (!addRoleResult.Succeeded)
					throw new Exception(string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
			}
		}
		[HttpPut("update-with-user/{id}")]
		public async Task<IActionResult> UpdateDealerWithUser(int id, [FromBody] DealerRegistration dealer)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			if (!IsWriteRole(role))
				return Forbid();

			if (id != dealer.Id) return BadRequest("ID mismatch");

			if (string.IsNullOrEmpty(dealer.UserTableId) && !string.IsNullOrEmpty(dealer.DealerCode))
			{
				var userError = await EnsureDealerUserAsync(dealer);
				if (!string.IsNullOrEmpty(userError))
					return BadRequest(userError);
			}

			// Save the DealerRegistration changes
			await _repo.UpdateAsync(dealer);

			return Ok(dealer);
		}

		/// <summary>
		/// Creates (or links) the login account for a dealer identified by its DealerCode.
		/// Returns an error message on failure, or null on success.
		/// </summary>
		private async Task<string?> EnsureDealerUserAsync(DealerRegistration dealer)
		{
			// 1. Check if the user ALREADY EXISTS in the database by their DealerCode
			var existingAppUser = await _userInfoRepo.GetAll()
				.FirstOrDefaultAsync(u => u.NormalizedUserName == dealer.DealerCode.ToUpper());

			if (existingAppUser != null)
			{
				// 2. The account already exists! Just link the existing ID to the dealer.
				// If it has no designation yet, assign the default Dealer designation.
				if (existingAppUser.DesignationId == null)
				{
					var existingUserDesignationId = await ResolveDealerDesignationIdAsync();
					if (existingUserDesignationId != null)
					{
						existingAppUser.DesignationId = existingUserDesignationId;
						existingAppUser.UpdatedAt = DateTime.UtcNow;
						existingAppUser.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
						await _userManager.UpdateAsync(existingAppUser);
					}
				}

				dealer.UserTableId = existingAppUser.Id;
				return null;
			}

			// 3. The account truly doesn't exist. Create it safely.
			var newUserId = Guid.NewGuid().ToString();
			var phonePass = string.IsNullOrWhiteSpace(dealer.OfficialContactNumber)
				? "1234567890"
				: dealer.OfficialContactNumber;

			var dealerDesignationId = await ResolveDealerDesignationIdAsync();

			var newUser = new UserInfo
			{
				Id = newUserId,
				UserName = dealer.DealerCode,
				NormalizedUserName = dealer.DealerCode.ToUpper(),
				PhoneNumber = dealer.OfficialContactNumber,
				Password = phonePass,
				Name = dealer.FirmName,
				Role = (SPIC.Core.Entities.AppRole)11,
				DesignationId = dealerDesignationId,
				IsActive = true,
				CreatedAt = DateTime.UtcNow,
				CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System",
				UpdatedAt = DateTime.UtcNow,
				UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System"
			};

			var createUserResult = await _userManager.CreateAsync(newUser, phonePass);

			if (!createUserResult.Succeeded)
			{
				return string.Join(", ", createUserResult.Errors.Select(e => e.Description));
			}

			await EnsureRoleAndAssignAsync(newUser, newUser.Role);
			dealer.UserTableId = newUserId;
			return null;
		}

		/// <summary>
		/// Submits an already-saved registration for approval without binding the
		/// complete DealerRegistration entity from the request body. This prevents
		/// hidden/unavailable fields such as UserTableId and DealerCode from causing
		/// model-binding validation errors in the New Dealer flow.
		/// </summary>
		[HttpPost("{id:int}/submit-for-review")]
		public async Task<IActionResult> SubmitForReview(int id)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
			if (!IsWriteRole(role))
				return Forbid();

			var dealer = await _repo.GetByIdAsync(id);
			if (dealer == null)
				return NotFound(new { message = "Dealer registration was not found." });

			// New Dealer Creation reuses the two Credit Limit routes for the
			// SPIC/GFL trade-deposit forms. Validate those saved fields instead
			// of the Existing Dealer credit-limit proposal.
			if (dealer.IsNewDealerRegistration)
			{
				var errors = new Dictionary<string, string[]>();

				if (!dealer.InSpic && !dealer.InGreenStar)
				{
					errors["CompanySelection"] = new[]
					{
						"Select SPIC, Greenstar, or both before final submission."
					};
				}

				if (dealer.InSpic)
				{
					if (dealer.DealershipApplicationFeeBankId is null or <= 0)
						errors[nameof(dealer.DealershipApplicationFeeBankId)] = new[] { "Application Fee Bank is required." };
					if (string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeeDDNumber))
						errors[nameof(dealer.DealershipApplicationFeeDDNumber)] = new[] { "Application Fee DD Number is required." };
					if (!dealer.DealershipApplicationFeeDDDate.HasValue)
						errors[nameof(dealer.DealershipApplicationFeeDDDate)] = new[] { "Application Fee DD Date is required." };
					if ((dealer.DealershipApplicationFeeAmount ?? 0) <= 0)
						errors[nameof(dealer.DealershipApplicationFeeAmount)] = new[] { "Application Fee Amount must be greater than zero." };
					if (string.IsNullOrWhiteSpace(dealer.DealershipApplicationFeePayableAt))
						errors[nameof(dealer.DealershipApplicationFeePayableAt)] = new[] { "Application Fee Payable At is required." };

					if (dealer.SpicTradeDepositDDBankId is null or <= 0)
						errors[nameof(dealer.SpicTradeDepositDDBankId)] = new[] { "SPIC Trade Deposit Bank is required." };
					if (string.IsNullOrWhiteSpace(dealer.SpicTradeDepositDDNumber))
						errors[nameof(dealer.SpicTradeDepositDDNumber)] = new[] { "SPIC Trade Deposit DD Number is required." };
					if (!dealer.SpicTradeDepositDDDate.HasValue)
						errors[nameof(dealer.SpicTradeDepositDDDate)] = new[] { "SPIC Trade Deposit DD Date is required." };
					if ((dealer.SpicTradeDepositDDAmount ?? 0) <= 0)
						errors[nameof(dealer.SpicTradeDepositDDAmount)] = new[] { "SPIC Trade Deposit Amount must be greater than zero." };
				}

				if (dealer.InGreenStar)
				{
					if (dealer.GflTradeDepositDDBankId is null or <= 0)
						errors[nameof(dealer.GflTradeDepositDDBankId)] = new[] { "GFL Trade Deposit Bank is required." };
					if (string.IsNullOrWhiteSpace(dealer.GflTradeDepositDDNumber))
						errors[nameof(dealer.GflTradeDepositDDNumber)] = new[] { "GFL Trade Deposit DD Number is required." };
					if (!dealer.GflTradeDepositDDDate.HasValue)
						errors[nameof(dealer.GflTradeDepositDDDate)] = new[] { "GFL Trade Deposit DD Date is required." };
					if ((dealer.GflTradeDepositDDAmount ?? 0) <= 0)
						errors[nameof(dealer.GflTradeDepositDDAmount)] = new[] { "GFL Trade Deposit Amount must be greater than zero." };
				}

				// Only the New Dealer flow requires this additional enclosure.
				if (_docsRepo != null)
				{
					var documents = await _docsRepo.GetAll()
						.FirstOrDefaultAsync(x => EF.Property<int>(x, "DealerId") == id);

					if (documents == null || string.IsNullOrWhiteSpace(documents.RequestLetterFilePath))
					{
						errors[nameof(DealerRegistrationDocuments.RequestLetterFilePath)] = new[]
						{
							"Request Letter for Dealership is required. Upload it on the firm's letterhead with seal and signature."
						};
					}
				}

				if (errors.Count > 0)
				{
					return BadRequest(new
					{
						title = "New Dealer final-submission validation failed.",
						status = 400,
						errors
					});
				}
			}

			// Preserve the existing rejection/resubmission workflow. The record
			// returns only to the approval level that rejected it.
			if (dealer.AVPApproved == false)
			{
				dealer.AVPApproved = null;
			}
			else if (dealer.SMApproved == false)
			{
				dealer.SMApproved = null;
				dealer.AVPApproved = null;
			}
			else if (dealer.RMApproved == false)
			{
				dealer.RMApproved = null;
				dealer.SMApproved = null;
				dealer.AVPApproved = null;
			}

			// Keep the Existing Dealer status behavior unchanged. A New Dealer
			// receives its DealerCode and active status only after final approval.
			if (!dealer.IsNewDealerRegistration &&
				dealer.Status != DealerStatus.InActive &&
				dealer.Status != DealerStatus.Terminated)
			{
				dealer.Status = DealerStatus.Active;
			}

			dealer.IsSubmittedForReview = true;
			dealer.UpdatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
			dealer.UpdatedAt = DateTime.Now;

			await _repo.UpdateAsync(dealer);

			return Ok(new
			{
				dealerId = dealer.Id,
				isSubmittedForReview = true,
				message = "Registration submitted successfully."
			});
		}

		/// <summary>
		/// Assigns a unique sequential numeric DealerCode to a new-flow dealer once its
		/// registration is finally approved. The code is the largest existing numeric
		/// DealerCode + 1, and is checked for uniqueness before being saved.
		///
		/// Safety rules (New Dealer workflow):
		///  - Only NEW_DEALER records (IsNewDealerRegistration == true) may receive a code here.
		///  - MO (RM), SMM (SM) and AVP approvals must all be complete.
		///  - The allocation is idempotent: a code is generated only once per dealer.
		///  - Generation happens inside a database transaction and never replaces an
		///    existing Dealer Code.
		/// </summary>
		[HttpPost("{id}/generate-dealer-code")]
		public async Task<IActionResult> GenerateDealerCode(int id)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			if (!IsWriteRole(role))
				return Forbid();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

			var dealer = await _repo.GetByIdAsync(id);
			if (dealer == null)
				return NotFound(new { message = "Dealer registration was not found." });

			if (!dealer.IsNewDealerRegistration)
			{
				// Existing Dealer records already have a code; never regenerate/reallocate it.
				return Ok(new { dealer.Id, dealer.DealerCode, alreadyAllocated = !string.IsNullOrWhiteSpace(dealer.DealerCode) });
			}

			// Dealer Code must only be generated after the complete approval chain.
			if (dealer.RMApproved != true || dealer.SMApproved != true || dealer.AVPApproved != true)
			{
				return BadRequest(new
				{
					message = "Dealer Code can only be generated after MO, SMM and AVP approvals are complete."
				});
			}

			// Idempotent: one New Dealer = one Dealer Code.
			if (!string.IsNullOrWhiteSpace(dealer.DealerCode))
				return Ok(new { dealer.Id, dealer.DealerCode, alreadyAllocated = true });

			await using var transaction = await _db.Database.BeginTransactionAsync();
			try
			{
				var fresh = await _db.DealerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
				if (fresh == null)
				{
					await transaction.RollbackAsync();
					return NotFound(new { message = "Dealer registration was not found." });
				}

				// Re-check inside the transaction to guard against concurrent allocation.
				if (!string.IsNullOrWhiteSpace(fresh.DealerCode))
				{
					await transaction.RollbackAsync();
					return Ok(new { fresh.Id, fresh.DealerCode, alreadyAllocated = true });
				}

				string candidate = await GetNextDealerCodeAsync();

				bool inUse = await _db.DealerRegistrations
					.AnyAsync(x => x.DealerCode != null && x.DealerCode == candidate);
				if (inUse)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new
					{
						message = "Dealer Code allocation failed: generated code is already in use. Please retry."
					});
				}

				fresh.DealerCode = candidate;
				fresh.Status = DealerStatus.Active;
				fresh.UpdatedBy = userId;
				fresh.UpdatedAt = DateTime.Now;

				await _db.SaveChangesAsync();
				await transaction.CommitAsync();

				if (string.IsNullOrEmpty(fresh.UserTableId) && !string.IsNullOrEmpty(fresh.DealerCode))
				{
					var userError = await EnsureDealerUserAsync(fresh);
					if (!string.IsNullOrEmpty(userError))
						return BadRequest(new { message = userError });

					// Persist the dealer's login account link (UserTableId).
					await _db.SaveChangesAsync();
				}

				return Ok(new { fresh.Id, fresh.DealerCode, alreadyAllocated = false });
			}
			catch
			{
				try { await transaction.RollbackAsync(); } catch { }
				return StatusCode(500, new
				{
					message = "Dealer Code allocation failed. The record was not modified. Please retry."
				});
			}
		}

		/// <summary>
		/// Final AVP approval for a New Dealer. Performs the entire final step atomically:
		/// validates the workflow and approval sequence, saves the AVP approval and remarks,
		/// allocates the Dealer Code and activates the dealer — all inside one transaction.
		///
		/// Idempotent: calling it again after a successful approval returns the already
		/// allocated code without generating another one.
		/// </summary>
		[HttpPost("{id}/final-approve")]
		public async Task<IActionResult> FinalApprove(int id, [FromBody] FinalApproveRequest request)
		{
			var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
			if (role != "AVP" && role != "Director" && role != "CorporateAdmin" && role != "Admin")
				return Forbid();

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

			var dealer = await _repo.GetByIdAsync(id);
			if (dealer == null)
				return NotFound(new { message = "Dealer registration was not found." });

			if (!dealer.IsNewDealerRegistration)
				return BadRequest(new { message = "This dealer is not a New Dealer registration." });

			if (dealer.RMApproved != true || dealer.SMApproved != true)
				return BadRequest(new
				{
					message = "Dealer must be approved by MO and SMM before final AVP approval."
				});

			// Idempotent: already finally approved → return the allocated code.
			if (dealer.AVPApproved == true && !string.IsNullOrWhiteSpace(dealer.DealerCode))
				return Ok(new { dealer.Id, dealer.DealerCode, alreadyApproved = true });

			if (dealer.AVPApproved == true)
				return Conflict(new
				{
					message = "Dealer is already marked AVP approved but has no Dealer Code. Please resolve manually."
				});

			if (!string.IsNullOrWhiteSpace(dealer.DealerCode))
				return BadRequest(new { message = "Dealer Code has already been allocated. Cannot allocate again." });

			var remarks = string.IsNullOrWhiteSpace(request?.Remarks) ? "" : request.Remarks.Trim();

			await using var transaction = await _db.Database.BeginTransactionAsync();
			try
			{
				// Reload with tracking inside the transaction to guard against
				// concurrent / duplicate final approvals.
				var fresh = await _db.DealerRegistrations.FirstOrDefaultAsync(x => x.Id == id);
				if (fresh == null)
				{
					await transaction.RollbackAsync();
					return NotFound(new { message = "Dealer registration was not found." });
				}

				if (fresh.AVPApproved == true && !string.IsNullOrWhiteSpace(fresh.DealerCode))
				{
					await transaction.RollbackAsync();
					return Ok(new { fresh.Id, fresh.DealerCode, alreadyApproved = true });
				}
				if (fresh.AVPApproved == true || !string.IsNullOrWhiteSpace(fresh.DealerCode))
				{
					await transaction.RollbackAsync();
					return Conflict(new
					{
						message = "This dealer has already been finally approved. Dealer Code cannot be generated twice."
					});
				}

				// Allocate a code and confirm it is not already used.
				string candidate = await GetNextDealerCodeAsync();
				bool inUse = await _db.DealerRegistrations
					.AnyAsync(x => x.DealerCode != null && x.DealerCode == candidate);
				if (inUse)
				{
					await transaction.RollbackAsync();
					return StatusCode(500, new
					{
						message = "Dealer Code allocation failed: generated code is already in use. The approval was not saved. Please retry."
					});
				}

				fresh.AVPApproved = true;
				fresh.Status = DealerStatus.Active;
				fresh.DealerCode = candidate;
				fresh.IsSubmittedForReview = true;
				fresh.UpdatedBy = userId;
				fresh.UpdatedAt = DateTime.Now;

				if (_historyRepo != null)
				{
					await _historyRepo.CreateAsync(new DealerApprovalHistory
					{
						DealerId = fresh.Id,
						ApprovedBy = userId,
						Role = role,
						ApprovedAt = DateTime.Now,
						Remarks = remarks,
						IsApproved = true
					});
				}

				await _db.SaveChangesAsync();
				await transaction.CommitAsync();

				// Create/link the dealer login account once the code is final.
				if (string.IsNullOrEmpty(fresh.UserTableId) && !string.IsNullOrWhiteSpace(fresh.DealerCode))
				{
					var userError = await EnsureDealerUserAsync(fresh);
					if (!string.IsNullOrEmpty(userError))
						return BadRequest(new { message = userError });

					await _db.SaveChangesAsync();
				}

				return Ok(new { fresh.Id, fresh.DealerCode, alreadyApproved = false });
			}
			catch
			{
				try { await transaction.RollbackAsync(); } catch { }
				return StatusCode(500, new
				{
					message = "Final approval failed and was rolled back. The Dealer Code was not allocated. Please retry."
				});
			}
		}

		/// <summary>
		/// Returns the next available numeric DealerCode (largest existing numeric code + 1).
		/// Only used inside an active transaction so the same code cannot be handed out twice.
		/// </summary>
		private async Task<string> GetNextDealerCodeAsync()
		{
			var existingCodes = await _db.DealerRegistrations
				.Where(x => x.DealerCode != null)
				.Select(x => x.DealerCode!)
				.ToListAsync();

			long max = 0;
			foreach (var code in existingCodes)
			{
				if (long.TryParse(code, out var numeric) && numeric > max)
					max = numeric;
			}

			string candidate;
			do
			{
				max++;
				candidate = max.ToString();
			}
			while (existingCodes.Any(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase)));

			return candidate;
		}

		public class FinalApproveRequest
		{
			public string? Remarks { get; set; }
		}
		[HttpPost("{id}/send-back")]
		public async Task<IActionResult> SendBack(int id, [FromBody] DealerSendBackRequest request)
		{
			var dealer = await _repo.GetByIdAsync(id);
			if (dealer == null) return NotFound();

			var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

			// Record history entry
			var remarks = new System.Text.StringBuilder();
			remarks.AppendLine($"Reason: {request.Reason}");
			remarks.AppendLine($"Priority: {request.Priority}");
			if (request.Sections != null && request.Sections.Any())
				remarks.AppendLine($"Sections: {string.Join(", ", request.Sections)}");
			if (!string.IsNullOrWhiteSpace(request.TargetRole))
				remarks.AppendLine($"TargetRole: {request.TargetRole}");

			var history = new DealerApprovalHistory
			{
				DealerId = id,
				ApprovedBy = userId,
				Role = role,
				ApprovedAt = DateTime.Now,
				Remarks = remarks.ToString()
			};

			if (_historyRepo != null)
			{
				await _historyRepo.CreateAsync(history);
			}

			// Update dealer approval flags depending on the caller role
			if (role == "RM" || role == "RMD")
				dealer.RMApproved = false;
			else if (role == "SMD" || role == "SMM")
				dealer.SMApproved = false;
			else if (role == "AVP" || role == "CorporateAdmin" || role == "Admin")
				dealer.AVPApproved = false;

			await _repo.PatchAsync(id, dealer);

			return Ok(new { message = "Send back recorded" });
		}
		[HttpPut("{id}/entity-type/{entityType:int}")]
		public async Task<IActionResult> SetEntityType(int id, int entityType)
		{
			var updated = await _repo.UpdatePropertyAsync(
				id,
				nameof(DealerRegistration.EntityType),
				(SPIC.Core.Entities.EntityType)entityType);

			if (updated == null) return NotFound();
			return Ok(new { updated.Id, EntityType = (int)entityType });
		}
		public class DealerSendBackRequest
		{
			public int DealerId { get; set; }
			public string Reason { get; set; } = "";
			public string Priority { get; set; } = "High";
			public List<string> Sections { get; set; } = new List<string>();
			public string? TargetRole { get; set; }
		}

		[HttpGet("dashboard-sap-counts")]
		public async Task<IActionResult> GetDashboardSapCounts()
		{
			var query = _repo.GetAllWithInactive();

			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var regionClaim = User.FindFirst("spic:region_id")?.Value;
			var stateClaim = User.FindFirst("spic:state_id")?.Value;
			var hqClaim = User.FindFirst("spic:hq_id")?.Value;

			if (role != "Admin" && role != "CorporateAdmin" && role != "Director" && role != "AVP")
			{
				if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
					query = query.Where(x => x.StateId == stateId);
				else if ((role == "RM" || role == "RMD") && int.TryParse(regionClaim, out var regionId) && regionId > 0)
					query = query.Where(x => x.Region == regionId);
				else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(hqClaim, out var hqId) && hqId > 0)
					query = query.Where(x => x.HQ == hqId);
				else
					query = query.Where(x => x.CreatedBy == userId);
			}

			var sapCounts = await query.Select(d => new { d.Id, d.StateId, d.InSpic, d.InGreenStar }).ToListAsync();

			var sapSpicByState = sapCounts.Where(d => d.InSpic).GroupBy(d => d.StateId).ToDictionary(g => g.Key, g => g.Count());
			var sapGflByState = sapCounts.Where(d => d.InGreenStar).GroupBy(d => d.StateId).ToDictionary(g => g.Key, g => g.Count());
			var allStateIds = sapCounts.Select(d => d.StateId).Distinct().ToList();

			return Ok(new
			{
				TotalDealers = sapCounts.Count,
				SpicByState = sapSpicByState,
				GflByState = sapGflByState,
				AllStateIds = allStateIds
			});
		}

		[HttpGet("dashboard-completion-counts")]
		public async Task<IActionResult> GetDashboardCompletionCounts()
		{
			var counts = new Dictionary<int, int>();

			var query = _repo.GetAllWithInactive()
				.Where(x => (x.PinCode != null && x.PinCode != "")
					|| x.Status == DealerStatus.Terminated
					|| (x.Status == DealerStatus.InActive && x.InactiveProposal == FutureBusinessProposal.Terminated)
					|| x.DealerType == RegistrationDealerType.Department);

			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var regionClaim = User.FindFirst("spic:region_id")?.Value;
			var stateClaim = User.FindFirst("spic:state_id")?.Value;
			var hqClaim = User.FindFirst("spic:hq_id")?.Value;

			if (role != "Admin" && role != "CorporateAdmin" && role != "Director" && role != "AVP")
			{
				if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
					query = query.Where(x => x.StateId == stateId);
				else if ((role == "RM" || role == "RMD") && int.TryParse(regionClaim, out var regionId) && regionId > 0)
					query = query.Where(x => x.Region == regionId);
				else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(hqClaim, out var hqId) && hqId > 0)
					query = query.Where(x => x.HQ == hqId);
				else
					query = query.Where(x => x.CreatedBy == userId);
			}

			var dealers = await query.Select(x => new { x.Id, x.PinCode }).ToListAsync();

			foreach (var d in dealers)
			{
				counts[d.Id] = 0;
				if (!string.IsNullOrWhiteSpace(d.PinCode))
				{
					counts[d.Id] = 1; // Step 1
				}
			}

			if (_expRepo != null) foreach (var id in await _expRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;
			if (_annualRepo != null) foreach (var id in await _annualRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

			var whIds = _whRepo != null ? await _whRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
			var railIds = _railRepo != null ? await _railRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
			var portIds = _portRepo != null ? await _portRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
			foreach (var id in whIds.Concat(railIds).Concat(portIds).Distinct()) if (counts.ContainsKey(id)) counts[id]++;

			if (_marketRepo != null) foreach (var id in await _marketRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;
			if (_compRepo != null) foreach (var id in await _compRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;
			if (_ownerRepo != null) foreach (var id in await _ownerRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

			if (_ownerRepo != null)
			{
				var ownerPartners = _partnerRepo != null ? await _partnerRepo.GetAll().Select(x => EF.Property<int>(x, "OwnershipPartnerId")).Distinct().ToListAsync() : new List<int>();
				var ownerOccs = _occRepo != null ? await _occRepo.GetAll().Select(x => EF.Property<int>(x, "OwnershipPartnerId")).Distinct().ToListAsync() : new List<int>();
				var step8Owners = ownerPartners.Concat(ownerOccs).Distinct().ToList();
				if (step8Owners.Any())
				{
					var step8Dealers = await _ownerRepo.GetAll().Where(o => step8Owners.Contains(o.Id)).Select(o => EF.Property<int>(o, "DealerId")).Distinct().ToListAsync();
					foreach (var id in step8Dealers) if (counts.ContainsKey(id)) counts[id]++;
				}
			}

			if (_salesPlanRepo != null) foreach (var id in await _salesPlanRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

			var bankIds = _bankRepo != null ? await _bankRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
			var landIds = _landRepo != null ? await _landRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
			var buildIds = _buildingRepo != null ? await _buildingRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync() : new List<int>();
			foreach (var id in bankIds.Concat(landIds).Concat(buildIds).Distinct()) if (counts.ContainsKey(id)) counts[id]++;

			if (_loanRepo != null) foreach (var id in await _loanRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

			if (_creditRepo != null)
			{
				var creditIds = await _creditRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync();
				foreach (var id in creditIds) if (counts.ContainsKey(id)) counts[id]++;
			}

			if (_docsRepo != null) foreach (var id in await _docsRepo.GetAll().Select(x => EF.Property<int>(x, "DealerId")).Distinct().ToListAsync()) if (counts.ContainsKey(id)) counts[id]++;

			return Ok(counts);
		}

		/// <summary>
		/// Returns only dealers that have submitted their basic info (PinCode is present).
		/// Used by the Dashboard — drafts without a PinCode are excluded.
		/// </summary>
		[HttpGet("submitted")]
		public async Task<IActionResult> GetSubmitted()
		{
			// A dealer counts as "submitted" if either:
			//  - it went through the full registration flow (PinCode collected), OR
			//  - it's a Terminated / Inactive-Terminated (restricted) flow, which
			//    intentionally skips Primary Location / PinCode collection, OR
			//  - it's a Department (entity type), which skips the full registration flow.
			var query = _repo.GetAllWithInactive()
				.Where(x => (x.PinCode != null && x.PinCode != "")
					|| x.Status == DealerStatus.Terminated
					|| (x.Status == DealerStatus.InActive && x.InactiveProposal == FutureBusinessProposal.Terminated)
					|| (x.Status == DealerStatus.InActive && x.InactiveProposal == FutureBusinessProposal.NotTraceable)
					|| x.DealerType == RegistrationDealerType.Department);

			var role = User.FindFirst(ClaimTypes.Role)?.Value;
			var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
			var regionClaim = User.FindFirst("spic:region_id")?.Value;
			var stateClaim = User.FindFirst("spic:state_id")?.Value;
			var hqClaim = User.FindFirst("spic:hq_id")?.Value;

			var isUnrestrictedRole =
				role == "Admin" || role == "CorporateAdmin" ||
				role == "Director" || role == "AVP";

			if (!isUnrestrictedRole)
			{
				if ((role == "SMD" || role == "SMM") && int.TryParse(stateClaim, out var stateId) && stateId > 0)
					query = query.Where(x => x.StateId == stateId);
				else if ((role == "RM" || role == "RMD") && int.TryParse(regionClaim, out var regionId) && regionId > 0)
					query = query.Where(x => x.Region == regionId);
				else if ((role == "MDO" || role == "JMDO" || role == "MO") && int.TryParse(hqClaim, out var hqId) && hqId > 0)
					query = query.Where(x => x.HQ == hqId);
				else
					query = query.Where(x => x.CreatedBy == userId);
			}

			// Return only the fields used by Dashboard. This prevents newly added
			// registration columns from breaking the existing-dealer list when a
			// database migration is pending. A new dealer has no DealerCode until
			// final approval, so the card type can be derived without reading the
			// new IsNewDealerRegistration database column.
			var dashboardDealers = await query
				.Select(x => new
				{
					x.Id,
					x.IsDealer,
					x.InSpic,
					x.InGreenStar,
					IsNewDealerRegistration = x.DealerCode == null || x.DealerCode == "",
					x.DealerCode,
					x.CreatedBy,
					x.SPICCode,
					x.GreenStarCode,
					x.StateId,
					x.Region,
					x.HQ,
					Status = (int)x.Status,
					x.InactiveProposal,
					x.FirmName,
					x.BusinessEntityType,
					x.EntityType,
					x.WholeSaleFertilizerLicenseNumber,
					x.RetailFertilizerLicenseNumber,
					x.PinCode,
					x.RMApproved,
					x.SMApproved,
					x.AVPApproved,
					x.IsSubmittedForReview,
					x.IsFinalAmountSettled,
					x.UpdatedAt
				})
				.ToListAsync();

			return Ok(dashboardDealers);
		}
	}
	[Route("api/[controller]")]
	public class DealerExperienceController(IGenericRepository<DealerExperience> repo) : GenericCrudController<DealerExperience>(repo);

	[Route("api/[controller]")]
	public class AnnualSaleDataLastFYController(IGenericRepository<AnnualSaleDataLastFYofDealerRegistration> repo) : GenericCrudController<AnnualSaleDataLastFYofDealerRegistration>(repo);

	[Route("api/[controller]")]
	public class DealerWarehouseFacilitiesController(IGenericRepository<DealerWarehouseFacilities> repo) : GenericCrudController<DealerWarehouseFacilities>(repo);

	[Route("api/[controller]")]
	public class DealerRailFacilitiesController(IGenericRepository<DealerRailFacilities> repo) : GenericCrudController<DealerRailFacilities>(repo);

	[Route("api/[controller]")]
	public class DealerPortFacilitiesController(IGenericRepository<DealerPortFacilities> repo) : GenericCrudController<DealerPortFacilities>(repo);

	[Route("api/[controller]")]
	public class DealerMarketDetailController(IGenericRepository<DealerMarketDetail> repo) : GenericCrudController<DealerMarketDetail>(repo);

	[Route("api/[controller]")]
	public class DealerCompaniesOperatingInAreaController
		: GenericCrudController<DealerCompaniesOperatingInArea>
	{
		private readonly IGenericRepository<DealerCompaniesOperatingInArea> _repo;

		public DealerCompaniesOperatingInAreaController(
			IGenericRepository<DealerCompaniesOperatingInArea> repo) : base(repo)
		{
			_repo = repo;
		}

		[HttpGet("dealer/{dealerId}/has-greenstar")]
		public async Task<IActionResult> HasGreenStar(int dealerId)
		{
			if (dealerId <= 0)
				return BadRequest(false);

			var hasGreenStar = await _repo.ExistsAsync(x =>
				EF.Property<int>(x, "DealerId") == dealerId &&
				EF.Property<string>(x, "CompaniesOperating") != null &&
				EF.Property<string>(x, "CompaniesOperating").ToUpper() == "GREEN STAR");

			return Ok(hasGreenStar);
		}

	}

	[Route("api/[controller]")]
	public class DealerOwnershipInfoController(IGenericRepository<DealerOwnershipInfo> repo) : GenericCrudController<DealerOwnershipInfo>(repo);

	[Route("api/[controller]")]
	public class PartnerFamilyDetailsController(IGenericRepository<PartnerFamilyDetails> repo) : GenericCrudController<PartnerFamilyDetails>(repo);

	[Route("api/[controller]")]
	public class PartnerOccupationController(IGenericRepository<PartnerOccupation> repo) : GenericCrudController<PartnerOccupation>(repo);

	[Route("api/[controller]")]
	public class SalesPlanningController(IGenericRepository<SalesPlanningInDealerRegistration> repo) : GenericCrudController<SalesPlanningInDealerRegistration>(repo);

	[Route("api/[controller]")]
	public class DealerAssetBankController(IGenericRepository<DealerAssetBank> repo) : GenericCrudController<DealerAssetBank>(repo);

	[Route("api/[controller]")]
	public class DealerAssetLandController(IGenericRepository<DealerAssetLand> repo) : GenericCrudController<DealerAssetLand>(repo);

	[Route("api/[controller]")]
	public class DealerAssetBuildingController(IGenericRepository<DealerAssetBuilding> repo) : GenericCrudController<DealerAssetBuilding>(repo);

	[Route("api/[controller]")]
	public class DealerLoanLiabilitiesController(IGenericRepository<DealerLoanLiabilities> repo) : GenericCrudController<DealerLoanLiabilities>(repo);

	[Route("api/[controller]")]
	public class DealerCreditLimitProposalController(IGenericRepository<DealerCreditLimitProposal> repo) : GenericCrudController<DealerCreditLimitProposal>(repo);

	[Route("api/[controller]")]
	public class DealerCreditLimitSalesPerformanceController(IGenericRepository<DealerCreditLimitSalesPerformance> repo) : GenericCrudController<DealerCreditLimitSalesPerformance>(repo);

	[Route("api/[controller]")]
	public class DealerRegistrationDocumentsController(IGenericRepository<DealerRegistrationDocuments> repo) : GenericCrudController<DealerRegistrationDocuments>(repo);

	[Route("api/[controller]")]
	public class DealerApprovalHistoryController(IGenericRepository<DealerApprovalHistory> repo) : GenericCrudController<DealerApprovalHistory>(repo);


}