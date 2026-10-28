using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.DTOs;
using System.Security.Claims;
using static SPIC.Core.Entities.EmployeeRegistration;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CSR2Controller : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        public CSR2Controller(AppDbContext db, IWebHostEnvironment env) { _db = db; _env = env; }

        // CSR-2 records are visible to the Responsible Person and to the workflow
        // validator/approver roles for the corresponding CSR-1 scope.
        private async Task<IQueryable<CSR1>> VisibleApprovedCSR1Async1()
        {
            var query = _db.Set<CSR1>()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Status == CSR1Status.Approved);

            if (User.IsInRole("Admin") ||
                User.IsInRole("SuperAdmin"))
            {
                return query;
            }

            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
                return query.Where(_ => false);

            // The CSR-1 Responsible Person must always be able to see/open the
            // corresponding CSR-2 so they can complete Tabs 1 & 2 and apply it.
            // Reviewer scope is OR-ed with this ownership rule; it never replaces it.
            var responsiblePredicate = query.Where(x =>
                x.ResponsiblePersonId == userId);

            var loginScope = await _db.Set<Employeelogin>()
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.StateId,
                    x.RegionId,
                    x.HeadquartersId
                })
                .FirstOrDefaultAsync();

            // A valid Responsible Person must not lose access only because their
            // Employeelogin scope values are incomplete.
            if (loginScope == null)
                return responsiblePredicate;

            // STATE workflow follows the CSR-1 applicant's organisation scope.
            // CSR-1 State flow: SMDO apply -> SMDO validate -> SMM approve.
            // SMD/SMDO are State workflow applicant/validator roles.
            if (User.IsInRole("SMD") ||
                User.IsInRole("SMDO"))
            {
                if (loginScope.StateId <= 0)
                    return responsiblePredicate;

                var stateId = loginScope.StateId;

                return query.Where(csr1 =>
                    csr1.ResponsiblePersonId == userId ||
                    _db.Set<Employeelogin>().Any(applicantLogin =>
                        applicantLogin.IsActive &&
                        applicantLogin.UserId == csr1.CreatedBy &&
                        applicantLogin.StateId == stateId));
            }

            // State approval role: SMM must see approved CSR-1 records in the same State.
            if (User.IsInRole("SMM"))
            {
                if (loginScope.StateId <= 0)
                    return responsiblePredicate;

                var stateId = loginScope.StateId;

                return query.Where(csr1 =>
                    csr1.ResponsiblePersonId == userId ||
                    _db.Set<Employeelogin>().Any(applicantLogin =>
                        applicantLogin.IsActive &&
                        applicantLogin.UserId == csr1.CreatedBy &&
                        applicantLogin.StateId == stateId));
            }

            // HQ workflow: MDO applies CSR-2, MO validates, RM/RMM approves.
            // Workflow visibility is Headquarters only.
            if (User.IsInRole("MDO") ||
                User.IsInRole("MO") ||
                User.IsInRole("RM") ||
                User.IsInRole("RMM"))
            {
                if (loginScope.HeadquartersId <= 0)
                    return responsiblePredicate;

                var hqId = loginScope.HeadquartersId;

                return query.Where(csr1 =>
                    csr1.ResponsiblePersonId == userId ||
                    _db.Set<Employeelogin>().Any(applicantLogin =>
                        applicantLogin.IsActive &&
                        applicantLogin.UserId == csr1.CreatedBy &&
                        applicantLogin.HeadquartersId == hqId));
            }

            if (User.IsInRole("RMDO") ||
                User.IsInRole("MDADMIN") ||
                User.IsInRole("MDHOD"))
            {
                return query;
            }

            return responsiblePredicate;
        }

        private async Task<IQueryable<CSR1>> VisibleApprovedCSR1Async()
        {
            var query = _db.Set<CSR1>()
                .AsNoTracking()
                .Where(x =>
                    x.IsActive &&
                    x.Status == CSR1Status.Approved);

            // IMPORTANT:
            // Use the same effective roles used by CSR-2 workflow approval.
            // Employeelogin role is primary; JWT role is fallback.
            var effectiveRoles = await GetEffectiveCSR2RolesAsync();

            // Admin / SuperAdmin can see all approved CSR-1 records.
            if (HasRole(effectiveRoles, "Admin", "SuperAdmin"))
                return query;

            var userId = CurrentUserId;

            if (string.IsNullOrWhiteSpace(userId))
                return query.Where(_ => false);

            // Responsible Person must always retain access to own CSR-1/CSR-2.
            var responsiblePredicate = query.Where(x =>
                x.ResponsiblePersonId == userId);

            // ============================================================
            // GLOBAL MULTI-LEVEL CSR-2 APPROVERS
            // ============================================================
            // MDADMIN:
            // Validated -> Level1Approved happens before this role.
            // MDADMIN must be able to see Level1Approved records for Level 2.
            //
            // MDHOD / CSMDHOD:
            // Must be able to see Level2Approved records for final Level 3.
            //
            // They are global approvers, therefore do NOT restrict by
            // State / Region / Headquarters here.
            // ============================================================
            if (HasRole(
                effectiveRoles,
                "MDADMIN",
                "MDHOD",
                "CSMDHOD",
                "CSMD-HOD"))
            {
                return query;
            }

            // Other support/global role retained from existing logic.
            if (HasRole(effectiveRoles, "RMDO"))
                return query;

            var loginScope = await _db.Set<Employeelogin>()
                .AsNoTracking()
                .Where(x =>
                    x.UserId == userId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.StateId,
                    x.RegionId,
                    x.HeadquartersId
                })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            // Responsible Person should not lose own record because
            // Employeelogin scope data is incomplete.
            if (loginScope == null)
                return responsiblePredicate;

            // ============================================================
            // STATE WORKFLOW
            // SMD / SMDO -> State records
            // ============================================================
            if (HasRole(effectiveRoles, "SMD", "SMDO"))
            {
                if (loginScope.StateId <= 0)
                    return responsiblePredicate;

                var stateId = loginScope.StateId;

                return query.Where(csr1 =>
                    csr1.ResponsiblePersonId == userId ||
                    _db.Set<Employeelogin>().Any(applicantLogin =>
                        applicantLogin.IsActive &&
                        applicantLogin.UserId == csr1.CreatedBy &&
                        applicantLogin.StateId == stateId));
            }

            // ============================================================
            // STATE APPROVER
            // SMM -> same State
            // ============================================================
            if (HasRole(effectiveRoles, "SMM"))
            {
                if (loginScope.StateId <= 0)
                    return responsiblePredicate;

                var stateId = loginScope.StateId;

                return query.Where(csr1 =>
                    csr1.ResponsiblePersonId == userId ||
                    _db.Set<Employeelogin>().Any(applicantLogin =>
                        applicantLogin.IsActive &&
                        applicantLogin.UserId == csr1.CreatedBy &&
                        applicantLogin.StateId == stateId));
            }

            // ============================================================
            // HQ WORKFLOW
            // MDO -> MO -> RM/RMM
            // ============================================================
            if (HasRole(
                effectiveRoles,
                "MDO",
                "MO",
                "RM",
                "RMM"))
            {
                if (loginScope.HeadquartersId <= 0)
                    return responsiblePredicate;

                var headquartersId = loginScope.HeadquartersId;

                return query.Where(csr1 =>
                    csr1.ResponsiblePersonId == userId ||
                    _db.Set<Employeelogin>().Any(applicantLogin =>
                        applicantLogin.IsActive &&
                        applicantLogin.UserId == csr1.CreatedBy &&
                        applicantLogin.HeadquartersId == headquartersId));
            }

            return responsiblePredicate;
        }

        [HttpGet("assigned-programs")]
        public async Task<IActionResult> GetAssignedPrograms()
        {
            var visibleCsr1 = await VisibleApprovedCSR1Async();

            var records = await visibleCsr1
                .OrderByDescending(x => x.Id)
                .Select(x => new
                {
                    CSR1Id = x.Id,
                    x.ProgramId,
                    x.BudgetProgramId,
                    x.ProgramMonth,
                    x.ProgramYear,
                    x.NumberOfPrograms,
                    ApprovedBudget = x.Budget,
                    SanctionNumber = x.SanctionNo,
                    ProgramDate = x.CSR1Date,
                    x.LocationId,
                    x.HeadquarterId,
                    x.ResponsiblePersonId,
                    CSR1Status = x.Status
                }).ToListAsync();
            var ids = records.Select(x => x.ProgramId).Distinct().ToList();
            var masters = await _db.Set<ProgramMaster>()
                .AsNoTracking().Where(x => ids.Contains(x.Id))
                .Select(x => new { x.Id, ProgramName = x.Name, ProgramType = x.ProgramType.Name })
                .ToDictionaryAsync(x => x.Id);
            return Ok(records.Select(x =>
            {
                masters.TryGetValue(x.ProgramId, out var master);
                return new
                {
                    x.CSR1Id,
                    x.ProgramId,
                    x.BudgetProgramId,
                    x.ProgramMonth,
                    x.ProgramYear,
                    x.NumberOfPrograms,
                    x.ApprovedBudget,
                    x.SanctionNumber,
                    x.ProgramDate,
                    x.LocationId,
                    x.HeadquarterId,
                    x.ResponsiblePersonId,
                    CSR1Status = x.CSR1Status.ToString(),
                    ProgramName = master?.ProgramName ?? "",
                    ProgramType = master?.ProgramType ?? ""
                };
            }));
        }

        [HttpGet("assigned-programs/{csr1Id:int}")]
        public async Task<IActionResult> GetProgramDetails(int csr1Id)
        {
            var visibleCsr1 = await VisibleApprovedCSR1Async();
            var csr1 = await visibleCsr1.FirstOrDefaultAsync(x => x.Id == csr1Id);
            if (csr1 == null) return NotFound(new { message = "Approved CSR1 record not found." });
            var master = await _db.Set<ProgramMaster>().AsNoTracking()
                .Where(x => x.Id == csr1.ProgramId)
                .Select(x => new { ProgramName = x.Name, ProgramType = x.ProgramType.Name })
                .FirstOrDefaultAsync();
            var focus1 = await _db.Set<CSR1Products1>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            var focus2 = await _db.Set<CSR1Products2>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            var focus3 = await _db.Set<CSR1Products3>().AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id && x.IsActive)
                .Select(x => new { x.CropId, x.ProductId }).ToListAsync();
            // Show every active expense master in CSR-2.
            // If CSR-1 has a matching CSR1Expenses row, use its amount; otherwise planned amount = 0.
            var plannedExpenses = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(masterExpense => masterExpense.IsActive)
                .OrderBy(masterExpense => masterExpense.Id)
                .Select(masterExpense => new
                {
                    ExpenseId = masterExpense.Id,
                    ExpenseName = masterExpense.Name,
                    ExpenseAmount = _db.Set<CSR1Expenses>()
                        .Where(e =>
                            e.CSR1Id == csr1Id &&
                            e.IsActive &&
                            e.ExpenseId == masterExpense.Id)
                        .Sum(e => (decimal?)e.ExpenseAmount) ?? 0m
                })
                .ToListAsync();

            return Ok(new
            {
                CSR1Id = csr1.Id,
                csr1.ProgramId,
                ProgramName = master?.ProgramName ?? "",
                ProgramType = master?.ProgramType ?? "",
                csr1.BudgetProgramId,
                csr1.ProgramMonth,
                csr1.ProgramYear,
                csr1.NumberOfPrograms,
                csr1.Budget,
                csr1.SanctionNo,
                csr1.CSR1Date,
                csr1.HeadquarterId,
                csr1.LocationId,
                csr1.ResponsiblePersonId,
                Status = csr1.Status.ToString(),
                Expenses = plannedExpenses,
                Focus1 = focus1,
                Focus2 = focus2,
                Focus3 = focus3
            });
        }

        [HttpGet("participant-mobile-search")]
        public async Task<IActionResult> SearchParticipantMobile([FromQuery] string? term)
        {
            var search = new string((term ?? string.Empty)
                .Where(char.IsDigit)
                .Take(15)
                .ToArray());

            if (search.Length < 3)
                return Ok(new List<ParticipantMobileLookupResult>());

            var farmers = await _db.Set<SasFarmer>()
                .AsNoTracking()
                .Where(x => !x.IsDeleted &&
                            x.Mobile != null &&
                            x.Mobile.Contains(search))
                .OrderBy(x => x.Mobile)
                .Take(20)
                .Select(x => new ParticipantMobileLookupResult
                {
                    SourceId = x.Id,
                    Mobile = x.Mobile,
                    Name = x.Name ?? string.Empty,
                    Role = "Farmer",
                    Source = "SasFarmers"
                })
                .ToListAsync(HttpContext.RequestAborted);

            var dealerRows = await _db.Set<DealerRegistration>()
                .AsNoTracking()
                .Where(x =>
                    (x.OfficialContactNumber != null &&
                     x.OfficialContactNumber.Contains(search)) ||
                    (x.AlternativeNumber != null &&
                     x.AlternativeNumber.Contains(search)))
                .OrderBy(x => x.FirmName)
                .Take(30)
                .Select(x => new
                {
                    x.Id,
                    x.FirmName,
                    x.OfficialContactNumber,
                    x.AlternativeNumber
                })
                .ToListAsync(HttpContext.RequestAborted);

            var dealers = dealerRows
                .SelectMany(x => new[]
                {
                    new ParticipantMobileLookupResult
                    {
                        SourceId = x.Id,
                        Mobile = x.OfficialContactNumber ?? string.Empty,
                        Name = x.FirmName ?? string.Empty,
                        Role = "Dealer",
                        Source = "DealerRegistrations"
                    },
                    new ParticipantMobileLookupResult
                    {
                        SourceId = x.Id,
                        Mobile = x.AlternativeNumber ?? string.Empty,
                        Name = x.FirmName ?? string.Empty,
                        Role = "Dealer",
                        Source = "DealerRegistrations"
                    }
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Mobile) &&
                            x.Mobile.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var result = farmers
                .Concat(dealers)
                .GroupBy(x => new
                {
                    x.SourceId,
                    Mobile = x.Mobile.Trim(),
                    x.Role
                })
                .Select(g => g.First())
                .OrderBy(x => x.Mobile.StartsWith(search, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Mobile)
                .ThenBy(x => x.Role)
                .Take(20)
                .ToList();

            return Ok(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> All()
        {
            var people = await _db.Set<EmployeeInformation>()
                .AsNoTracking()
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.OfficialPhoneNumber,
                    e.PersonalPhoneNumber
                })
                .ToListAsync();

            // Get Employeelogin + UserName from Users table
            var logins = await (
                from login in _db.Set<Employeelogin>().AsNoTracking()
                join user in _db.Users.AsNoTracking()
                    on login.UserId equals user.Id
                where login.IsActive
                select new
                {
                    login.EmployeeInformationID,
                    login.UserId,
                    user.UserName,
                    login.Role
                }
            ).ToListAsync();

            var loginByEmployee = logins
                .GroupBy(x => x.EmployeeInformationID)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        UserId = g
                            .Select(x => x.UserId)
                            .FirstOrDefault(x =>
                                !string.IsNullOrWhiteSpace(x)),

                        UserName = g
                            .Select(x => x.UserName)
                            .FirstOrDefault(x =>
                                !string.IsNullOrWhiteSpace(x)),

                        Roles = g
                            .Select(x => x.Role.ToString())
                            .Distinct()
                            .OrderBy(role => role)
                            .ToList()
                    });

            var result = people
                .Select(e =>
                {
                    loginByEmployee.TryGetValue(
                        e.Id,
                        out var login);

                    return new
                    {
                        e.Id,
                        e.Name,
                        e.OfficialPhoneNumber,
                        e.PersonalPhoneNumber,

                        // Employeelogins.UserId
                        UserId = login?.UserId,

                        // Users.UserName
                        UserName = login?.UserName ?? "",

                        Roles = login?.Roles
                            ?? new List<string>()
                    };
                })
                .OrderBy(e => e.Name)
                .ToList();

            return Ok(result);
        }

        // CSR-2 persistence endpoints (merged from previous package)
        // Expense categories are dynamic and are validated against CSR1Expenses/ExpenseInformations.
        private string Actor => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "unknown";
        private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        private string UploadRoot => Path.Combine(_env.ContentRootPath, "App_Data", "csr2-private");

        // CSR-2 workflow permissions use the active employee login role first.
        // JWT/AppUsers role is retained only as a compatibility fallback.
        private async Task<HashSet<string>> GetEffectiveCSR2RolesAsync()
        {
            var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(CurrentUserId))
            {
                var employeeRoles = await _db.Set<Employeelogin>()
                    .AsNoTracking()
                    .Where(x => x.IsActive && x.UserId == CurrentUserId)
                    .Select(x => x.Role.ToString())
                    .Distinct()
                    .ToListAsync(HttpContext.RequestAborted);

                foreach (var role in employeeRoles)
                    if (!string.IsNullOrWhiteSpace(role))
                        roles.Add(role);
            }

            foreach (var claim in User.FindAll(ClaimTypes.Role))
                if (!string.IsNullOrWhiteSpace(claim.Value))
                    roles.Add(claim.Value);

            return roles;
        }

        private static bool HasRole(IReadOnlySet<string> effectiveRoles, params string[] roles) =>
            roles.Any(effectiveRoles.Contains);

        private bool IsResponsiblePerson(CSR1 csr1) =>
            !string.IsNullOrWhiteSpace(CurrentUserId) &&
            string.Equals(csr1.ResponsiblePersonId, CurrentUserId, StringComparison.OrdinalIgnoreCase);

        private async Task<string> ResolveCSR2ScopeAsync(CSR1 csr1)
        {
            // Resolve workflow scope from the CSR-1 applicant's Employeelogin role.
            // Do not infer State/HQ from HeadquarterId because State CSR-1 records
            // may also carry a HeadquarterId.
            if (!string.IsNullOrWhiteSpace(csr1.CreatedBy))
            {
                var creatorRoles = await _db.Set<Employeelogin>()
                    .AsNoTracking()
                    .Where(x => x.UserId == csr1.CreatedBy)
                    .Select(x => x.Role.ToString())
                    .Distinct()
                    .ToListAsync(HttpContext.RequestAborted);

                if (creatorRoles.Any(role =>
                        string.Equals(role, "SMD", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "SMDO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "SMM", StringComparison.OrdinalIgnoreCase)))
                {
                    return "State";
                }

                if (creatorRoles.Any(role =>
                        string.Equals(role, "MDO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "JMDO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "RMDO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "MO", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "RM", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "RMM", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(role, "RMD", StringComparison.OrdinalIgnoreCase)))
                {
                    return "HQ";
                }
            }

            // If a historical CSR-1 creator no longer has an Employeelogin row,
            // infer the workflow family from the current authorized workflow role.
            // This avoids misclassifying State CSR-1 records merely because they also
            // contain a HeadquarterId.
            var currentRoles = await GetEffectiveCSR2RolesAsync();
            if (HasRole(currentRoles, "SMD", "SMDO", "SMM"))
                return "State";
            if (HasRole(currentRoles, "MDO", "JMDO", "RMDO", "MO", "RM", "RMM", "RMD"))
                return "HQ";

            // Legacy last-resort fallback for roles with no State/HQ identity
            // (for example MDADMIN/MDHOD). Normal workflow reviewers should have
            // been resolved above from the CSR-1 creator role.
            return csr1.HeadquarterId > 0 ? "HQ" : "State";
        }

        private async Task<int> GetCSR2CutoffDayAsync()
        {
            return await _db.Set<CSRExceedingDaySetting>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderByDescending(x => x.Id)
                .Select(x => (int?)x.CSR2Day)
                .FirstOrDefaultAsync(HttpContext.RequestAborted) ?? 0;
        }

        private async Task<bool> RequiresCSR2MultiLevelApprovalAsync(CSR2? report = null)
        {
            var cutoffDay = await GetCSR2CutoffDayAsync();
            if (cutoffDay <= 0)
                return false;

            // CSR2Day ONLY selects the workflow type.
            // It does not grant/revoke role permissions.
            //
            // Draft/not-yet-created CSR-2 has no submission date yet.
            // Multi-level is selected only after Submit stamps SubmittedAt.
            if (report == null || report.Status == CSR2Status.Draft)
                return false;

            // Once submitted, permanently use the actual submission date.
            // If an old/incomplete row has no SubmittedAt, keep it in the normal workflow
            // instead of incorrectly switching it based on today's date.
            if (!report.SubmittedAt.HasValue)
                return false;

            return report.SubmittedAt.Value.Day > cutoffDay;
        }

        [HttpGet("workflow-mode/{csr1Id:int}")]
        public async Task<IActionResult> GetWorkflowMode(int csr1Id)
        {
            var csr1 = await ApprovedCsr1Async(csr1Id);
            if (csr1 == null)
                return NotFound("Approved CSR1 not available or access denied.");

            var report = await _db.Set<CSR2>()
                .AsNoTracking()
                .Where(x => x.CSR1Id == csr1Id)
                .Select(x => new CSR2
                {
                    Id = x.Id,
                    CSR1Id = x.CSR1Id,
                    SubmittedAt = x.SubmittedAt,
                    Status = x.Status
                })
                .FirstOrDefaultAsync(HttpContext.RequestAborted);

            var cutoffDay = await GetCSR2CutoffDayAsync();
            var referenceDay = report?.SubmittedAt?.Day ?? DateTime.Now.Day;
            var requiresMultiLevelApproval = await RequiresCSR2MultiLevelApprovalAsync(report);
            var scope = await ResolveCSR2ScopeAsync(csr1);
            var isState = string.Equals(scope, "State", StringComparison.OrdinalIgnoreCase);
            var isHq = string.Equals(scope, "HQ", StringComparison.OrdinalIgnoreCase);
            var effectiveRoles = await GetEffectiveCSR2RolesAsync();

            var canApply = report == null || report.Status == CSR2Status.Draft
                ? await CanCreateCSR2DraftAsync(csr1)
                : false;

            // Validation is mandatory in BOTH normal and exceeded-date workflows.
            // The approved CSR-1 visibility check above already enforces record access.
            // For the Submitted review stage, use the validator role as the final action
            // authority so legacy/incomplete scope metadata cannot hide a valid action.
            var canValidate = report?.Status == CSR2Status.Submitted &&
                              (HasRole(effectiveRoles, "SMD", "SMDO") ||
                               (isHq && HasRole(effectiveRoles, "MO")));

            var canApprove = false;
            var actionLabel = string.Empty;
            var isResponsiblePerson = IsResponsiblePerson(csr1);

            if (report != null && !isResponsiblePerson)
            {
                if (requiresMultiLevelApproval)
                {
                    // Exceeded-date workflow still validates first.
                    // Level 1 approval starts only after Status becomes Validated.
                    if (report.Status == CSR2Status.Validated)
                    {
                        canApprove = isState
                            ? HasRole(effectiveRoles, "SMM")
                            : isHq && HasRole(effectiveRoles, "RMM");
                        if (canApprove) actionLabel = "Approve Level 1";
                    }
                    else if (report.Status == CSR2Status.Level1Approved)
                    {
                        canApprove = HasRole(effectiveRoles, "MDADMIN");
                        if (canApprove) actionLabel = "Approve Level 2";
                    }
                    else if (report.Status == CSR2Status.Level2Approved)
                    {
                        canApprove = HasRole(effectiveRoles, "CSMDHOD", "CSMD-HOD", "MDHOD");
                        if (canApprove) actionLabel = "Approve Level 3";
                    }
                }
                else if (report.Status == CSR2Status.Validated)
                {
                    canApprove = isState
                        ? HasRole(effectiveRoles, "SMM")
                        : isHq && HasRole(effectiveRoles, "RM");
                    if (canApprove) actionLabel = "Approve CSR-2";
                }
            }

            if (canValidate)
                actionLabel = "Validate CSR-2";
            else if (canApply)
                actionLabel = "Apply CSR-2";

            return Ok(new
            {
                CSR2Day = cutoffDay,
                ReferenceDay = referenceDay,
                RequiresMultiLevelApproval = requiresMultiLevelApproval,
                Scope = scope,
                Status = report?.Status.ToString() ?? CSR2Status.Draft.ToString(),
                CanApply = canApply,
                CanValidate = canValidate,
                CanApprove = canApprove,
                ActionLabel = actionLabel
            });
        }

        private async Task<bool> CanCreateCSR2DraftAsync(CSR1 csr1)
        {
            // Responsible Person can apply in both normal and exceeded-date flows.
            if (IsResponsiblePerson(csr1))
                return true;

            var scope = await ResolveCSR2ScopeAsync(csr1);
            var effectiveRoles = await GetEffectiveCSR2RolesAsync();
            var cutoffDay = await GetCSR2CutoffDayAsync();
            var isExceededDate = cutoffDay > 0 && DateTime.Now.Day > cutoffDay;

            // Normal flow:
            // State -> SMD/SMDO apply
            // HQ    -> MDO apply
            if (!isExceededDate)
            {
                if (string.Equals(scope, "State", StringComparison.OrdinalIgnoreCase))
                    return HasRole(effectiveRoles, "SMD", "SMDO");

                if (string.Equals(scope, "HQ", StringComparison.OrdinalIgnoreCase))
                    return HasRole(effectiveRoles, "MDO");

                return false;
            }

            // Exceeded-date flow: MO, SMD/SMDO and RMDO may apply
            // (subject to the existing CSR-1 visibility/scope authorization).
            return HasRole(effectiveRoles, "MO", "SMD", "SMDO", "RMDO");
        }

        private async Task<bool> CanEditCSR2Async(CSR1 csr1, CSR2 report)
        {
            // Existing Draft behavior remains unchanged.
            if (report.Status == CSR2Status.Draft)
                return await CanCreateCSR2DraftAsync(csr1);

            // Same review pattern as CSR-1: the current validator may edit/save
            // the Submitted record before Validate. ApprovedCsr1Async has already
            // enforced per-record visibility, so do not reject a valid SMDO/SMD
            // merely because legacy CSR-1 scope metadata resolves incorrectly.
            if (report.Status == CSR2Status.Submitted)
            {
                var effectiveRoles = await GetEffectiveCSR2RolesAsync();

                if (HasRole(effectiveRoles, "SMD", "SMDO"))
                    return true;

                var scope = await ResolveCSR2ScopeAsync(csr1);
                if (string.Equals(scope, "HQ", StringComparison.OrdinalIgnoreCase) &&
                    HasRole(effectiveRoles, "MO"))
                    return true;
            }

            return false;
        }

        // IMPORTANT: Replace this with the SAME scope/claims authorization policy as your
        // CSR1 details endpoint. Being logged in alone MUST NOT grant access to every CSR1.
        private async Task<CSR1?> ApprovedCsr1Async(int csr1Id)
        {
            var visibleCsr1 = await VisibleApprovedCSR1Async();
            return await visibleCsr1.FirstOrDefaultAsync(x => x.Id == csr1Id);
        }

        private static CSR2FocusGroupDto ToFocusGroupDto(CSR2Products1? source) => new()
        {
            CropId = source?.CropId ?? Array.Empty<int>(),
            ProductId = source?.ProductId ?? Array.Empty<int>()
        };

        private static CSR2FocusGroupDto ToFocusGroupDto(CSR2Products2? source) => new()
        {
            CropId = source?.CropId ?? Array.Empty<int>(),
            ProductId = source?.ProductId ?? Array.Empty<int>()
        };

        private static CSR2FocusGroupDto ToFocusGroupDto(CSR2Products3? source) => new()
        {
            CropId = source?.CropId ?? Array.Empty<int>(),
            ProductId = source?.ProductId ?? Array.Empty<int>()
        };

        private static (int[] Crops, int[] Products) NormalizeFocusGroup(CSR2FocusGroupDto? source)
        {
            var crops = (source?.CropId ?? Array.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .Take(3)
                .ToArray();

            var products = (source?.ProductId ?? Array.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .Take(3)
                .ToArray();

            return (crops, products);
        }

        [HttpGet("by-csr1/{csr1Id:int}")]
        public async Task<IActionResult> GetByCsr1(int csr1Id)
        {
            if (await ApprovedCsr1Async(csr1Id) == null)
                return NotFound("Approved CSR1 not available or access denied.");

            var report = await _db.Set<CSR2>()
                .AsNoTracking()
                .Include(x => x.Participants)
                .Include(x => x.Employees)
                .Include(x => x.Media)
                .Include(x => x.Products1)
                .Include(x => x.Products2)
                .Include(x => x.Products3)
                .Include(x => x.Expenses)
                    .ThenInclude(x => x.Proofs)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    x => x.CSR1Id == csr1Id,
                    HttpContext.RequestAborted);

            if (report == null)
                return NotFound();


            // =========================================================
            // PARTICIPANT DISPLAY DETAILS
            // =========================================================

            var participantDtos = new List<CSR2ParticipantDto>();

            foreach (var participant in report.Participants)
            {
                // For manually entered Other participants, Name and PhoneNumber are
                // stored directly in CSR2Participants. Farmer/Dealer continue to
                // resolve display details from their existing master tables.
                string? name = string.Equals(participant.Role, "Other", StringComparison.OrdinalIgnoreCase)
                    ? participant.Name
                    : null;
                string? phone = string.Equals(participant.Role, "Other", StringComparison.OrdinalIgnoreCase)
                    ? participant.PhoneNumber
                    : null;

                if (participant.ParticipantSourceId.HasValue)
                {
                    if (string.Equals(
                            participant.ParticipantSource,
                            "SasFarmers",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        var farmer = await _db.Set<SasFarmer>()
                            .AsNoTracking()
                            .Where(x =>
                                x.Id == participant.ParticipantSourceId.Value &&
                                !x.IsDeleted)
                            .Select(x => new
                            {
                                Name = x.Name,
                                Phone = x.Mobile
                            })
                            .FirstOrDefaultAsync();

                        name = farmer?.Name;
                        phone = farmer?.Phone;
                    }
                    else if (string.Equals(
                                 participant.ParticipantSource,
                                 "DealerRegistrations",
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        var dealer = await _db.Set<DealerRegistration>()
                            .AsNoTracking()
                            .Where(x =>
                                x.Id == participant.ParticipantSourceId.Value)
                            .Select(x => new
                            {
                                Name = x.FirmName,
                                Phone = x.OfficialContactNumber
                            })
                            .FirstOrDefaultAsync();

                        name = dealer?.Name;
                        phone = dealer?.Phone;
                    }
                }

                participantDtos.Add(new CSR2ParticipantDto
                {
                    ParticipantSourceId =
                        participant.ParticipantSourceId,

                    ParticipantSource =
                        participant.ParticipantSource,

                    Role =
                        participant.Role,

                    Name =
                        name,

                    PhoneNumber =
                        phone
                });
            }


            // =========================================================
            // EMPLOYEE DISPLAY DETAILS
            // =========================================================

            var employeeDtos = new List<CSR2EmployeeDto>();

            foreach (var item in report.Employees)
            {
                var employeeDetails = await (
                    from login in _db.Set<Employeelogin>().AsNoTracking()

                    join employee in _db.Set<EmployeeInformation>().AsNoTracking()
                        on login.EmployeeInformationID equals employee.Id

                    where login.UserId == item.EmployeeInformationId

                    select new
                    {
                        employee.Name,

                        PhoneNumber =
                            employee.OfficialPhoneNumber
                            ?? employee.PersonalPhoneNumber
                    }
                ).FirstOrDefaultAsync();

                employeeDtos.Add(new CSR2EmployeeDto
                {
                    EmployeeInformationId =
                        item.EmployeeInformationId,

                    RoleName =
                        item.RoleName,

                    EmployeeName =
                        employeeDetails?.Name,

                    PhoneNumber =
                        employeeDetails?.PhoneNumber
                });
            }


            // =========================================================
            // EXPENSE MASTER NAMES
            // =========================================================

            var expenseInformationIds = report.Expenses
                .Select(x => x.ExpenseInformationId)
                .Distinct()
                .ToList();

            var expenseNames = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(x => expenseInformationIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.Name);


            // =========================================================
            // RESPONSE
            // =========================================================

            return Ok(new CSR2DetailsDto
            {
                Id = report.Id,

                CSR1Id = report.CSR1Id,

                Status = report.Status.ToString(),

                ActualEventDate =
                    report.ActualEventDate,

                StartTime =
                    report.StartTime,

                EndTime =
                    report.EndTime,

                EventCoordinatorEmployeeId =
                    report.EventCoordinatorEmployeeId,

                ActualLocation =
                    report.ActualLocation,

                ExpectedParticipantsCount =
                    report.ExpectedParticipantsCount,

                Remarks =
                    report.Remarks,


                Participants =
                    participantDtos,


                Employees =
                    employeeDtos,


                Products1 = ToFocusGroupDto(report.Products1.FirstOrDefault()),
                Products2 = ToFocusGroupDto(report.Products2.FirstOrDefault()),
                Products3 = ToFocusGroupDto(report.Products3.FirstOrDefault()),


                Expenses =
                    report.Expenses
                        .OrderBy(x => x.ExpenseInformationId)
                        .ThenBy(x => x.Id)
                        .Select(x => new CSR2ExpenseDto
                        {
                            Id = x.Id,

                            ExpenseInformationId =
                                x.ExpenseInformationId,

                            ExpenseName =
                                expenseNames.TryGetValue(
                                    x.ExpenseInformationId,
                                    out var expenseName)
                                        ? expenseName
                                        : "",

                            ActualAmount =
                                x.ActualAmount,

                            ExpenseDate =
                                x.ExpenseDate,

                            BillNo =
                                x.BillNo,

                            Proofs =
                                x.Proofs
                                    .OrderBy(p => p.Id)
                                    .Select(p => new CSR2ExpenseProofDto
                                    {
                                        Id = p.Id,

                                        OriginalFileName =
                                            p.OriginalFileName
                                    })
                                    .ToList()
                        })
                        .ToList(),


                Media =
                    report.Media
                        .Select(x => new CSR2MediaDto
                        {
                            Id = x.Id,

                            MediaType =
                                x.MediaType,

                            OriginalFileName =
                                x.OriginalFileName,

                            Caption =
                                x.Caption,

                            FileSizeBytes =
                                x.FileSizeBytes
                        })
                        .ToList()
            });
        }

        [HttpPost("save-draft")]
        public async Task<IActionResult> SaveDraft([FromBody] CSR2SaveDto dto)
        {
            var csr1 = await ApprovedCsr1Async(dto.CSR1Id);
            if (csr1 == null)
                return NotFound("Approved CSR1 not available or access denied.");

            if (dto.EndTime.HasValue && dto.StartTime.HasValue && dto.EndTime <= dto.StartTime)
                return BadRequest("Event end time must be later than start time.");

            if (string.IsNullOrWhiteSpace(dto.EventCoordinatorEmployeeId))
                return BadRequest("Event Coordinator is required.");

            if (dto.ExpectedParticipantsCount < 0)
                return BadRequest("Invalid participant count.");

            if (dto.Expenses.Any(x => x.ActualAmount < 0))
                return BadRequest("Expense amount cannot be negative.");

            // CSR-2 focus groups mirror CSR-1: Products1, Products2 and Products3.
            var focus1 = NormalizeFocusGroup(dto.Products1);
            var focus2 = NormalizeFocusGroup(dto.Products2);
            var focus3 = NormalizeFocusGroup(dto.Products3);

            if (focus1.Crops.Length == 0 || focus1.Products.Length == 0)
                return BadRequest("Focus Group 1 requires at least one crop and one product.");

            if (focus1.Crops.Length > 3 || focus1.Products.Length > 3 ||
                focus2.Crops.Length > 3 || focus2.Products.Length > 3 ||
                focus3.Crops.Length > 3 || focus3.Products.Length > 3)
            {
                return BadRequest("Maximum 3 crops and 3 products are allowed in each focus group.");
            }

            if ((focus2.Crops.Length > 0 && focus2.Products.Length == 0) ||
                (focus2.Crops.Length == 0 && focus2.Products.Length > 0))
            {
                return BadRequest("Focus Group 2 must contain both crop and product selections.");
            }

            if ((focus3.Crops.Length > 0 && focus3.Products.Length == 0) ||
                (focus3.Crops.Length == 0 && focus3.Products.Length > 0))
            {
                return BadRequest("Focus Group 3 must contain both crop and product selections.");
            }

            // Expenses are driven dynamically by ExpenseInformations.
            // Duplicate ExpenseInformationId values are intentionally allowed because
            // one expense category can contain multiple bill/expense rows.
            var activeExpenseIds = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToListAsync(HttpContext.RequestAborted);

            if (dto.Expenses
                .Where(x => x.ActualAmount.HasValue && x.ActualAmount.Value > 0)
                .Any(x =>
                    x.ExpenseInformationId <= 0 ||
                    !activeExpenseIds.Contains(x.ExpenseInformationId)))
            {
                return BadRequest("Invalid expense. Expense must come from active ExpenseInformations.");
            }

            if (dto.Participants.Any(x => string.IsNullOrWhiteSpace(x.Role)))
                return BadRequest("Participant role is required.");

            if (dto.Participants.Any(x =>
                    !string.IsNullOrWhiteSpace(x.ParticipantSource) &&
                    !string.Equals(x.ParticipantSource, "Other", StringComparison.OrdinalIgnoreCase) &&
                    (!x.ParticipantSourceId.HasValue || x.ParticipantSourceId.Value <= 0)))
            {
                return BadRequest("Participant master ID is required.");
            }

            if (dto.Employees.Any(x =>
                    string.IsNullOrWhiteSpace(x.EmployeeInformationId) ||
                    string.IsNullOrWhiteSpace(x.RoleName)))
            {
                return BadRequest("Required employee information is missing.");
            }

            var report = await _db.Set<CSR2>()
                .Include(x => x.Participants)
                .Include(x => x.Employees)
                .Include(x => x.Expenses)
                    .ThenInclude(x => x.Proofs)
                .Include(x => x.Products1)
                .Include(x => x.Products2)
                .Include(x => x.Products3)
                .AsSplitQuery()
                .FirstOrDefaultAsync(x => x.CSR1Id == dto.CSR1Id, HttpContext.RequestAborted);

            if (report != null && dto.Id.HasValue && dto.Id.Value != report.Id)
                return Conflict("Report ID mismatch.");

            if (report == null && !await CanCreateCSR2DraftAsync(csr1))
                return StatusCode(StatusCodes.Status403Forbidden, "You are not allowed to create this CSR-2 draft.");

            if (report != null)
            {
                var canEdit = await CanEditCSR2Async(csr1, report);

                if (!canEdit)
                {
                    var scope = await ResolveCSR2ScopeAsync(csr1);
                    var roles = await GetEffectiveCSR2RolesAsync();

                    return StatusCode(
                        StatusCodes.Status403Forbidden,
                        $"CSR-2 edit denied. " +
                        $"CSR2Id={report.Id}; " +
                        $"CSR1Id={report.CSR1Id}; " +
                        $"Status={report.Status}; " +
                        $"Scope={scope}; " +
                        $"Roles=[{string.Join(",", roles)}]; " +
                        $"UserId={CurrentUserId}");
                }
            }

            await using var tx = await _db.Database.BeginTransactionAsync(HttpContext.RequestAborted);
            try
            {
                if (report == null)
                {
                    report = new CSR2
                    {
                        CSR1Id = dto.CSR1Id,
                        CreatedBy = Actor,
                        CreatedAt = DateTime.UtcNow,
                        Status = CSR2Status.Draft
                    };

                    _db.Set<CSR2>().Add(report);
                    // Generate CSR2.Id before child rows/proof uploads are created.
                    await _db.SaveChangesAsync(HttpContext.RequestAborted);
                }

                report.ActualEventDate = dto.ActualEventDate ?? csr1.CSR1Date;
                report.StartTime = dto.StartTime;
                report.EndTime = dto.EndTime;
                report.EventCoordinatorEmployeeId = string.IsNullOrWhiteSpace(dto.EventCoordinatorEmployeeId)
                    ? null
                    : dto.EventCoordinatorEmployeeId.Trim();
                report.ActualLocation = string.IsNullOrWhiteSpace(dto.ActualLocation)
                    ? null
                    : dto.ActualLocation.Trim();
                report.ExpectedParticipantsCount = dto.ExpectedParticipantsCount;
                report.Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? null : dto.Remarks.Trim();
                report.UpdatedAt = DateTime.UtcNow;
                report.UpdatedBy = Actor;

                // Farmer / Dealer participants keep their master linkage and their
                // display Name / Phone are resolved from the source master.
                // Manual/Other participants persist Name + PhoneNumber directly.
                //
                // IMPORTANT: Apply CSR-2 calls SaveDraft once more before Submit.
                // If that second UI payload omits manual Name/Phone, do not erase values
                // that were already saved in CSR2Participants during the earlier draft save.
                var existingManualParticipants = report.Participants
                    .Where(p =>
                        !p.ParticipantSourceId.HasValue ||
                        p.ParticipantSourceId.Value <= 0 ||
                        string.Equals(p.ParticipantSource, "Other", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Role, "Other", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(p => p.Id)
                    .Select(p => new
                    {
                        Name = p.Name,
                        PhoneNumber = p.PhoneNumber
                    })
                    .ToList();

                if (report.Participants.Count > 0)
                    _db.Set<CSR2Participant>().RemoveRange(report.Participants);

                var manualParticipantIndex = 0;

                report.Participants = dto.Participants
                    .Select(x =>
                    {
                        var role = (x.Role ?? string.Empty).Trim();
                        var source = (x.ParticipantSource ?? string.Empty).Trim();

                        var isMasterParticipant =
                            x.ParticipantSourceId.HasValue &&
                            x.ParticipantSourceId.Value > 0 &&
                            (string.Equals(source, "SasFarmers", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(source, "DealerRegistrations", StringComparison.OrdinalIgnoreCase));

                        if (isMasterParticipant)
                        {
                            return new CSR2Participant
                            {
                                CSR2Id = report.Id,
                                ParticipantSourceId = x.ParticipantSourceId,
                                ParticipantSource = source,
                                Role = string.IsNullOrWhiteSpace(role) ? "Other" : role,
                                Name = null,
                                PhoneNumber = null,
                                CreatedAt = DateTime.UtcNow
                            };
                        }

                        var existingManual = manualParticipantIndex < existingManualParticipants.Count
                            ? existingManualParticipants[manualParticipantIndex]
                            : null;
                        manualParticipantIndex++;

                        var manualName = !string.IsNullOrWhiteSpace(x.Name)
                            ? x.Name.Trim()
                            : existingManual?.Name;

                        var manualPhone = !string.IsNullOrWhiteSpace(x.PhoneNumber)
                            ? x.PhoneNumber.Trim()
                            : existingManual?.PhoneNumber;

                        return new CSR2Participant
                        {
                            CSR2Id = report.Id,
                            ParticipantSourceId = null,
                            ParticipantSource = "Other",
                            Role = string.IsNullOrWhiteSpace(role) ? "Other" : role,
                            Name = string.IsNullOrWhiteSpace(manualName) ? null : manualName,
                            PhoneNumber = string.IsNullOrWhiteSpace(manualPhone) ? null : manualPhone,
                            CreatedAt = DateTime.UtcNow
                        };
                    })
                    .ToList();

                // Employee name/mobile are display-only and resolved from Employeelogin/EmployeeInformation.
                if (report.Employees.Count > 0)
                    _db.Set<CSR2Employee>().RemoveRange(report.Employees);

                report.Employees = dto.Employees
                    .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeInformationId))
                    .GroupBy(x => x.EmployeeInformationId.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Select(x => new CSR2Employee
                    {
                        CSR2Id = report.Id,
                        EmployeeInformationId = x.EmployeeInformationId.Trim(),
                        RoleName = x.RoleName.Trim()
                    })
                    .ToList();

                // Persist CSR-2 focus groups exactly like CSR-1 arrays.
                // Products1 is mandatory.
                // Products2 and Products3 are optional and are saved only
                // when both CropId and ProductId are available.


                // =====================================================
                // PRODUCTS 1 - MANDATORY
                // =====================================================

                var product1 = report.Products1.FirstOrDefault();

                if (product1 == null)
                {
                    product1 = new CSR2Products1
                    {
                        CSR2Id = report.Id
                    };

                    report.Products1.Add(product1);
                }

                product1.CropId = focus1.Crops;
                product1.ProductId = focus1.Products;
                product1.IsActive = true;


                // =====================================================
                // PRODUCTS 2 - OPTIONAL
                // Save only if both crop and product are available.
                // =====================================================

                var product2 = report.Products2.FirstOrDefault();

                var hasProduct2Data =
                    focus2.Crops.Length > 0 &&
                    focus2.Products.Length > 0;

                if (hasProduct2Data)
                {
                    if (product2 == null)
                    {
                        product2 = new CSR2Products2
                        {
                            CSR2Id = report.Id
                        };

                        report.Products2.Add(product2);
                    }

                    product2.CropId = focus2.Crops;
                    product2.ProductId = focus2.Products;
                    product2.IsActive = true;
                }
                else if (product2 != null)
                {
                    // If previously saved and user clears Products2,
                    // remove the row completely.
                    _db.Set<CSR2Products2>().Remove(product2);
                }


                // =====================================================
                // PRODUCTS 3 - OPTIONAL
                // Save only if both crop and product are available.
                // =====================================================

                var product3 = report.Products3.FirstOrDefault();

                var hasProduct3Data =
                    focus3.Crops.Length > 0 &&
                    focus3.Products.Length > 0;

                if (hasProduct3Data)
                {
                    if (product3 == null)
                    {
                        product3 = new CSR2Products3
                        {
                            CSR2Id = report.Id
                        };

                        report.Products3.Add(product3);
                    }

                    product3.CropId = focus3.Crops;
                    product3.ProductId = focus3.Products;
                    product3.IsActive = true;
                }
                else if (product3 != null)
                {
                    // If previously saved and user clears Products3,
                    // remove the row completely.
                    _db.Set<CSR2Products3>().Remove(product3);
                }

                // Save only expense rows where ActualAmount > 0.
                // Rows with null/0 amount are not persisted. If an existing row
                // is changed to null/0, it is removed from CSR2Expenses.
                var validIncomingExpenses = dto.Expenses
                    .Where(x => x.ActualAmount.HasValue && x.ActualAmount.Value > 0)
                    .ToList();

                var incomingExistingIds = validIncomingExpenses
                    .Where(x => x.Id.HasValue && x.Id.Value > 0)
                    .Select(x => x.Id!.Value)
                    .ToHashSet();

                var removedExpenses = report.Expenses
                    .Where(x => x.Id > 0 && !incomingExistingIds.Contains(x.Id))
                    .ToList();

                if (removedExpenses.Count > 0)
                    _db.Set<CSR2Expense>().RemoveRange(removedExpenses);

                foreach (var item in validIncomingExpenses)
                {
                    CSR2Expense? expense = null;

                    if (item.Id.HasValue && item.Id.Value > 0)
                    {
                        expense = report.Expenses.FirstOrDefault(x => x.Id == item.Id.Value);
                        if (expense == null)
                            return BadRequest($"Expense row #{item.Id.Value} does not belong to this CSR-2.");
                    }

                    if (expense == null)
                    {
                        expense = new CSR2Expense
                        {
                            CSR2Id = report.Id
                        };
                        report.Expenses.Add(expense);
                    }

                    expense.ExpenseInformationId = item.ExpenseInformationId;
                    expense.ActualAmount = item.ActualAmount;
                    expense.ExpenseDate = item.ExpenseDate;
                    expense.BillNo = string.IsNullOrWhiteSpace(item.BillNo)
                        ? null
                        : item.BillNo.Trim();
                }

                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                await tx.CommitAsync(HttpContext.RequestAborted);

                var deletedIds = removedExpenses.Select(x => x.Id).ToHashSet();
                return Ok(new
                {
                    id = report.Id,
                    report.CSR1Id,
                    Status = report.Status.ToString(),
                    Expenses = report.Expenses
                        .Where(x => !deletedIds.Contains(x.Id))
                        .OrderBy(x => x.ExpenseInformationId)
                        .ThenBy(x => x.Id)
                        .Select(x => new
                        {
                            x.Id,
                            x.ExpenseInformationId,
                            x.ActualAmount,
                            x.BillNo,
                            x.ExpenseDate
                        })
                        .ToList()
                });
            }
            catch
            {
                await tx.RollbackAsync(HttpContext.RequestAborted);
                throw;
            }
        }


        [HttpPost("{id:int}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var report = await _db.Set<CSR2>()
                .Include(x => x.Expenses)
                .Include(x => x.Participants)
                .Include(x => x.Employees)
                .Include(x => x.Media)
                .FirstOrDefaultAsync(x => x.Id == id, HttpContext.RequestAborted);

            if (report == null) return NotFound();

            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();

            if (!await CanCreateCSR2DraftAsync(csr1))
                return StatusCode(StatusCodes.Status403Forbidden, "You are not allowed to apply this CSR-2.");

            if (report.Status != CSR2Status.Draft)
                return Conflict("CSR-2 has already been applied.");

            if (!report.ActualEventDate.HasValue ||
                !report.StartTime.HasValue ||
                !report.EndTime.HasValue ||
                report.EndTime <= report.StartTime ||
                string.IsNullOrWhiteSpace(report.EventCoordinatorEmployeeId) ||
                string.IsNullOrWhiteSpace(report.ActualLocation))
                return BadRequest("Complete all mandatory actual event details before applying.");

            if (!report.Media.Any(x => x.MediaType == "Photo"))
                return BadRequest("At least one Event Photo is required.");

            if (!report.Media.Any(x => x.MediaType == "Video"))
                return BadRequest("At least one Event Video is required.");

            if (!report.Participants.Any())
                return BadRequest("At least one Participant is required.");

            if (!report.Employees.Any())
                return BadRequest("At least one SPIC Employee is required.");

            report.Status = CSR2Status.Submitted;
            report.SubmittedAt = DateTime.UtcNow;
            report.SubmittedBy = Actor;
            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedBy = Actor;

            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            var requiresMultiLevelApproval = await RequiresCSR2MultiLevelApprovalAsync(report);
            return Ok(new
            {
                report.Id,
                Status = report.Status.ToString(),
                RequiresMultiLevelApproval = requiresMultiLevelApproval
            });
        }

        [HttpPost("{id:int}/validate")]
        public async Task<IActionResult> ValidateCSR2(int id)
        {
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id, HttpContext.RequestAborted);
            if (report == null) return NotFound();

            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();

            if (report.Status != CSR2Status.Submitted)
                return Conflict("Only submitted CSR-2 can be validated.");

            // Validation is required even when CSR2Day is exceeded.
            // The cutoff changes only what happens AFTER validation.
            var requiresMultiLevelApproval = await RequiresCSR2MultiLevelApprovalAsync(report);

            var scope = await ResolveCSR2ScopeAsync(csr1);
            var isHqScope = string.Equals(scope, "HQ", StringComparison.OrdinalIgnoreCase);
            var effectiveRoles = await GetEffectiveCSR2RolesAsync();

            // Match CSR-1 review behavior: a valid State validator (SMD/SMDO) who
            // can access this approved CSR-1 may validate the Submitted CSR-2.
            // This also covers older records whose creator/scope metadata is incomplete.
            if (HasRole(effectiveRoles, "SMD", "SMDO"))
            {
                scope = "State";
            }
            else if (isHqScope && HasRole(effectiveRoles, "MO"))
            {
                scope = "HQ";
            }
            else
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Only SMD/SMDO can validate State CSR-2, and only MO can validate HQ CSR-2.");
            }

            var now = DateTime.UtcNow;
            report.Status = CSR2Status.Validated;
            report.ValidateBy = Actor;
            report.ValidateAt = now;
            report.UpdatedAt = now;
            report.UpdatedBy = Actor;

            await _db.SaveChangesAsync(HttpContext.RequestAborted);
            return Ok(new { report.Id, Scope = scope, Status = report.Status.ToString(), RequiresMultiLevelApproval = requiresMultiLevelApproval });
        }

        [HttpPost("{id:int}/approve")]
        public async Task<IActionResult> ApproveCSR2(int id)
        {
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id, HttpContext.RequestAborted);
            if (report == null) return NotFound();

            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();

            var scope = await ResolveCSR2ScopeAsync(csr1);
            var isStateScope = string.Equals(scope, "State", StringComparison.OrdinalIgnoreCase);
            var isHqScope = string.Equals(scope, "HQ", StringComparison.OrdinalIgnoreCase);
            var effectiveRoles = await GetEffectiveCSR2RolesAsync();
            var requiresMultiLevel = await RequiresCSR2MultiLevelApprovalAsync(report);
            var now = DateTime.UtcNow;

            if (requiresMultiLevel)
            {
                string approverLevel;

                if (report.Status == CSR2Status.Validated)
                {
                    if (isStateScope)
                    {
                        if (!HasRole(effectiveRoles, "SMM"))
                            return StatusCode(StatusCodes.Status403Forbidden,
                                "Only SMM can perform Level 1 approval for State CSR-2 after validation.");
                    }
                    else if (isHqScope)
                    {
                        if (!HasRole(effectiveRoles, "RMM"))
                            return StatusCode(StatusCodes.Status403Forbidden,
                                "Only RMM can perform Level 1 approval for HQ CSR-2 after validation.");
                    }
                    else
                    {
                        return BadRequest("Unable to determine CSR-2 workflow scope.");
                    }

                    approverLevel = "Level1";
                    report.Status = CSR2Status.Level1Approved;
                }
                else if (report.Status == CSR2Status.Level1Approved)
                {
                    if (!HasRole(effectiveRoles, "MDADMIN"))
                        return StatusCode(StatusCodes.Status403Forbidden,
                            "Only MDADMIN can perform Level 2 approval for CSR-2.");

                    approverLevel = "Level2";
                    report.Status = CSR2Status.Level2Approved;
                }
                else if (report.Status == CSR2Status.Level2Approved)
                {
                    if (!HasRole(effectiveRoles, "CSMDHOD", "CSMD-HOD", "MDHOD"))
                        return StatusCode(StatusCodes.Status403Forbidden,
                            "Only CSMD-HOD can perform Level 3 approval for CSR-2.");

                    approverLevel = "Level3";
                    report.Status = CSR2Status.Approved;
                    report.ApprovedBy = Actor;
                    report.ApprovedAt = now;
                }
                else
                {
                    return Conflict("CSR-2 is not at a valid multi-level approval stage.");
                }

                _db.Set<CSR2ApprovalDetails>().Add(new CSR2ApprovalDetails
                {
                    CSR2Id = report.Id,
                    ApprovedBy = Actor,
                    ApprovedAt = now,
                    ApproverLevel = approverLevel,
                    CreatedBy = Actor,
                    CreatedAt = now
                });

                report.UpdatedAt = now;
                report.UpdatedBy = Actor;

                await _db.SaveChangesAsync(HttpContext.RequestAborted);
                return Ok(new
                {
                    report.Id,
                    Scope = scope,
                    Status = report.Status.ToString(),
                    RequiresMultiLevelApproval = true,
                    ApproverLevel = approverLevel
                });
            }

            if (report.Status != CSR2Status.Validated)
                return Conflict("Only validated CSR-2 can be approved in the normal workflow.");

            if (isStateScope)
            {
                if (!HasRole(effectiveRoles, "SMM"))
                    return StatusCode(StatusCodes.Status403Forbidden,
                        "Only SMM can approve State CSR-2 within the assigned State scope.");
            }
            else if (isHqScope)
            {
                if (!HasRole(effectiveRoles, "RM"))
                    return StatusCode(StatusCodes.Status403Forbidden,
                        "Only RM can approve HQ CSR-2 within the assigned Headquarters scope.");
            }
            else
            {
                return BadRequest("Unable to determine CSR-2 workflow scope.");
            }

            report.Status = CSR2Status.Approved;
            report.ApprovedBy = Actor;
            report.ApprovedAt = now;
            report.UpdatedAt = now;
            report.UpdatedBy = Actor;

            // Normal approval does NOT write CSR2ApprovalDetails.
            await _db.SaveChangesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                report.Id,
                Scope = scope,
                Status = report.Status.ToString(),
                RequiresMultiLevelApproval = false
            });
        }

        [HttpPost("{id:int}/media")]
        [RequestSizeLimit(720L * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 720L * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadMedia(int id, [FromForm] CSR2UploadMediaDto form)
        {
            var file = form.File;
            var mediaType = form.MediaType?.Trim();
            var caption = form.Caption;

            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == id);
            if (report == null) return NotFound();
            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();
            if (!await CanEditCSR2Async(csr1, report))
                return StatusCode(StatusCodes.Status403Forbidden, "You are not allowed to upload media at the current workflow stage.");

            var isPhoto = string.Equals(mediaType, "Photo", StringComparison.OrdinalIgnoreCase);
            var isVideo = string.Equals(mediaType, "Video", StringComparison.OrdinalIgnoreCase);
            var isDocument = string.Equals(mediaType, "Document", StringComparison.OrdinalIgnoreCase);

            if (!isPhoto && !isVideo && !isDocument)
                return BadRequest("MediaType must be Photo, Video or Document.");

            var max = isPhoto
                ? 20L * 1024 * 1024
                : isVideo
                    ? 700L * 1024 * 1024
                    : 25L * 1024 * 1024;

            var extensions = isPhoto
                ? new[] { ".jpg", ".jpeg", ".png" }
                : isVideo
                    ? new[] { ".mp4", ".mov" }
                    : new[] { ".pdf", ".doc", ".docx" };

            if (file == null ||
                file.Length <= 0 ||
                file.Length > max ||
                !extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            {
                return BadRequest("Invalid file type or size.");
            }

            var stored = await StoreFileAsync(id, file, extensions);

            var media = new CSR2Media
            {
                CSR2Id = id,
                MediaType = isPhoto ? "Photo" : isVideo ? "Video" : "Document",
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = stored.ContentType,
                FileSizeBytes = file.Length,
                StorageKey = stored.StorageKey,
                Caption = caption,
                UploadedBy = Actor
            };

            try
            {
                _db.Set<CSR2Media>().Add(media);
                await _db.SaveChangesAsync();
            }
            catch
            {
                System.IO.File.Delete(stored.Path);
                throw;
            }

            return Ok(new { media.Id, media.MediaType, media.OriginalFileName });
        }

        [HttpPost("{id:int}/expenses/{expenseId:int}/proof")]
        [RequestSizeLimit(12L * 1024 * 1024)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadExpenseProof(int id, int expenseId, IFormFile file)
        {
            var report = await _db.Set<CSR2>()
                .FirstOrDefaultAsync(x => x.Id == id, HttpContext.RequestAborted);

            if (report == null) return NotFound();
            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();
            if (!await CanEditCSR2Async(csr1, report))
                return StatusCode(StatusCodes.Status403Forbidden, "You are not allowed to upload expense proofs at the current workflow stage.");

            var expense = await _db.Set<CSR2Expense>()
                .Include(x => x.Proofs)
                .FirstOrDefaultAsync(
                    x => x.Id == expenseId && x.CSR2Id == id,
                    HttpContext.RequestAborted);

            if (expense == null)
                return NotFound("Save the expense row before uploading proof.");

            var extensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            if (file == null ||
                file.Length <= 0 ||
                file.Length > 10L * 1024 * 1024 ||
                !extensions.Contains(Path.GetExtension(file.FileName).ToLowerInvariant()))
            {
                return BadRequest("Invalid proof type or size.");
            }

            var stored = await StoreFileAsync(id, file, extensions);
            var proof = new CSR2ExpenseProof
            {
                CSR2ExpenseId = expense.Id,
                OriginalFileName = Path.GetFileName(file.FileName),
                ContentType = stored.ContentType,
                FileSizeBytes = file.Length,
                StorageKey = stored.StorageKey,
                UploadedAt = DateTime.UtcNow
            };

            try
            {
                _db.Set<CSR2ExpenseProof>().Add(proof);
                await _db.SaveChangesAsync(HttpContext.RequestAborted);
            }
            catch
            {
                if (System.IO.File.Exists(stored.Path))
                    System.IO.File.Delete(stored.Path);
                throw;
            }

            return Ok(new
            {
                id = proof.Id,
                expenseId = expense.Id,
                proof.OriginalFileName
            });
        }

        [HttpGet("media/{mediaId:int}/file")]
        public async Task<IActionResult> ReadMedia(int mediaId)
        {
            var media = await _db.Set<CSR2Media>().FirstOrDefaultAsync(x => x.Id == mediaId);
            if (media == null || await ApprovedCsr1Async((await _db.Set<CSR2>().Where(x => x.Id == media.CSR2Id).Select(x => x.CSR1Id).FirstOrDefaultAsync())) == null) return NotFound();
            var path = ResolveStoragePath(media.StorageKey);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, media.ContentType, enableRangeProcessing: true);
        }
        [HttpGet("proof/{proofId:int}/file")]
        public async Task<IActionResult> ReadProof(int proofId)
        {
            var proof = await _db.Set<CSR2ExpenseProof>().Include(x => x.CSR2Expense).FirstOrDefaultAsync(x => x.Id == proofId);
            if (proof?.CSR2Expense == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == proof.CSR2Expense.CSR2Id);
            if (report == null || await ApprovedCsr1Async(report.CSR1Id) == null) return NotFound();
            var path = ResolveStoragePath(proof.StorageKey);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, proof.ContentType, enableRangeProcessing: true);
        }
        [HttpDelete("media/{mediaId:int}")]
        public async Task<IActionResult> DeleteMedia(int mediaId)
        {
            var media = await _db.Set<CSR2Media>().FirstOrDefaultAsync(x => x.Id == mediaId);
            if (media == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == media.CSR2Id);
            if (report == null) return NotFound();
            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();
            if (!await CanEditCSR2Async(csr1, report)) return StatusCode(StatusCodes.Status403Forbidden);
            _db.Remove(media); await _db.SaveChangesAsync();
            System.IO.File.Delete(ResolveStoragePath(media.StorageKey)); return NoContent();
        }
        [HttpDelete("proof/{proofId:int}")]
        public async Task<IActionResult> DeleteProof(int proofId)
        {
            var proof = await _db.Set<CSR2ExpenseProof>().Include(x => x.CSR2Expense).FirstOrDefaultAsync(x => x.Id == proofId);
            if (proof?.CSR2Expense == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == proof.CSR2Expense.CSR2Id);
            if (report == null) return NotFound();
            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();
            if (!await CanEditCSR2Async(csr1, report)) return StatusCode(StatusCodes.Status403Forbidden);
            _db.Remove(proof); await _db.SaveChangesAsync();
            System.IO.File.Delete(ResolveStoragePath(proof.StorageKey)); return NoContent();
        }
        [HttpPatch("media/{mediaId:int}")]
        public async Task<IActionResult> UpdateMedia(int mediaId, [FromBody] CSR2MediaDto patch)
        {
            var media = await _db.Set<CSR2Media>().FirstOrDefaultAsync(x => x.Id == mediaId);
            if (media == null) return NotFound();
            var report = await _db.Set<CSR2>().FirstOrDefaultAsync(x => x.Id == media.CSR2Id);
            if (report == null) return NotFound();
            var csr1 = await ApprovedCsr1Async(report.CSR1Id);
            if (csr1 == null) return NotFound();
            if (!await CanEditCSR2Async(csr1, report)) return StatusCode(StatusCodes.Status403Forbidden);
            media.Caption = patch.Caption;
            await _db.SaveChangesAsync(); return NoContent();
        }

        private async Task<(string StorageKey, string Path, string ContentType)> StoreFileAsync(int reportId, IFormFile file, string[] allowedExtensions)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext)) throw new InvalidDataException("File extension rejected.");
            // Content-Type supplied by browser is not trusted for security; use allowed extension mapping.
            var type = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".mov" => "video/quicktime",
                _ => "video/mp4"
            };
            var key = $"{reportId}/{Guid.NewGuid():N}{ext}";
            var path = ResolveStoragePath(key); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await file.CopyToAsync(target, HttpContext.RequestAborted);
            return (key, path, type);
        }
        private string ResolveStoragePath(string storageKey)
        {
            // Storage keys originate only from server-generated GUID values.
            var root = Path.GetFullPath(UploadRoot) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(root, StringComparison.Ordinal)) throw new InvalidOperationException("Invalid storage key.");
            return path;
        }


        // =============================================================
        // CSR-2 LIST PAGE ENDPOINTS
        // Uses this existing CSR2Controller. No separate list controller.
        // =============================================================
        [HttpGet("list/lookups")]
        public async Task<IActionResult> GetCSR2ListLookups()
        {
            var programTypes = await _db.Set<ProgramMaster>()
                .AsNoTracking()
                .Where(x => x.ProgramType != null)
                .Select(x => x.ProgramType.Name)
                .Where(x => x != null && x != "")
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync(HttpContext.RequestAborted);

            var programs = await _db.Set<ProgramMaster>()
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new
                {
                    x.Id,
                    Name = x.Name,
                    ProgramType = x.ProgramType.Name
                })
                .ToListAsync(HttpContext.RequestAborted);

            var statuses = Enum.GetValues<CSR2Status>()
                .Select(value => new
                {
                    Value = (int)value,
                    Name = value.ToString()
                })
                .ToList();

            return Ok(new
            {
                ProgramTypes = programTypes,
                Programs = programs,
                Statuses = statuses
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetCSR2List(
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? programType,
            [FromQuery] int? programId,
            [FromQuery] int? locationId,
            [FromQuery] CSR2Status? status,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 16)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            // Keep the existing visibility/authorization rule unchanged.
            var visibleCsr1 = await VisibleApprovedCSR1Async();

            var query =
                from csr2 in _db.Set<CSR2>().AsNoTracking()
                join csr1 in visibleCsr1 on csr2.CSR1Id equals csr1.Id
                join program in _db.Set<ProgramMaster>().AsNoTracking() on csr1.ProgramId equals program.Id
                select new
                {
                    CSR2 = csr2,
                    CSR1 = csr1,
                    Program = program
                };

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x =>
                    x.CSR2.ActualEventDate.HasValue &&
                    x.CSR2.ActualEventDate.Value >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(x =>
                    x.CSR2.ActualEventDate.HasValue &&
                    x.CSR2.ActualEventDate.Value < toExclusive);
            }

            if (!string.IsNullOrWhiteSpace(programType))
            {
                var type = programType.Trim();
                query = query.Where(x => x.Program.ProgramType.Name == type);
            }

            if (programId.HasValue && programId.Value > 0)
                query = query.Where(x => x.Program.Id == programId.Value);

            if (locationId.HasValue && locationId.Value > 0)
                query = query.Where(x => x.CSR1.LocationId == locationId.Value);

            if (status.HasValue)
                query = query.Where(x => x.CSR2.Status == status.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.CSR1.SanctionNo != null && x.CSR1.SanctionNo.Contains(term)) ||
                    x.Program.Name.Contains(term) ||
                    x.Program.ProgramType.Name.Contains(term) ||
                    (x.CSR2.ActualLocation != null && x.CSR2.ActualLocation.Contains(term)));
            }

            // Keep count queries simple. The expensive child-table aggregates are intentionally
            // not part of this base query.
            var totalCount = await query.CountAsync(HttpContext.RequestAborted);

            var statusCounts = await query
                .GroupBy(x => x.CSR2.Status)
                .Select(g => new
                {
                    Status = g.Key.ToString(),
                    Count = g.Count()
                })
                .ToListAsync(HttpContext.RequestAborted);

            // Load only the current page first. This avoids executing several correlated
            // COUNT/SUM subqueries once per CSR-2 row inside one large SQL statement.
            var rows = await query
                .OrderByDescending(x => x.CSR2.UpdatedAt ?? x.CSR2.CreatedAt)
                .ThenByDescending(x => x.CSR2.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new
                {
                    CSR2Id = x.CSR2.Id,
                    CSR1Id = x.CSR1.Id,
                    SanctionNumber = x.CSR1.SanctionNo ?? "",
                    ProgramType = x.Program.ProgramType.Name,
                    ProgramId = x.Program.Id,
                    ProgramName = x.Program.Name,
                    LocationId = x.CSR1.LocationId,
                    HeadquarterId = x.CSR1.HeadquarterId,
                    ApprovedBudget = x.CSR1.Budget,
                    Status = x.CSR2.Status.ToString(),
                    StatusValue = (int)x.CSR2.Status,
                    x.CSR2.ActualEventDate,
                    x.CSR2.StartTime,
                    x.CSR2.EndTime,
                    x.CSR2.EventCoordinatorEmployeeId,
                    x.CSR2.ActualLocation,
                    x.CSR2.SubmittedAt,
                    ResponsiblePersonId = x.CSR1.ResponsiblePersonId,
                    CSR1CreatedBy = x.CSR1.CreatedBy
                })
                .ToListAsync(HttpContext.RequestAborted);

            if (rows.Count == 0)
            {
                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
                    StatusCounts = statusCounts,
                    Items = Array.Empty<object>()
                });
            }

            var csr2Ids = rows.Select(x => x.CSR2Id).ToList();

            // Aggregate child data only for rows on the current page.
            var participantCounts = await _db.Set<CSR2Participant>()
                .AsNoTracking()
                .Where(x => csr2Ids.Contains(x.CSR2Id))
                .GroupBy(x => x.CSR2Id)
                .Select(g => new { CSR2Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CSR2Id, x => x.Count, HttpContext.RequestAborted);

            var employeeCounts = await _db.Set<CSR2Employee>()
                .AsNoTracking()
                .Where(x => csr2Ids.Contains(x.CSR2Id))
                .GroupBy(x => x.CSR2Id)
                .Select(g => new { CSR2Id = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CSR2Id, x => x.Count, HttpContext.RequestAborted);

            var mediaCounts = await _db.Set<CSR2Media>()
                .AsNoTracking()
                .Where(x => csr2Ids.Contains(x.CSR2Id))
                .GroupBy(x => x.CSR2Id)
                .Select(g => new
                {
                    CSR2Id = g.Key,
                    PhotoCount = g.Count(x => x.MediaType == "Photo"),
                    VideoCount = g.Count(x => x.MediaType == "Video")
                })
                .ToDictionaryAsync(x => x.CSR2Id, HttpContext.RequestAborted);

            var expenseAggregates = await _db.Set<CSR2Expense>()
                .AsNoTracking()
                .Where(x => csr2Ids.Contains(x.CSR2Id))
                .GroupBy(x => x.CSR2Id)
                .Select(g => new
                {
                    CSR2Id = g.Key,
                    ExpenseCount = g.Count(),
                    ActualExpense = g.Sum(x => (decimal?)x.ActualAmount) ?? 0m
                })
                .ToDictionaryAsync(x => x.CSR2Id, HttpContext.RequestAborted);

            var proofCounts = await (
                from proof in _db.Set<CSR2ExpenseProof>().AsNoTracking()
                join expense in _db.Set<CSR2Expense>().AsNoTracking()
                    on proof.CSR2ExpenseId equals expense.Id
                where csr2Ids.Contains(expense.CSR2Id)
                group proof by expense.CSR2Id into g
                select new { CSR2Id = g.Key, Count = g.Count() }
            ).ToDictionaryAsync(x => x.CSR2Id, x => x.Count, HttpContext.RequestAborted);

            // Determine State/HQ from the CSR-1 applicant role, preserving the existing logic.
            var csr1CreatorIds = rows
                .Select(x => x.CSR1CreatedBy)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            var csr1CreatorRoles = await _db.Set<Employeelogin>()
                .AsNoTracking()
                .Where(x => csr1CreatorIds.Contains(x.UserId))
                .Select(x => new { x.UserId, x.Role })
                .ToListAsync(HttpContext.RequestAborted);

            var creatorRolesByUserId = csr1CreatorRoles
                .GroupBy(x => x.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Role.ToString())
                          .Where(role => !string.IsNullOrWhiteSpace(role))
                          .ToHashSet(StringComparer.OrdinalIgnoreCase),
                    StringComparer.OrdinalIgnoreCase);

            var effectiveRoles = await GetEffectiveCSR2RolesAsync();
            var cutoffDay = await GetCSR2CutoffDayAsync();

            var items = rows.Select(x =>
            {
                participantCounts.TryGetValue(x.CSR2Id, out var participantCount);
                employeeCounts.TryGetValue(x.CSR2Id, out var employeeCount);
                mediaCounts.TryGetValue(x.CSR2Id, out var mediaCount);
                expenseAggregates.TryGetValue(x.CSR2Id, out var expenseAggregate);
                proofCounts.TryGetValue(x.CSR2Id, out var proofCount);

                var photoCount = mediaCount?.PhotoCount ?? 0;
                var videoCount = mediaCount?.VideoCount ?? 0;
                var expenseCount = expenseAggregate?.ExpenseCount ?? 0;
                var actualExpense = expenseAggregate?.ActualExpense ?? 0m;

                var mandatoryComplete = 0;
                if (x.ActualEventDate.HasValue) mandatoryComplete++;
                if (x.StartTime.HasValue) mandatoryComplete++;
                if (x.EndTime.HasValue) mandatoryComplete++;
                if (!string.IsNullOrWhiteSpace(x.EventCoordinatorEmployeeId)) mandatoryComplete++;
                if (!string.IsNullOrWhiteSpace(x.ActualLocation)) mandatoryComplete++;
                if (photoCount > 0) mandatoryComplete++;
                if (videoCount > 0) mandatoryComplete++;
                if (participantCount > 0) mandatoryComplete++;
                if (employeeCount > 0) mandatoryComplete++;

                var reportCompletion = (int)Math.Round(mandatoryComplete / 9d * 100d);

                var creatorRoles = !string.IsNullOrWhiteSpace(x.CSR1CreatedBy) &&
                                   creatorRolesByUserId.TryGetValue(x.CSR1CreatedBy, out var roleSet)
                    ? roleSet
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var isState = creatorRoles.Overlaps(new[] { "SMD", "SMDO", "SMM" });
                var isHq = creatorRoles.Overlaps(new[] { "MDO", "JMDO", "RMDO", "MO", "RM", "RMM", "RMD" });

                if (!isState && !isHq)
                {
                    if (HasRole(effectiveRoles, "SMD", "SMDO", "SMM")) isState = true;
                    else if (HasRole(effectiveRoles, "MDO", "JMDO", "RMDO", "MO", "RM", "RMM", "RMD")) isHq = true;
                }

                var scope = isState ? "State" : "HQ";
                var requiresMultiLevelApproval =
                    cutoffDay > 0 &&
                    x.SubmittedAt.HasValue &&
                    x.SubmittedAt.Value.Day > cutoffDay;

                var isResponsiblePerson =
                    !string.IsNullOrWhiteSpace(CurrentUserId) &&
                    string.Equals(x.ResponsiblePersonId, CurrentUserId, StringComparison.OrdinalIgnoreCase);

                // Workflow behavior intentionally unchanged by this performance patch.
                var canValidate =
                    x.StatusValue == (int)CSR2Status.Submitted &&
                    !isResponsiblePerson &&
                    (isState
                        ? HasRole(effectiveRoles, "SMD", "SMDO")
                        : HasRole(effectiveRoles, "MO"));

                var canApprove = false;
                var actionLabel = string.Empty;

                if (!isResponsiblePerson && requiresMultiLevelApproval)
                {
                    if (x.StatusValue == (int)CSR2Status.Validated)
                    {
                        canApprove = isState
                            ? HasRole(effectiveRoles, "SMM")
                            : HasRole(effectiveRoles, "RMM");
                        if (canApprove) actionLabel = "Approve Level 1";
                    }
                    else if (x.StatusValue == (int)CSR2Status.Level1Approved)
                    {
                        canApprove = HasRole(effectiveRoles, "MDADMIN");
                        if (canApprove) actionLabel = "Approve Level 2";
                    }
                    else if (x.StatusValue == (int)CSR2Status.Level2Approved)
                    {
                        canApprove = HasRole(effectiveRoles, "CSMDHOD", "CSMD-HOD", "MDHOD");
                        if (canApprove) actionLabel = "Approve Level 3";
                    }
                }
                else if (!isResponsiblePerson && x.StatusValue == (int)CSR2Status.Validated)
                {
                    canApprove = isState
                        ? HasRole(effectiveRoles, "SMM")
                        : HasRole(effectiveRoles, "RM");
                    if (canApprove) actionLabel = "Approve CSR-2";
                }

                if (canValidate)
                    actionLabel = "Validate CSR-2";

                return new
                {
                    x.CSR2Id,
                    x.CSR1Id,
                    x.SanctionNumber,
                    x.ProgramType,
                    x.ProgramId,
                    x.ProgramName,
                    x.LocationId,
                    x.HeadquarterId,
                    x.ApprovedBudget,
                    x.Status,
                    x.StatusValue,
                    x.ActualEventDate,
                    x.StartTime,
                    x.EndTime,
                    x.EventCoordinatorEmployeeId,
                    x.ActualLocation,
                    x.ResponsiblePersonId,
                    Scope = scope,
                    RequiresMultiLevelApproval = requiresMultiLevelApproval,
                    CanValidate = canValidate,
                    CanApprove = canApprove,
                    ActionLabel = actionLabel,
                    ParticipantCount = participantCount,
                    EmployeeCount = employeeCount,
                    PhotoCount = photoCount,
                    VideoCount = videoCount,
                    ExpenseCount = expenseCount,
                    ActualExpense = actualExpense,
                    ProofCount = proofCount,
                    ReportCompletion = reportCompletion
                };
            }).ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
                StatusCounts = statusCounts,
                Items = items
            });
        }

        [HttpGet("csr1-display/{csr1Id:int}")]
        public async Task<IActionResult> GetCSR1DisplayDetails(int csr1Id)
        {
            // CSR1 is the authoritative source for all three values shown in CSR2:
            // ApprovedBy, ApprovedAt and ResponsiblePersonId.
            // Read these values directly from CSR1.
            // Do not apply CreatedBy ownership here; the CSR-2 page may be opened
            // by an authorized reviewer/employee who did not create the CSR-1.
            var csr1 = await _db.Set<CSR1>()
                .AsNoTracking()
                .Where(x =>
                    x.Id == csr1Id &&
                    x.IsActive &&
                    x.Status == CSR1Status.Approved)
                .Select(x => new
                {
                    x.Id,
                    x.ApprovedBy,
                    x.ApprovedAt,
                    x.ResponsiblePersonId
                })
                .FirstOrDefaultAsync();

            if (csr1 == null)
            {
                return NotFound(new
                {
                    message = $"Approved CSR-1 #{csr1Id} not found or access denied."
                });
            }

            // Return all active ExpenseInformations for CSR-2 display.
            // Matching CSR1Expenses values are filled in; missing rows are returned as 0.
            var plannedExpenses = await _db.Set<ExpenseInformation>()
                .AsNoTracking()
                .Where(masterExpense => masterExpense.IsActive)
                .OrderBy(masterExpense => masterExpense.Id)
                .Select(masterExpense => new
                {
                    ExpenseId = masterExpense.Id,
                    ExpenseName = masterExpense.Name,
                    ExpenseAmount = _db.Set<CSR1Expenses>()
                        .Where(expense =>
                            expense.CSR1Id == csr1Id &&
                            expense.IsActive &&
                            expense.ExpenseId == masterExpense.Id)
                        .Sum(expense => (decimal?)expense.ExpenseAmount) ?? 0m
                })
                .ToListAsync(HttpContext.RequestAborted);

            string? approvedByName = null;
            string? responsiblePersonName = null;

            // CSR1.ApprovedBy stores the application UserId.
            // Resolve it through Employeelogin -> EmployeeInformation.Name.
            if (!string.IsNullOrWhiteSpace(csr1.ApprovedBy))
            {
                approvedByName = await (
                    from login in _db.Set<Employeelogin>().AsNoTracking()
                    join employee in _db.Set<EmployeeInformation>().AsNoTracking()
                        on login.EmployeeInformationID equals employee.Id
                    where login.UserId == csr1.ApprovedBy
                    select employee.Name
                ).FirstOrDefaultAsync();
            }

            // CSR1.ResponsiblePersonId stores Employeelogins.UserId.
            // Resolve UserId -> EmployeeInformation so CSR-2 receives the name
            // and the numeric employee id required by its dropdown.
            int? responsiblePersonEmployeeId = null;

            if (!string.IsNullOrWhiteSpace(csr1.ResponsiblePersonId))
            {
                var responsible = await (
                    from login in _db.Set<Employeelogin>().AsNoTracking()
                    join employee in _db.Set<EmployeeInformation>().AsNoTracking()
                        on login.EmployeeInformationID equals employee.Id
                    where login.UserId == csr1.ResponsiblePersonId
                    select new
                    {
                        EmployeeId = employee.Id,
                        employee.Name
                    }
                ).FirstOrDefaultAsync();

                responsiblePersonEmployeeId = responsible?.EmployeeId;
                responsiblePersonName = responsible?.Name;
            }

            return Ok(new
            {
                csr1Id = csr1.Id,
                approvedById = csr1.ApprovedBy,
                approvedByName = approvedByName ?? "",
                approvedAt = csr1.ApprovedAt,
                responsiblePersonId = csr1.ResponsiblePersonId,
                responsiblePersonEmployeeId = responsiblePersonEmployeeId?.ToString(),
                responsiblePersonName = responsiblePersonName ?? "",
                expenses = plannedExpenses
            });
        }
    }

    public class ParticipantMobileLookupResult
    {
        public int SourceId { get; set; }
        public string Mobile { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class CSR2UploadMediaDto
    {
        public IFormFile File { get; set; } = null!;

        public string MediaType { get; set; } = string.Empty;

        public string? Caption { get; set; }
    }
}

