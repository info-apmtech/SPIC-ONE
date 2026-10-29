using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace SPIC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubDealerRegistrationController : ControllerBase
{
    private const long MaxExcelImportSize = 10 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubDealerRegistrationController> _logger;

    public SubDealerRegistrationController(AppDbContext db, IWebHostEnvironment environment,
        IConfiguration configuration, ILogger<SubDealerRegistrationController> logger)
    {
        _db = db;
        _environment = environment;
        _configuration = configuration;
        _logger = logger;
    }

    private string? CurrentUserId() => Actor(User);
    private IQueryable<SubDealerRegistration> Scoped(bool tracking = false)
    {
        var source = tracking ? _db.SubDealerRegistrations.AsQueryable() : _db.SubDealerRegistrations.AsNoTracking();
        return Scope(source, _db, User);
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<List<SubDealerLookupDto>>> Lookup(
        [FromQuery] int? stateId,
        [FromQuery] int? regionId,
        [FromQuery] int? hqId,
        CancellationToken cancellationToken)
    {
        var query = Scoped();
        // Query parameters narrow the authenticated scope; they never grant access.
        if (stateId.GetValueOrDefault() > 0) query = query.Where(x => x.StateId == stateId!.Value);
        if (regionId.GetValueOrDefault() > 0) query = query.Where(x => x.Region == regionId!.Value);
        if (hqId.GetValueOrDefault() > 0) query = query.Where(x => x.HQ == hqId!.Value);

        query = query.Where(x => x.SubDealerCode != null && x.SubDealerCode.Trim() != "");

        var items = await query
            .OrderBy(x => x.SubDealerCode)
            .Select(x => new SubDealerLookupDto
            {
                Id = x.Id,
                SubDealerCode = x.SubDealerCode ?? string.Empty,
                FirmName = x.FirmName,
                StateId = x.StateId,
                RegionId = x.Region,
                HQId = x.HQ
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("list")]
    public async Task<ActionResult<SubDealerPagedListResponse>> GetList(
        [FromQuery] string? search,
        [FromQuery] int? status,
        [FromQuery] int? stateId,
        [FromQuery] int? regionId,
        [FromQuery] int? hqId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 10 ? 10 : pageSize > 1000 ? 1000 : pageSize;

        await using var snapshot = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var query = Scoped().Where(HasMaintainedDetails);

        // UI filters can only NARROW the already role-scoped query.
        // --------------------------------------------------------------
        if (stateId.HasValue && stateId.Value > 0)
            query = query.Where(x => x.StateId == stateId.Value);

        if (regionId.HasValue && regionId.Value > 0)
            query = query.Where(x => x.Region == regionId.Value);

        if (hqId.HasValue && hqId.Value > 0)
            query = query.Where(x => x.HQ == hqId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();

            query = query.Where(x =>
                (x.SubDealerCode != null && x.SubDealerCode.ToLower().Contains(term)) ||
                x.FirmName.ToLower().Contains(term) ||
                (x.OfficialContactNumber != null && x.OfficialContactNumber.ToLower().Contains(term)) ||
                (x.WhatsAppNumber != null && x.WhatsAppNumber.ToLower().Contains(term)) ||
                (x.GSTNumber != null && x.GSTNumber.ToLower().Contains(term)) ||
                (x.PANNo != null && x.PANNo.ToLower().Contains(term)) ||
                (x.WholesaleMFMSId != null && x.WholesaleMFMSId.ToLower().Contains(term)) ||
                (x.RetailMFMSId != null && x.RetailMFMSId.ToLower().Contains(term)));
        }

        // Counts follow role + location + search filters, but not the selected
        // status filter. This lets the page show useful Active/Inactive totals.
        var statusCounts = await query
            .GroupBy(x => x.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        var activeCount = statusCounts
            .FirstOrDefault(x => x.Status == SubDealerStatus.Active)?.Count ?? 0;

        var inactiveCount = statusCounts
            .FirstOrDefault(x => x.Status == SubDealerStatus.InActive)?.Count ?? 0;

        if (status.HasValue &&
            (status.Value == (int)SubDealerStatus.Active ||
             status.Value == (int)SubDealerStatus.InActive))
        {
            var requestedStatus = (SubDealerStatus)status.Value;
            query = query.Where(x => x.Status == requestedStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)pageSize);

        page = totalPages == 0 ? 1 : Math.Min(page, totalPages);

        var skip = (page - 1) * pageSize;

        var items = await query
            .OrderBy(x => x.FirmName)
            .ThenBy(x => x.SubDealerCode)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new SubDealerListItemDto
            {
                Id = x.Id,
                SubDealerCode = x.SubDealerCode ?? string.Empty,
                FirmName = x.FirmName,

                StateId = x.StateId,
                RegionId = x.Region,
                HQId = x.HQ,

                Status = x.Status,

                OfficialContactNumber = x.OfficialContactNumber,
                WhatsAppNumber = x.WhatsAppNumber,

                GSTNumber = x.GSTNumber,
                PANNo = x.PANNo,

                WholesaleMFMSId = x.WholesaleMFMSId,
                RetailMFMSId = x.RetailMFMSId,
                // -------------------------------------------------
                // Location details required for Excel export
                // -------------------------------------------------
                Latitude = x.Latitude,
                Longitude = x.Longitude,

                ShopNoORRoomNoOrBlockNo = x.ShopNoORRoomNoOrBlockNo,
                Street = x.Street,
                SubVillage = x.SubVillage,
                Village = x.Village,
                Block = x.Block,
                Taluk = x.Taluk,
                DistrictId = x.DistrictId,
                PinCode = x.PinCode,

                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        await snapshot.CommitAsync(cancellationToken);
        return Ok(new SubDealerPagedListResponse
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            ActiveCount = activeCount,
            InactiveCount = inactiveCount
        });
    }

    public sealed class SubDealerStateSummaryDto
    {
        public int StateId { get; set; }
        public string StateName { get; set; } = string.Empty;
        public int TotalSubDealers { get; set; }
        public int SavedSubDealers { get; set; }
        public int ActiveSubDealers { get; set; }
        public int InactiveSubDealers { get; set; }
    }

    [HttpGet("state-summary")]
    public async Task<ActionResult<List<SubDealerStateSummaryDto>>> StateSummary(CancellationToken cancellationToken)
    {
        // A repeatable snapshot prevents master/saved counters from describing different moments.
        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken);
        var master = await Scoped().GroupBy(x => x.StateId)
            .Select(g => new { StateId = g.Key, Total = g.Count() }).ToListAsync(cancellationToken);
        var maintained = await Scoped().Where(HasMaintainedDetails).GroupBy(x => x.StateId)
            .Select(g => new {
                StateId = g.Key,
                Active = g.Count(x => x.Status == SubDealerStatus.Active),
                Inactive = g.Count(x => x.Status == SubDealerStatus.InActive)
            }).ToListAsync(cancellationToken);
        var ids = master.Select(x => x.StateId).ToList();
        var names = await _db.States.AsNoTracking().Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.StateName }).ToDictionaryAsync(x => x.Id, x => x.StateName, cancellationToken);
        var savedByState = maintained.ToDictionary(x => x.StateId);
        var rows = master.Select(x =>
        {
            savedByState.TryGetValue(x.StateId, out var saved);
            return new SubDealerStateSummaryDto
            {
                StateId = x.StateId,
                StateName = names.GetValueOrDefault(x.StateId, $"State {x.StateId}"),
                TotalSubDealers = x.Total,
                ActiveSubDealers = saved?.Active ?? 0,
                InactiveSubDealers = saved?.Inactive ?? 0,
                SavedSubDealers = (saved?.Active ?? 0) + (saved?.Inactive ?? 0)
            };
        }).OrderBy(x => x.StateName).ToList();
        await transaction.CommitAsync(cancellationToken);
        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SubDealerFormModel>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var entity = await Scoped().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (entity == null)
            return NotFound($"Sub Dealer with Id {id} was not found.");

        return Ok(ToModel(entity));
    }

    [HttpPost]
    public Task<ActionResult<SubDealerFormModel>> Create(
        [FromBody] SubDealerFormModel model, CancellationToken cancellationToken)
    {
        // Keep the route/signature for existing clients, but never insert from the profile form.
        return Task.FromResult<ActionResult<SubDealerFormModel>>(BadRequest(
            "Sub Dealer masters must be created by Admin Excel import. Select the imported Sub Dealer and save its details using the edit screen."));
    }

    // ONE-TIME SUPERADMIN MAINTENANCE: Excel maps an existing code to a new code.
    // This remains separate from normal master import and profile maintenance.
    // Correct only SubDealerCode and server-managed modification audit fields.
    // Id, original creation audit, FirmName, Status, hierarchy and contact information are preserved.
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("bulk-code-correction/preview")]
    [RequestSizeLimit(MaxExcelImportSize + 2 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> PreviewSubDealerCodeCorrection(
        IFormFile file,
        CancellationToken cancellationToken) =>
        ProcessSubDealerCodeCorrection(file, false, cancellationToken);

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("bulk-code-correction/apply")]
    [RequestSizeLimit(MaxExcelImportSize + 2 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public Task<IActionResult> ApplySubDealerCodeCorrection(
        IFormFile file,
        CancellationToken cancellationToken) =>
        ProcessSubDealerCodeCorrection(file, true, cancellationToken);

    private sealed class SubDealerCodeCorrectionRow
    {
        public int ExcelRowNumber { get; set; }
        public string ExistingCode { get; set; } = string.Empty;
        public string NewCode { get; set; } = string.Empty;
        public string FirmName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    private sealed class SubDealerCodeCorrectionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int Ready { get; set; }
        public int Unchanged { get; set; }
        public int Errors { get; set; }
        public int Updated { get; set; }
        public List<SubDealerCodeCorrectionRow> Rows { get; set; } = new();
    }

    private static string NormalizeCode(string? code) => SubDealerFormModel.CodeKey(code);

    private async Task<IActionResult> ProcessSubDealerCodeCorrection(
        IFormFile file,
        bool apply,
        CancellationToken cancellationToken)
    {
        if (!HasRole(User, "SuperAdmin")) return Forbid();
        if (string.IsNullOrWhiteSpace(CurrentUserId())) return Unauthorized();
        if (file == null || file.Length == 0)
            return BadRequest(new { Success = false, Message = "Select an Excel file." });

        if (file.Length > MaxExcelImportSize ||
            !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { Success = false, Message = "Upload a .xlsx file smaller than 10 MB." });

        List<SubDealerCodeCorrectionRow> rows;
        try
        {
            await using var excelStream = file.OpenReadStream();
            using var workbook = new XLWorkbook(excelStream);
            IXLWorksheet? correctionSheet = null;
            Dictionary<string, int>? correctionHeaders = null;
            var headerRowNumber = 0;

            // The supplied workbook may have unrelated sheets before 'Sheet1'.
            // Select the sheet by column headings, not by sheet order/name.
            foreach (var worksheet in workbook.Worksheets)
            {
                var header = worksheet.FirstRowUsed();
                if (header == null) continue;

                var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var cell in header.CellsUsed())
                {
                    var name = NormalizeHeader(cell.GetString());
                    if (name.Length > 0 && !map.ContainsKey(name))
                        map.Add(name, cell.Address.ColumnNumber);
                }

                if (map.ContainsKey("EXISTINGSUBDEALERID") && map.ContainsKey("NEWSUBDEALERID"))
                {
                    if (correctionSheet != null)
                        return BadRequest(new
                        {
                            Success = false,
                            Message = "More than one worksheet has the correction columns. Keep the mapping on one sheet only."
                        });
                    correctionSheet = worksheet;
                    correctionHeaders = map;
                    headerRowNumber = header.RowNumber();
                }
            }

            if (correctionSheet == null || correctionHeaders == null)
                return BadRequest(new
                {
                    Success = false,
                    Message = "No worksheet contains both 'Existing Sub-Dealer ID' and 'New Sub-Dealer ID' columns."
                });

            rows = new List<SubDealerCodeCorrectionRow>();
            var lastRow = correctionSheet.LastRowUsed()?.RowNumber() ?? headerRowNumber;
            if (lastRow - headerRowNumber > 5000)
                return BadRequest(new { Success = false, Message = "The correction sheet spans more than 5000 rows. Remove distant/unused rows." });
            for (var rowNumber = headerRowNumber + 1; rowNumber <= lastRow; rowNumber++)
            {
                var source = correctionSheet.Row(rowNumber);
                var existingCell = source.Cell(correctionHeaders["EXISTINGSUBDEALERID"]);
                var newCell = source.Cell(correctionHeaders["NEWSUBDEALERID"]);
                if (existingCell.HasFormula || newCell.HasFormula)
                    return BadRequest(new { Success = false, Message = $"Excel row {rowNumber}: codes must be literal text, not formulas." });
                var existingCode = existingCell.GetString().Trim();
                var newCode = newCell.GetString().Trim();

                if (existingCode.Length == 0 && newCode.Length == 0) continue;
                rows.Add(new SubDealerCodeCorrectionRow
                {
                    ExcelRowNumber = rowNumber,
                    ExistingCode = existingCode,
                    NewCode = SubDealerFormModel.CodeValue(newCode)
                });

                if (rows.Count > 2000)
                    return BadRequest(new { Success = false, Message = "Maximum 2000 correction rows per Excel file." });
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return BadRequest(new
            {
                Success = false,
                Message = "The Excel file could not be read. Check that it is a valid .xlsx workbook with text code values."
            });
        }

        if (rows.Count == 0)
            return BadRequest(new { Success = false, Message = "No correction rows found in the Excel file." });

        var result = new SubDealerCodeCorrectionResult
        {
            TotalRows = rows.Count,
            Rows = rows
        };

        var duplicateOldCodes = rows.Where(r => r.ExistingCode.Length > 0)
            .GroupBy(r => NormalizeCode(r.ExistingCode), StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        var duplicateNewCodes = rows.Where(r => r.NewCode.Length > 0)
            .GroupBy(r => NormalizeCode(r.NewCode), StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.Ordinal);

        var neededCodes = rows.SelectMany(r => new[] { NormalizeCode(r.ExistingCode), NormalizeCode(r.NewCode) })
            .Where(c => c.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        try
        {
            // Serializable transaction for Apply: check collisions and save together.
            // Preview runs read-only without opening a transaction.
            await using var transaction = apply
                ? await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken)
                : null;

            IQueryable<SubDealerRegistration> query = _db.SubDealerRegistrations;
            if (!apply) query = query.AsNoTracking();

            // Resolve both old codes and potential target collisions against the DB.
            var matched = await query
                .Where(x => x.SubDealerCode != null &&
                            neededCodes.Contains(x.SubDealerCode.Trim().ToUpper()))
                .ToListAsync(cancellationToken);

            // A legacy database can already contain duplicate codes. Reject ambiguity;
            // never select the first match silently when changing an identifier.
            var byCode = matched
                .GroupBy(x => NormalizeCode(x.SubDealerCode), StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            var updates = new List<(SubDealerCodeCorrectionRow Row, SubDealerRegistration Entity)>();
            foreach (var row in rows)
            {
                var oldKey = NormalizeCode(row.ExistingCode);
                var newKey = NormalizeCode(row.NewCode);
                string? error = null;

                if (oldKey.Length == 0 || newKey.Length == 0)
                    error = "Both old and new codes are required.";
                else if (duplicateOldCodes.Contains(oldKey))
                    error = "Existing code is repeated in the Excel file.";
                else if (duplicateNewCodes.Contains(newKey))
                    error = "New code is repeated in the Excel file.";
                else if (GetExcelCodeError(row.NewCode) is string newCodeError)
                    error = newCodeError;
                else if (!byCode.TryGetValue(oldKey, out var found) || found.Count == 0)
                    error = "Existing code not found in SubDealerRegistrations.";
                else if (found.Count != 1)
                    error = "More than one Sub Dealer has this existing code. Correct duplicates manually.";
                else
                {
                    var entity = found[0];
                    row.FirmName = entity.FirmName ?? string.Empty;

                    if (byCode.TryGetValue(newKey, out var owners) &&
                        owners.Any(x => x.Id != entity.Id))
                        error = "New code already belongs to a different Sub Dealer.";
                    else if (oldKey == newKey)
                    {
                        row.Status = "Unchanged";
                        row.Message = "Already has this code.";
                    }
                    else
                    {
                        row.Status = "Ready";
                        row.Message = "SubDealerCode and modification audit will change; creation history and other business fields are preserved.";
                        updates.Add((row, entity));
                    }
                }

                if (error != null)
                {
                    row.Status = "Error";
                    row.Message = error;
                }
            }

            result.Ready = rows.Count(x => x.Status == "Ready");
            result.Unchanged = rows.Count(x => x.Status == "Unchanged");
            result.Errors = rows.Count(x => x.Status == "Error");
            result.Success = result.Errors == 0;

            if (!apply)
            {
                result.Message = result.Success
                    ? $"Preview ready: {result.Ready} code(s) can be changed, {result.Unchanged} already correct. Nothing saved."
                    : $"Preview detected {result.Errors} error(s). Nothing saved. Correct the Excel and preview again.";
                return Ok(result);
            }

            // Atomic: if ANY row is invalid, none of the rows are saved.
            if (result.Errors > 0)
            {
                result.Message = $"Upload rejected: {result.Errors} error(s). No code was changed.";
                return Conflict(result);
            }

            foreach (var (row, entity) in updates)
            {
                entity.SubDealerCode = row.NewCode;
                StampModified(_db, _configuration, entity, CurrentUserId()!);
                // All other business fields and CreatedAt/CreatedBy remain unchanged.
            }

            if (updates.Count > 0)
                await _db.SaveChangesAsync(cancellationToken);

            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            foreach (var (row, _) in updates)
            {
                row.Status = "Updated";
                row.Message = "SubDealerCode changed successfully.";
            }

            result.Updated = updates.Count;
            result.Message = $"Completed: {result.Updated} Sub Dealer Code(s) updated; {result.Unchanged} unchanged. No new records created.";
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return StatusCode(409, new
            {
                Success = false,
                Message = "Database rejected the update (possibly a duplicate code or concurrent change). No batch changes were committed. Recheck the data and preview again."
            });
        }
        catch (Exception)
        {
            return StatusCode(500, new
            {
                Success = false,
                Message = "Unable to complete code correction. The update was not confirmed; verify the database before retrying."
            });
        }
    }
    // LEGACY MISSING-CODE REPAIR (SuperAdmin only, separate from existing code-to-code Excel correction).
    // A genuine externally verified code is supplied with the DATABASE record Id.
    // Preview is read-only; Apply rechecks the code, record identity, and collisions.
    public sealed class MissingCodeRepairRequest
    {
        public int Id { get; set; }
        public string? ActualCode { get; set; }
        public string? ExpectedFirmName { get; set; }
    }

    public sealed class MissingCodeRepairResult
    {
        public bool Success { get; set; }
        public bool Ready { get; set; }
        public bool Updated { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Id { get; set; }
        public string FirmName { get; set; } = string.Empty;
        public string ExistingCode { get; set; } = string.Empty;
        public string ActualCode { get; set; } = string.Empty;
    }

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("missing-code-repair/preview")]
    public Task<ActionResult<MissingCodeRepairResult>> PreviewMissingCodeRepair(
        [FromBody] MissingCodeRepairRequest request,
        CancellationToken cancellationToken) =>
        ProcessMissingCodeRepair(request, false, cancellationToken);

    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("missing-code-repair/apply")]
    public Task<ActionResult<MissingCodeRepairResult>> ApplyMissingCodeRepair(
        [FromBody] MissingCodeRepairRequest request,
        CancellationToken cancellationToken) =>
        ProcessMissingCodeRepair(request, true, cancellationToken);

    private async Task<ActionResult<MissingCodeRepairResult>> ProcessMissingCodeRepair(
        MissingCodeRepairRequest request,
        bool apply,
        CancellationToken cancellationToken)
    {
        var result = new MissingCodeRepairResult
        {
            Id = request?.Id ?? 0,
            ActualCode = SubDealerFormModel.CodeValue(request?.ActualCode)
        };

        if (request == null || request.Id <= 0)
        {
            result.Message = "A valid existing database Sub Dealer Id is required.";
            return BadRequest(result);
        }

        if (!HasRole(User, "SuperAdmin")) return Forbid();
        if (string.IsNullOrWhiteSpace(CurrentUserId())) return Unauthorized();
        var codeError = GetExcelCodeError(request.ActualCode);
        if (codeError != null)
        {
            result.Message = codeError;
            return BadRequest(result);
        }

        if (result.ActualCode.Length > 50)
        {
            result.Message = "Actual Sub Dealer Code must be 50 characters or fewer.";
            return BadRequest(result);
        }

        // An Apply must have the expected firm name from a successful preview.
        // This prevents correcting an unrelated Id if the record changed in between.
        if (apply && string.IsNullOrWhiteSpace(request.ExpectedFirmName))
        {
            result.Message = "Preview the exact record and verify its Firm Name before applying.";
            return BadRequest(result);
        }

        try
        {
            await using var transaction = apply
                ? await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken)
                : null;

            IQueryable<SubDealerRegistration> query = _db.SubDealerRegistrations;
            if (!apply) query = query.AsNoTracking();

            var entity = await query.SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
            if (entity == null)
            {
                result.Message = "Sub Dealer record Id was not found. Nothing changed.";
                return NotFound(result);
            }

            result.FirmName = entity.FirmName ?? string.Empty;
            result.ExistingCode = entity.SubDealerCode ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(entity.SubDealerCode))
            {
                result.Message = "This record already has a Sub Dealer Code. Existing genuine codes cannot be replaced using Missing Code Repair.";
                return Conflict(result);
            }

            if (apply && !string.Equals(
                    request.ExpectedFirmName?.Trim(),
                    result.FirmName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                result.Message = "Firm Name changed since preview. Nothing changed; preview again.";
                return Conflict(result);
            }

            var actualKey = NormalizeCode(result.ActualCode);
            var duplicate = await _db.SubDealerRegistrations.AsNoTracking().AnyAsync(
                x => x.Id != request.Id &&
                     x.SubDealerCode != null &&
                     x.SubDealerCode.Trim().ToUpper() == actualKey,
                cancellationToken);

            if (duplicate)
            {
                result.Message = "This actual Sub Dealer Code already belongs to a different record. Nothing changed.";
                return Conflict(result);
            }

            if (!apply)
            {
                result.Success = true;
                result.Ready = true;
                result.Message = "Ready. Verify the database Id, Firm Name and genuine Sub Dealer Code, then confirm Apply.";
                return Ok(result);
            }

            // Correct one business field; record the real modifier/time without rewriting creation history.
            entity.SubDealerCode = result.ActualCode;
            StampModified(_db, _configuration, entity, CurrentUserId()!);
            await _db.SaveChangesAsync(cancellationToken);
            if (transaction != null)
                await transaction.CommitAsync(cancellationToken);

            result.Success = true;
            result.Updated = true;
            result.Message = $"Actual Sub Dealer Code saved for database record {request.Id}. Other business fields and creation history were preserved; modification audit was recorded.";
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            result.Message = "Database rejected the correction. No change confirmed; verify the record before retrying.";
            return Conflict(result);
        }
        catch (Exception)
        {
            result.Message = "Correction could not be confirmed. Check database state before retrying.";
            return StatusCode(500, result);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<SubDealerFormModel>> Update(
        int id, [FromBody] SubDealerFormModel model, CancellationToken cancellationToken)
    {
        if (!CanMaintain(User)) return Forbid();
        var actor = CurrentUserId();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (model == null || id <= 0 || model.Id != id) return BadRequest("Route Id and model Id do not match.");
        var commitStarted = false;
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var entity = await Scoped(tracking: true).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return NotFound("Sub Dealer not found in your permitted scope.");
            if (GetExcelCodeError(entity.SubDealerCode) is string codeError)
                return BadRequest($"The selected Sub Dealer master has an invalid Sub Dealer Code. Admin must repair the master record before profile maintenance.");

            if (string.IsNullOrWhiteSpace(model.SubDealerCode))
                return BadRequest(new { message = "Sub Dealer Code cannot be changed." });

            var existingCodeKey = SubDealerFormModel.CodeKey(entity.SubDealerCode);
            var submittedCodeKey = SubDealerFormModel.CodeKey(model.SubDealerCode);
            if (!string.Equals(existingCodeKey, submittedCodeKey, StringComparison.Ordinal))
                return BadRequest(new { message = "Sub Dealer Code cannot be changed." });

            // Compare the version loaded by the page. Do not accept stale full-form overwrites.
            if (model.UpdatedAt == default || model.UpdatedAt != entity.UpdatedAt)
                return Conflict("This record changed after you loaded it. Reload it and reapply your edits; nothing was saved.");

            // The server owns master identity/hierarchy and creation/modification audit.
            model.SubDealerCode = entity.SubDealerCode;
            model.StateId = entity.StateId;
            model.Region = entity.Region;
            model.HQ = entity.HQ;
            NormalizeProfile(model);
            var validationError = ValidateModel(model);
            if (validationError != null) return BadRequest(validationError);
            var locationError = await ValidatePrimaryLocationAsync(model, cancellationToken);
            if (locationError != null) return BadRequest(locationError);
            if (!string.IsNullOrWhiteSpace(model.GSTFilePath))
            {
                var physical = SubDealerFileController.FindExisting(_environment, _configuration, model.GSTFilePath, id);
                if (physical == null)
                    return BadRequest("Upload a GST PDF for this Sub Dealer before saving. The supplied file reference is invalid or no longer exists.");
                SubDealerFileController.TryParse(model.GSTFilePath, out _, out var fileName);
                model.GSTFilePath = SubDealerFileController.LogicalPath(id, fileName);
            }

            // Explicitly map only editable profile fields; do not overwrite protected master fields.
            entity.Status = model.Status;
            entity.FirmName = model.FirmName.Trim().ToUpperInvariant();
            entity.GoogleMapURL = model.GoogleMapURL;
            entity.Latitude = model.Latitude;
            entity.Longitude = model.Longitude;
            entity.ShopNoORRoomNoOrBlockNo = model.ShopNoORRoomNoOrBlockNo ?? string.Empty;
            entity.Street = model.Street;
            entity.SubVillage = model.SubVillage;
            entity.Village = model.Village ?? string.Empty;
            entity.PinCode = model.PinCode ?? string.Empty;
            entity.Block = model.Block;
            entity.Taluk = model.Taluk;
            entity.DistrictId = model.DistrictId;
            entity.DealerStateId = model.DealerStateId;
            entity.OfficialContactNumber = model.OfficialContactNumber ?? string.Empty;
            entity.WhatsAppNumber = model.WhatsAppNumber ?? string.Empty;
            entity.AlternativeNumber = model.AlternativeNumber;
            entity.WholesaleMFMSId = string.IsNullOrWhiteSpace(model.WholesaleMFMSId) ? null : model.WholesaleMFMSId.Trim();
            entity.RetailMFMSId = string.IsNullOrWhiteSpace(model.RetailMFMSId) ? null : model.RetailMFMSId.Trim();
            entity.PANNo = string.IsNullOrWhiteSpace(model.PANNo) ? null : model.PANNo.Trim().ToUpperInvariant();
            entity.GSTNumber = model.GSTNumber?.Trim().ToUpperInvariant();
            entity.GSTLegalName = model.GSTLegalName;
            entity.GSTTradeName = model.GSTTradeName;
            entity.GSTConstitutionofBusiness = model.GSTConstitutionofBusiness;
            entity.GSTFilePath = model.GSTFilePath;
            StampModified(_db, _configuration, entity, actor);
            await _db.SaveChangesAsync(cancellationToken);
            // Return the database's actual timestamp precision to the next edit request.
            await _db.Entry(entity).ReloadAsync(cancellationToken);
            commitStarted = true;
            await transaction.CommitAsync(cancellationToken);
            return Ok(ToModel(entity));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sub Dealer profile save failed for Id {SubDealerId}", id);
            return commitStarted
                ? StatusCode(503, "The save outcome could not be confirmed. Reload the record before retrying.")
                : Conflict("The record could not be saved, possibly due to a concurrent change. Reload and retry; this transaction was not committed.");
        }
    }

    private static string? GetExcelCodeError(string? rawCode) => SubDealerFormModel.CodeError(rawCode);

    /// <summary>
    /// Admin roles only: reads the Excel master and returns the five required text columns.
    /// State/Region/HQ names are mapped to IDs by the UI using the same LookupCache
    /// already used by the Register page.
    /// </summary>
    [Authorize(Roles = AdminRoles)]
    [HttpPost("parse-import-excel")]
    [RequestSizeLimit(MaxExcelImportSize + 2 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxExcelImportSize + 2 * 1024 * 1024)]
    public async Task<ActionResult<SubDealerExcelParseResponse>> ParseImportExcel(
        IFormFile file,
        CancellationToken cancellationToken,
        [FromQuery] bool partial = false)
    {
        // partial=true: row-level problems (missing values, duplicate codes) are NOT
        // reported as file-level errors. Every non-blank row is returned and the
        // caller validates each row so valid rows can still be imported.
        if (!CanImport(User)) return Forbid();
        var result = new SubDealerExcelParseResponse();

        if (file == null || file.Length == 0)
            return BadRequest("Excel file is required.");

        if (file.Length > MaxExcelImportSize)
            return BadRequest("Excel file size must be less than 10 MB.");

        if (!string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only .xlsx Excel files are allowed.");
        }

        try
        {
            await using var source = file.OpenReadStream();
            using var memory = new MemoryStream();
            await source.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;

            using var workbook = new XLWorkbook(memory);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
                return BadRequest("The Excel workbook does not contain a worksheet.");

            var firstRow = worksheet.FirstRowUsed();
            var lastRow = worksheet.LastRowUsed();

            if (firstRow == null || lastRow == null)
                return BadRequest("The Excel worksheet is empty.");

            var headerRowNumber = firstRow.RowNumber();
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var cell in firstRow.CellsUsed())
            {
                var header = NormalizeHeader(cell.GetString());
                if (!string.IsNullOrWhiteSpace(header) && !headerMap.TryAdd(header, cell.Address.ColumnNumber))
                    return BadRequest($"Duplicate Excel header '{header}'. Keep each master column once; nothing was saved.");
            }

            var requiredHeaders = new[]
            {
                "SUBDEALERCODE",
                "SUBDEALERNAME",
                "HQ",
                "REGION",
                "STATE"
            };

            var missingHeaders = requiredHeaders
                .Where(x => !headerMap.ContainsKey(x))
                .ToList();

            if (missingHeaders.Count > 0)
            {
                result.Errors.Add(
                    "Missing required Excel column(s): " +
                    string.Join(", ", missingHeaders.Select(ToDisplayHeader)));
                return Ok(result);
            }

            var seenTextRows = new Dictionary<string, (int RowNumber, string Name, string Hq, string Region, string State)>(StringComparer.OrdinalIgnoreCase);

            for (var rowNumber = headerRowNumber + 1;
                 rowNumber <= lastRow.RowNumber();
                 rowNumber++)
            {
                var row = worksheet.Row(rowNumber);

                var codeCell = row.Cell(headerMap["SUBDEALERCODE"]);
                if (codeCell.HasFormula)
                    return BadRequest($"Excel row {rowNumber}: Sub Dealer Code must be literal text, not a formula. Nothing was saved.");
                var code = codeCell.GetString().Trim();
                var name = row.Cell(headerMap["SUBDEALERNAME"]).GetString().Trim();
                var hq = row.Cell(headerMap["HQ"]).GetString().Trim();
                var region = row.Cell(headerMap["REGION"]).GetString().Trim();
                var state = row.Cell(headerMap["STATE"]).GetString().Trim();

                // Ignore completely blank rows.
                if (string.IsNullOrWhiteSpace(code) &&
                    string.IsNullOrWhiteSpace(name) &&
                    string.IsNullOrWhiteSpace(hq) &&
                    string.IsNullOrWhiteSpace(region) &&
                    string.IsNullOrWhiteSpace(state))
                {
                    continue;
                }

                if (!partial)
                {
                    var codeError = GetExcelCodeError(code);
                    if (codeError != null)
                        result.Errors.Add($"Excel row {rowNumber}: {codeError}");

                    if (string.IsNullOrWhiteSpace(name))
                        result.Errors.Add($"Excel row {rowNumber}: Sub Dealer Name is required.");

                    if (string.IsNullOrWhiteSpace(hq))
                        result.Errors.Add($"Excel row {rowNumber}: HQ is required.");

                    if (string.IsNullOrWhiteSpace(region))
                        result.Errors.Add($"Excel row {rowNumber}: Region is required.");

                    if (string.IsNullOrWhiteSpace(state))
                        result.Errors.Add($"Excel row {rowNumber}: State is required.");

                    if (!string.IsNullOrWhiteSpace(code))
                    {
                        if (seenTextRows.TryGetValue(code, out var earlier))
                        {
                            bool sameMapping =
                                string.Equals(earlier.Name, name, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(earlier.Hq, hq, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(earlier.Region, region, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(earlier.State, state, StringComparison.OrdinalIgnoreCase);

                            if (!sameMapping)
                            {
                                result.Errors.Add($"Excel row {rowNumber}: Sub Dealer Code {code} already appears in this Excel with a different Firm / State / Region / HQ mapping.");
                            }
                        }
                        else
                        {
                            seenTextRows[code] = (rowNumber, name, hq, region, state);
                        }
                    }
                }

                result.Rows.Add(new SubDealerExcelRowDto
                {
                    RowNumber = rowNumber,
                    SubDealerCode = code,
                    SubDealerName = name,
                    HQ = hq,
                    Region = region,
                    State = state
                });
            }

            result.TotalRows = result.Rows.Count;
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sub Dealer Excel parsing failed");
            return BadRequest("Unable to read Excel file. Check the .xlsx format, text codes and required headers; nothing was saved.");
        }
    }

    /// <summary>
    /// Admin-only master import. Every UI import is previewed first; no database row is
    /// changed until the caller explicitly confirms. Existing rows with identical master
    /// values are counted as Unchanged and are not written again, preserving UpdatedAt.
    /// </summary>
    [Authorize(Roles = AdminRoles)]
    [HttpPost("bulk-import")]
    public async Task<ActionResult<SubDealerBulkImportResponse>> BulkImport(
        [FromBody] SubDealerBulkImportRequest request,
        CancellationToken cancellationToken,
        [FromQuery] bool partial = false,
        [FromQuery] bool previewOnly = false,
        [FromQuery] bool confirm = false)
    {
        if (!CanImport(User)) return Forbid();
        var actor = CurrentUserId();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (request?.Rows == null || request.Rows.Count == 0)
            return BadRequest("No Sub Dealer rows were supplied for import.");
        if (request.Rows.Any(x => x == null))
            return BadRequest("The import request contains an invalid null row.");

        // A save request must always be explicit. This prevents a caller from accidentally
        // bypassing the preview/Proceed step and changing the master immediately.
        if (!previewOnly && !confirm)
            return BadRequest("Import confirmation is required. Preview the Excel first, then proceed explicitly.");

        var result = new SubDealerBulkImportResponse
        {
            PreviewOnly = previewOnly
        };

        var rows = new List<SubDealerBulkImportRowDto>();

        void Reject(SubDealerBulkImportRowDto row, string category, string reason)
        {
            if (partial) result.Rejected.Add(ToRejected(row, category, reason));
            else result.Errors.Add($"Excel row {row.ExcelRowNumber}: {reason}");
        }

        foreach (var row in request.Rows)
        {
            var reasons = new List<string>();
            if (GetExcelCodeError(row.SubDealerCode) is string error) reasons.Add(error);
            if (string.IsNullOrWhiteSpace(row.FirmName)) reasons.Add("Sub Dealer Name is required.");
            if (row.StateId <= 0 || row.RegionId <= 0 || row.HQId <= 0)
                reasons.Add("State, Region and HQ mappings are required.");

            if (reasons.Count > 0)
            {
                Reject(row, SubDealerImportRejectCategory.Validation, string.Join(" ", reasons));
                continue;
            }

            rows.Add(row);
        }

        if (!partial && result.Errors.Count > 0) return BadRequest(result);
        if (rows.Count == 0) return Ok(result);

        var commitStarted = false;
        try
        {
            // Preview and save both use the same validation path. Save repeats all checks in
            // a fresh serializable transaction, so a stale preview cannot silently overwrite
            // a concurrent master change.
            await using var transaction = await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

            var hqIds = rows.Select(x => x.HQId).Distinct().ToList();
            var hierarchy = await (from h in _db.Headquarters.AsNoTracking()
                                   join r in _db.Regions.AsNoTracking() on h.RegionId equals r.Id
                                   join st in _db.States.AsNoTracking() on r.StateId equals st.Id
                                   where hqIds.Contains(h.Id)
                                   select new { HqId = h.Id, RegionId = r.Id, StateId = st.Id })
                                  .ToDictionaryAsync(x => x.HqId, cancellationToken);

            var valid = new List<SubDealerBulkImportRowDto>();
            var seenInRequest = new Dictionary<string, SubDealerBulkImportRowDto>(StringComparer.Ordinal);
            foreach (var row in rows)
            {
                if (!hierarchy.TryGetValue(row.HQId, out var master) ||
                    master.RegionId != row.RegionId ||
                    master.StateId != row.StateId)
                {
                    Reject(
                        row,
                        SubDealerImportRejectCategory.Validation,
                        "HQ does not belong to the supplied Region and State. Correct the Excel hierarchy; it was not reassigned automatically.");
                    continue;
                }

                var codeKey = NormalizeCode(row.SubDealerCode);
                if (seenInRequest.TryGetValue(codeKey, out var earlierRow))
                {
                    bool sameMapping =
                        string.Equals((earlierRow.FirmName ?? string.Empty).Trim(), (row.FirmName ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase) &&
                        earlierRow.StateId == row.StateId &&
                        earlierRow.RegionId == row.RegionId &&
                        earlierRow.HQId == row.HQId;

                    if (sameMapping)
                    {
                        Reject(
                            row,
                            SubDealerImportRejectCategory.Duplicate,
                            $"Sub Dealer Code {row.SubDealerCode} already appears in this Excel with the same mapping; row {earlierRow.ExcelRowNumber} was processed.");
                    }
                    else
                    {
                        Reject(
                            row,
                            SubDealerImportRejectCategory.Duplicate,
                            $"Sub Dealer Code {row.SubDealerCode} already appears in this Excel with a different Firm / State / Region / HQ mapping.");
                    }
                    continue;
                }

                seenInRequest[codeKey] = row;
                valid.Add(row);
            }

            if (!partial && result.Errors.Count > 0) return BadRequest(result);
            rows = valid;
            if (rows.Count == 0) return Ok(result);

            var codes = rows
                .Select(x => NormalizeCode(x.SubDealerCode))
                .Distinct()
                .ToList();

            var existing = await _db.SubDealerRegistrations
                .Where(x => x.SubDealerCode != null &&
                            codes.Contains(x.SubDealerCode.Trim().ToUpper()))
                .ToListAsync(cancellationToken);

            var byCode = existing
                .GroupBy(x => NormalizeCode(x.SubDealerCode))
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);

            valid = new List<SubDealerBulkImportRowDto>();
            foreach (var row in rows)
            {
                if (byCode.TryGetValue(NormalizeCode(row.SubDealerCode), out var matches) &&
                    matches.Count != 1)
                {
                    Reject(
                        row,
                        SubDealerImportRejectCategory.Duplicate,
                        "Multiple existing masters have this code. Resolve the ambiguity; no record was chosen automatically.");
                }
                else
                {
                    valid.Add(row);
                }
            }

            if (!partial && result.Errors.Count > 0) return BadRequest(result);
            rows = valid;
            if (rows.Count == 0) return Ok(result);

            var existingStateIds = existing.Select(x => x.StateId).Distinct().ToList();
            var existingRegionIds = existing.Select(x => x.Region).Distinct().ToList();
            var existingHqIds = existing.Select(x => x.HQ).Distinct().ToList();

            var stateMap = await _db.States
                .Where(s => existingStateIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.StateName, cancellationToken);
            var regionMap = await _db.Regions
                .Where(r => existingRegionIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.RegionName, cancellationToken);
            var hqMap = await _db.Headquarters
                .Where(h => existingHqIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, h => h.HeadquarterName, cancellationToken);

            foreach (var row in rows)
            {
                var key = NormalizeCode(row.SubDealerCode);
                var targetFirmName = row.FirmName.Trim().ToUpperInvariant();

                if (byCode.TryGetValue(key, out var matches))
                {
                    var entity = matches[0]; // uniqueness was explicitly verified above
                    var unchanged =
                        string.Equals(
                            (entity.FirmName ?? string.Empty).Trim(),
                            targetFirmName,
                            StringComparison.OrdinalIgnoreCase) &&
                        entity.StateId == row.StateId &&
                        entity.Region == row.RegionId &&
                        entity.HQ == row.HQId;

                    if (unchanged)
                    {
                        result.Unchanged++;
                        continue;
                    }

                    // Any difference in FirmName, StateId, RegionId, HQId is rejected!
                    // Bulk Upload must never update an existing Sub Dealer record.
                    var exStateName = stateMap.TryGetValue(entity.StateId, out var sn) && !string.IsNullOrWhiteSpace(sn) ? sn : $"State {entity.StateId}";
                    var exRegionName = regionMap.TryGetValue(entity.Region, out var rn) && !string.IsNullOrWhiteSpace(rn) ? rn : $"Region {entity.Region}";
                    var exHqName = hqMap.TryGetValue(entity.HQ, out var hn) && !string.IsNullOrWhiteSpace(hn) ? hn : $"HQ {entity.HQ}";

                    var warningReason =
                        $"Sub Dealer Code {row.SubDealerCode} is already available.\n\n" +
                        $"Existing mapping:\n" +
                        $"Firm Name : {entity.FirmName}\n" +
                        $"State     : {exStateName}\n" +
                        $"Region    : {exRegionName}\n" +
                        $"HQ        : {exHqName}\n\n" +
                        $"Uploaded file contains a different Firm / State / Region / HQ mapping.\n\n" +
                        $"Bulk Upload cannot modify an existing Sub Dealer master.\n" +
                        $"If you want to change this mapping, please contact Admin.";

                    Reject(
                        row,
                        SubDealerImportRejectCategory.Validation,
                        warningReason);
                    continue;
                }
                else
                {
                    result.Inserted++;
                    if (previewOnly)
                        continue;

                    var entity = new SubDealerRegistration
                    {
                        SubDealerCode = SubDealerFormModel.CodeValue(row.SubDealerCode),
                        FirmName = targetFirmName,
                        StateId = row.StateId,
                        Region = row.RegionId,
                        HQ = row.HQId,
                        Status = SubDealerStatus.Active,
                        ShopNoORRoomNoOrBlockNo = string.Empty,
                        Village = string.Empty,
                        PinCode = string.Empty,
                        OfficialContactNumber = string.Empty,
                        WhatsAppNumber = string.Empty
                    };

                    StampCreated(_db, _configuration, entity, actor);
                    _db.SubDealerRegistrations.Add(entity);
                    byCode[key] = new List<SubDealerRegistration> { entity };
                }
            }

            if (previewOnly)
            {
                result.RequiresConfirmation = result.Inserted + result.Unchanged > 0;
                await transaction.RollbackAsync(cancellationToken);
                return Ok(result);
            }

            await _db.SaveChangesAsync(cancellationToken);
            commitStarted = true;
            await transaction.CommitAsync(cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, previewOnly
                ? "Sub Dealer master import preview failed"
                : "Sub Dealer master import failed");

            result.Inserted = 0;
            result.Updated = 0;
            result.Unchanged = 0;
            result.RequiresConfirmation = false;

            if (commitStarted)
                return StatusCode(
                    503,
                    "Import commit outcome is unknown. Verify the master before retrying; no saved count is being claimed.");

            var reason = previewOnly
                ? "Import preview failed. Nothing was saved. Check the hierarchy, duplicate-code constraints and database connection, then retry."
                : "The import transaction was not committed. Check the hierarchy, duplicate-code constraints and concurrent imports, then retry.";

            result.Errors.Add(reason);
            if (!partial) return BadRequest(result);
            foreach (var row in rows)
                result.Rejected.Add(ToRejected(row, SubDealerImportRejectCategory.DatabaseError, reason));
            return Ok(result);
        }
    }

    private static SubDealerImportRejectedRowDto ToRejected(
        SubDealerBulkImportRowDto row,
        string category,
        string reason) => new()
        {
            ExcelRowNumber = row.ExcelRowNumber,
            SubDealerCode = row.SubDealerCode?.Trim() ?? string.Empty,
            SubDealerName = row.FirmName?.Trim() ?? string.Empty,
            Category = category,
            Reason = reason
        };

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        Task.FromResult<IActionResult>(BadRequest(
            "Physical master deletion is disabled. Use the list's Delete Details action; the Id, actual code and master geography must remain."));

    [HttpDelete("{id:int}/details")]
    public async Task<IActionResult> DeleteSavedDetails(int id, CancellationToken cancellationToken,
        [FromQuery] DateTime? expectedUpdatedAt = null)
    {
        if (!CanMaintain(User)) return Forbid();
        var actor = CurrentUserId();
        if (string.IsNullOrWhiteSpace(actor)) return Unauthorized();
        if (!expectedUpdatedAt.HasValue) return BadRequest("Refresh the list before clearing details; the record version is required.");
        var commitStarted = false;
        try
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var entity = await Scoped(tracking: true).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            if (entity == null) return NotFound("Sub Dealer not found in your permitted scope.");
            if (GetExcelCodeError(entity.SubDealerCode) != null)
                return BadRequest("A SuperAdmin must repair the invalid master code before details can be cleared.");
            if (entity.UpdatedAt != expectedUpdatedAt.Value)
                return Conflict("This record changed. Refresh the list before confirming Delete Details.");

            // Clear only fields maintained by this form. Keep Id, code, master name,
            // State/Region/HQ, CreatedAt and CreatedBy. Detached PDFs are retained for safe recovery.
            ClearMaintainedDetails(entity);
            StampModified(_db, _configuration, entity, actor);
            await _db.SaveChangesAsync(cancellationToken);
            commitStarted = true;
            await transaction.CommitAsync(cancellationToken);
            return NoContent();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sub Dealer details reset failed for Id {SubDealerId}", id);
            return commitStarted ? StatusCode(503, "Reset outcome is unknown. Refresh the record before retrying.")
                : Conflict("Details were not cleared by this transaction. Refresh and retry.");
        }
    }

    private static void ClearMaintainedDetails(SubDealerRegistration entity)
    {
        // Reset every form-maintained detail back to the untouched master state.
        entity.Status = SubDealerStatus.Active;
        //entity.FirmName = string.Empty;

        entity.GoogleMapURL = null;
        entity.Latitude = null;
        entity.Longitude = null;
        entity.ShopNoORRoomNoOrBlockNo = string.Empty;
        entity.Street = null;
        entity.SubVillage = null;
        entity.Village = string.Empty;
        entity.PinCode = string.Empty;
        entity.Block = null;
        entity.Taluk = null;
        entity.DistrictId = null;
        entity.DealerStateId = null;
        entity.OfficialContactNumber = string.Empty;
        entity.WhatsAppNumber = string.Empty;
        entity.AlternativeNumber = null;

        entity.WholesaleMFMSId = null;
        entity.RetailMFMSId = null;
        entity.PANNo = null;

        entity.GSTNumber = null;
        entity.GSTLegalName = null;
        entity.GSTTradeName = null;
        entity.GSTConstitutionofBusiness = null;
        entity.GSTFilePath = null;
    }

    private static string NormalizeHeader(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
            return string.Empty;

        return new string(header
            .Trim()
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string ToDisplayHeader(string normalizedHeader)
    {
        return normalizedHeader switch
        {
            "SUBDEALERCODE" => "Sub Dealer Code",
            "SUBDEALERNAME" => "Sub Dealer Name",
            "HQ" => "HQ",
            "REGION" => "Region",
            "STATE" => "State",
            _ => normalizedHeader
        };
    }

    private static void NormalizeProfile(SubDealerFormModel model)
    {
        model.FirmName = (model.FirmName ?? string.Empty).Trim();
        model.PinCode = (model.PinCode ?? string.Empty).Trim();
        model.OfficialContactNumber = (model.OfficialContactNumber ?? string.Empty).Trim();
        model.WhatsAppNumber = (model.WhatsAppNumber ?? string.Empty).Trim();
        model.AlternativeNumber = model.AlternativeNumber?.Trim();
        model.PANNo = model.PANNo?.Trim().ToUpperInvariant();
        model.GSTNumber = model.GSTNumber?.Trim().ToUpperInvariant();
    }

    private async Task<string?> ValidatePrimaryLocationAsync(SubDealerFormModel model, CancellationToken cancellationToken)
    {
        if (model.DealerStateId.GetValueOrDefault() > 0 &&
            !await _db.States.AsNoTracking().AnyAsync(x => x.Id == model.DealerStateId, cancellationToken))
            return "Primary Location State is not in the master.";
        if (model.DistrictId.GetValueOrDefault() > 0 &&
            !await _db.Districts.AsNoTracking().AnyAsync(x => x.Id == model.DistrictId && x.StateId == model.DealerStateId, cancellationToken))
            return "Primary Location District does not belong to the selected State.";
        return null;
    }

    private static string? ValidateModel(SubDealerFormModel model) =>
        model.Validate(new ValidationContext(model)).FirstOrDefault()?.ErrorMessage;

    private static void ApplyModel(
        SubDealerRegistration entity,
        SubDealerFormModel model,
        bool applyMasterHierarchy = true)
    {
        if (applyMasterHierarchy)
        {
            entity.StateId = model.StateId;
            entity.Region = model.Region;
            entity.HQ = model.HQ;
        }

        entity.Status = model.Status;

        entity.FirmName = model.FirmName.Trim().ToUpperInvariant();

        entity.GoogleMapURL = model.GoogleMapURL;
        entity.Latitude = model.Latitude;
        entity.Longitude = model.Longitude;
        entity.ShopNoORRoomNoOrBlockNo = model.ShopNoORRoomNoOrBlockNo ?? string.Empty;
        entity.Street = model.Street;
        entity.SubVillage = model.SubVillage;
        entity.Village = model.Village ?? string.Empty;
        entity.PinCode = model.PinCode ?? string.Empty;
        entity.Block = model.Block;
        entity.Taluk = model.Taluk;
        entity.DistrictId = model.DistrictId;
        entity.DealerStateId = model.DealerStateId;
        entity.OfficialContactNumber = model.OfficialContactNumber ?? string.Empty;
        entity.WhatsAppNumber = model.WhatsAppNumber ?? string.Empty;
        entity.AlternativeNumber = model.AlternativeNumber;

        entity.WholesaleMFMSId = string.IsNullOrWhiteSpace(model.WholesaleMFMSId)
            ? null
            : model.WholesaleMFMSId.Trim();

        entity.RetailMFMSId = string.IsNullOrWhiteSpace(model.RetailMFMSId)
            ? null
            : model.RetailMFMSId.Trim();

        entity.PANNo = string.IsNullOrWhiteSpace(model.PANNo)
            ? null
            : model.PANNo.Trim().ToUpperInvariant();

        entity.GSTNumber = model.GSTNumber?.Trim().ToUpperInvariant();
        entity.GSTLegalName = model.GSTLegalName;
        entity.GSTTradeName = model.GSTTradeName;
        entity.GSTConstitutionofBusiness = model.GSTConstitutionofBusiness;
        entity.GSTFilePath = model.GSTFilePath;

        // Audit is assigned only by the authenticated server action, never from the client DTO.
    }

    private static SubDealerFormModel ToModel(SubDealerRegistration entity)
    {
        return new SubDealerFormModel
        {
            Id = entity.Id,
            SubDealerCode = entity.SubDealerCode ?? string.Empty,

            StateId = entity.StateId,
            Region = entity.Region,
            HQ = entity.HQ,
            Status = entity.Status,

            FirmName = entity.FirmName,

            GoogleMapURL = entity.GoogleMapURL,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            ShopNoORRoomNoOrBlockNo = entity.ShopNoORRoomNoOrBlockNo,
            Street = entity.Street,
            SubVillage = entity.SubVillage,
            Village = entity.Village,
            PinCode = entity.PinCode,
            Block = entity.Block,
            Taluk = entity.Taluk,
            DistrictId = entity.DistrictId,
            DealerStateId = entity.DealerStateId,
            OfficialContactNumber = entity.OfficialContactNumber,
            WhatsAppNumber = entity.WhatsAppNumber,
            AlternativeNumber = entity.AlternativeNumber,

            WholesaleMFMSId = entity.WholesaleMFMSId,
            RetailMFMSId = entity.RetailMFMSId,
            PANNo = entity.PANNo,

            GSTNumber = entity.GSTNumber,
            GSTLegalName = entity.GSTLegalName,
            GSTTradeName = entity.GSTTradeName,
            GSTConstitutionofBusiness = entity.GSTConstitutionofBusiness,
            GSTFilePath = entity.GSTFilePath,

            CreatedBy = entity.CreatedBy,
            CreatedAt = entity.CreatedAt,
            UpdatedBy = entity.UpdatedBy,
            UpdatedAt = entity.UpdatedAt
        };
    }

    // Shared scope and audit methods remain in this ORIGINAL controller file.
    internal const string AdminRoles = "Admin,SuperAdmin,CorporateAdmin";

    internal static bool HasRole(ClaimsPrincipal user, params string[] roles) =>
        user.Identity?.IsAuthenticated == true &&
        user.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "Role" || c.Type == "role")
            .Any(c => roles.Any(r => string.Equals(r, c.Value.Trim(), StringComparison.OrdinalIgnoreCase)));

    internal static bool IsAdmin(ClaimsPrincipal user) => HasRole(user, "Admin", "SuperAdmin", "CorporateAdmin");
    internal static bool CanImport(ClaimsPrincipal user) => IsAdmin(user);

    // Retain existing Director/AVP maintenance permissions; AVP remains zone-scoped.
    internal static bool CanMaintain(ClaimsPrincipal user) =>
        IsAdmin(user) || HasRole(user, "MO", "MDO", "JMDO", "Director", "AVP");

    internal static string? Actor(ClaimsPrincipal user) =>
        new[] { ClaimTypes.NameIdentifier, "sub", "spic:user_id" }
            .Select(t => user.FindFirst(t)?.Value)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static int ClaimId(ClaimsPrincipal user, params string[] names)
    {
        foreach (var name in names)
            if (int.TryParse(user.FindFirst(name)?.Value, out var id) && id > 0) return id;
        return 0;
    }

    internal static IQueryable<SubDealerRegistration> Scope(
        IQueryable<SubDealerRegistration> query, AppDbContext db, ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return query.Where(x => false);
        if (IsAdmin(user) || HasRole(user, "Director")) return query;

        if (HasRole(user, "AVP"))
        {
            var zoneId = ClaimId(user, "spic:zone_id", "ZoneId");
            if (zoneId == 0) return query.Where(x => false);
            var states = db.States.Where(s => s.ZoneId == zoneId).Select(s => s.Id);
            return query.Where(x => states.Contains(x.StateId));
        }

        var stateId = ClaimId(user, "spic:state_id", "StateId");
        var regionId = ClaimId(user, "spic:region_id", "RegionId");
        var hqId = ClaimId(user, "spic:hq_id", "HQId", "HqId", "HeadquartersId");
        if (HasRole(user, "MO", "MDO", "JMDO"))
        {
            if (hqId == 0) return query.Where(x => false);
            query = query.Where(x => x.HQ == hqId);
            if (regionId > 0) query = query.Where(x => x.Region == regionId);
            if (stateId > 0) query = query.Where(x => x.StateId == stateId);
            return query;
        }
        if (HasRole(user, "SMM", "SMD"))
            return stateId > 0 ? query.Where(x => x.StateId == stateId) : query.Where(x => false);
        if (HasRole(user, "RM", "RMD"))
        {
            if (regionId == 0) return query.Where(x => false);
            query = query.Where(x => x.Region == regionId);
            return stateId > 0 ? query.Where(x => x.StateId == stateId) : query;
        }
        // Unknown roles/missing claims must not fall through to unrestricted data.
        return query.Where(x => false);
    }

    // Preserve the supplied list's maintained-record definition, and reuse it for counts.
    // File upload itself does not set GSTFilePath in the database: only a successful profile save does.
    internal static readonly Expression<Func<SubDealerRegistration, bool>> HasMaintainedDetails = x =>
        x.Status == SubDealerStatus.InActive ||
        x.Latitude.HasValue || x.Longitude.HasValue || x.DistrictId.HasValue || x.DealerStateId.HasValue ||
        !string.IsNullOrEmpty(x.ShopNoORRoomNoOrBlockNo) || !string.IsNullOrEmpty(x.Village) ||
        !string.IsNullOrEmpty(x.PinCode) || !string.IsNullOrEmpty(x.OfficialContactNumber) ||
        !string.IsNullOrEmpty(x.WhatsAppNumber) || !string.IsNullOrEmpty(x.AlternativeNumber) ||
        !string.IsNullOrEmpty(x.GSTNumber) || !string.IsNullOrEmpty(x.GSTFilePath) ||
        !string.IsNullOrEmpty(x.PANNo) || !string.IsNullOrEmpty(x.WholesaleMFMSId) ||
        !string.IsNullOrEmpty(x.RetailMFMSId);

    private static DateTime AuditAt(AppDbContext db, IConfiguration config, string property, DateTime utc)
    {
        var mapped = db.Model.FindEntityType(typeof(SubDealerRegistration))?.FindProperty(property);
        if (mapped == null) throw new InvalidOperationException("SubDealer audit property mapping is missing.");
        string? storeType = null;
        if (db.Database.IsRelational())
        {
            storeType = mapped.GetColumnType();
        }
        if (storeType != null && (storeType.Contains("with time zone", StringComparison.OrdinalIgnoreCase) ||
            storeType.StartsWith("timestamptz", StringComparison.OrdinalIgnoreCase)))
            return new DateTime(utc.Ticks / 10 * 10, DateTimeKind.Utc);

        // The supplied historical model uses timestamp WITHOUT time zone.
        // Default to the existing server timezone, rather than silently shifting historical wall times.
        var zoneId = config["SubDealer:AuditTimeZoneId"];
        var zone = string.IsNullOrWhiteSpace(zoneId) ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        return new DateTime(local.Ticks / 10 * 10, DateTimeKind.Unspecified);
    }

    private static void StampCreated(AppDbContext db, IConfiguration config, SubDealerRegistration entity, string actor)
    {
        var utc = DateTime.UtcNow;
        entity.CreatedBy = actor;
        entity.CreatedAt = AuditAt(db, config, nameof(entity.CreatedAt), utc);
        entity.UpdatedBy = actor;
        entity.UpdatedAt = AuditAt(db, config, nameof(entity.UpdatedAt), utc);
    }

    private static void StampModified(AppDbContext db, IConfiguration config, SubDealerRegistration entity, string actor)
    {
        // CreatedAt and CreatedBy are deliberately never assigned during updates.
        entity.UpdatedBy = actor;
        var next = AuditAt(db, config, nameof(entity.UpdatedAt), DateTime.UtcNow);
        // Make two operations in the same microsecond distinguishable without changing historical dates.
        entity.UpdatedAt = next == entity.UpdatedAt ? next.AddTicks(10) : next;
    }
}