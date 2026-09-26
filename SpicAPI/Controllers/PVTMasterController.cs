using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PVTMasterController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PVTMasterController(AppDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // GET ALL ACTIVE PVT MASTER RECORDS
        // ============================================================

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var records = await _context.PVTMasters
                    .Where(x => x.IsActive)
                    .Select(x => new
                    {
                        x.Id,
                        x.Code,
                        x.Name,
                        x.StateId,
                        x.IsSpecialityProduct,
                        x.CreatedAt,
                        x.UpdatedAt
                    })
                    .OrderBy(x => x.Name)
                    .ToListAsync();

                return Ok(records);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Unable to load PVT Master data: {ex.Message}"
                });
            }
        }

        // ============================================================
        // SEARCH PVT MASTER
        // ============================================================

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new
                {
                    message = "Query is required"
                });
            }

            try
            {
                query = query.Trim();

                var records = await _context.PVTMasters
                    .Where(x =>
                        x.IsActive &&
                        (x.Code.Contains(query) || x.Name.Contains(query)))
                    .Select(x => new
                    {
                        x.Id,
                        x.Code,
                        x.Name,
                        x.StateId,
                        x.IsSpecialityProduct,
                        x.CreatedAt,
                        x.UpdatedAt
                    })
                    .OrderBy(x => x.Name)
                    .Take(20)
                    .ToListAsync();

                return Ok(records);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Search failed: {ex.Message}"
                });
            }
        }

        // ============================================================
        // SAVE SINGLE PVT MASTER
        // ============================================================

        [HttpPost("save")]
        public async Task<IActionResult> SavePVTMaster([FromBody] PVTMasterSaveDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "Invalid request"
                });
            }

            if (string.IsNullOrWhiteSpace(dto.Code) ||
                string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new
                {
                    message = "Code and Name are required"
                });
            }

            try
            {
                var code = dto.Code.Trim();
                var name = dto.Name.Trim();

                var existingRecord = await _context.PVTMasters
                    .FirstOrDefaultAsync(x => x.Code == code);

                if (existingRecord != null)
                {
                    return BadRequest(new
                    {
                        message = $"PVT Code '{code}' already exists."
                    });
                }

                var now = DateTime.Now;
                var userName = User?.Identity?.Name ?? "System";

                var pvtMaster = new PVTMaster
                {
                    Code = code,
                    Name = name,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    CreatedBy = userName,
                    UpdatedBy = userName
                };

                _context.PVTMasters.Add(pvtMaster);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "PVT Master saved successfully.",
                    id = pvtMaster.Id
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Save failed: {ex.Message}"
                });
            }
        }

        // ============================================================
        // MAP STATE TO A PVT MASTER RECORD
        // Only the State mapping is editable. The SAP Code is never changed.
        // ============================================================

        [HttpPost("map-state")]
        public async Task<IActionResult> MapState([FromBody] PVTMapStateDto dto)
        {
            if (dto == null || dto.Id <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid request"
                });
            }

            try
            {
                var record = await _context.PVTMasters
                    .FirstOrDefaultAsync(x => x.Id == dto.Id && x.IsActive);

                if (record == null)
                {
                    return NotFound(new
                    {
                        message = "SAP Code record not found."
                    });
                }

                if (dto.StateId.HasValue &&
                    dto.StateId.Value > 0 &&
                    !(await _context.States.AnyAsync(s => s.Id == dto.StateId.Value)))
                {
                    return BadRequest(new
                    {
                        message = "Selected State does not exist."
                    });
                }

                var now = DateTime.Now;
                var userName = User?.Identity?.Name ?? "System";

                record.StateId = dto.StateId.HasValue && dto.StateId.Value > 0
                    ? dto.StateId.Value
                    : (int?)null;
                record.UpdatedAt = now;
                record.UpdatedBy = userName;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "State mapping saved successfully.",
                    id = record.Id,
                    stateId = record.StateId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = $"Save failed: {ex.Message}"
                });
            }
        }

        // ============================================================
        // BULK EXCEL UPLOAD
        // ============================================================

        [HttpPost("bulk-upload")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            // --------------------------------------------------------
            // Validate file
            // --------------------------------------------------------

            if (file == null)
            {
                return BadRequest(new
                {
                    message = "No Excel file received."
                });
            }

            if (file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Uploaded Excel file has 0 bytes."
                });
            }

            var extension = Path.GetExtension(file.FileName)
                .ToLowerInvariant();

            if (extension != ".xlsx")
            {
                return BadRequest(new
                {
                    message = "Only .xlsx Excel files are supported."
                });
            }

            try
            {
                // ----------------------------------------------------
                // Copy upload stream into memory first
                // ----------------------------------------------------

                await using var uploadedStream = file.OpenReadStream();
                using var memoryStream = new MemoryStream();

                await uploadedStream.CopyToAsync(memoryStream);

                if (memoryStream.Length == 0)
                {
                    return BadRequest(new
                    {
                        message = "Uploaded Excel stream is empty."
                    });
                }

                memoryStream.Position = 0;

                using var workbook = new XLWorkbook(memoryStream);

                if (!workbook.Worksheets.Any())
                {
                    return BadRequest(new
                    {
                        message = "Excel workbook does not contain any worksheets."
                    });
                }

                // ----------------------------------------------------
                // Search all worksheets for Code / Name headers
                // ----------------------------------------------------

                IXLWorksheet? worksheet = null;

                int headerRowNumber = 0;
                int codeColumn = 0;
                int nameColumn = 0;
                int stateColumn = 0; // Optional "State" header -> state name lookup
                int specialityColumn = 0; // Optional "IsSpecialityProduct" header -> boolean lookup

                foreach (var currentSheet in workbook.Worksheets)
                {
                    var lastRowUsed = currentSheet.LastRowUsed();

                    if (lastRowUsed == null)
                    {
                        continue;
                    }

                    var rowsToCheck = Math.Min(
                        lastRowUsed.RowNumber(),
                        20);

                    for (int rowNumber = 1;
                         rowNumber <= rowsToCheck;
                         rowNumber++)
                    {
                        var row = currentSheet.Row(rowNumber);

                        int foundCodeColumn = 0;
                        int foundNameColumn = 0;
                        int foundStateColumn = 0;
                        int foundSpecialityColumn = 0;

                        var lastColumnUsed =
                            row.LastCellUsed()?.Address.ColumnNumber ?? 0;

                        if (lastColumnUsed == 0)
                        {
                            continue;
                        }

                        for (int columnNumber = 1;
                             columnNumber <= lastColumnUsed;
                             columnNumber++)
                        {
                            var header =
                                row.Cell(columnNumber)
                                    .GetFormattedString()
                                    .Trim();

                            if (header.Equals(
                                "Code",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                foundCodeColumn = columnNumber;
                            }

                            if (header.Equals(
                                "Name",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                foundNameColumn = columnNumber;
                            }

                            if (header.Equals(
                                "State",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                foundStateColumn = columnNumber;
                            }

                            if (IsSpecialityHeader(header))
                            {
                                foundSpecialityColumn = columnNumber;
                            }
                        }

                        if (foundCodeColumn > 0 &&
                            foundNameColumn > 0)
                        {
                            worksheet = currentSheet;
                            headerRowNumber = rowNumber;
                            codeColumn = foundCodeColumn;
                            nameColumn = foundNameColumn;
                            // State is optional; 0 means the workbook has no State column.
                            stateColumn = foundStateColumn;
                            // IsSpecialityProduct is optional; 0 means the workbook has no column.
                            specialityColumn = foundSpecialityColumn;

                            break;
                        }
                    }

                    if (worksheet != null)
                    {
                        break;
                    }
                }

                // ----------------------------------------------------
                // Required headers not found
                // ----------------------------------------------------

                if (worksheet == null)
                {
                    var sheetNames = workbook.Worksheets
                        .Select(x => x.Name)
                        .ToList();

                    return BadRequest(new
                    {
                        message =
                            "Could not find the required Excel columns 'Code' and 'Name'. " +
                            "Please make sure your Excel contains these headers.",
                        sheets = sheetNames
                    });
                }

                // ----------------------------------------------------
                // Check for data rows
                // ----------------------------------------------------

                var lastUsedRow = worksheet.LastRowUsed();

                if (lastUsedRow == null ||
                    lastUsedRow.RowNumber() <= headerRowNumber)
                {
                    return BadRequest(new
                    {
                        message =
                            $"Excel sheet '{worksheet.Name}' contains headers but no data rows."
                    });
                }

                // ----------------------------------------------------
                // Load existing database records
                // ----------------------------------------------------

                var existingRecords = await _context.PVTMasters
                    .ToListAsync();

                var existingByCode = existingRecords
                    .Where(x => !string.IsNullOrWhiteSpace(x.Code))
                    .GroupBy(
                        x => x.Code.Trim(),
                        StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        x => x.Key,
                        x => x.First(),
                        StringComparer.OrdinalIgnoreCase);

                var uploadedCodes =
                    new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);

                // Pre-load the State master so the optional "State" column can be
                // resolved to a StateId. An unknown State name only skips that row;
                // it never fails a valid upload.
                var stateIdByName =
                    new Dictionary<string, int>(
                        StringComparer.OrdinalIgnoreCase);

                foreach (var state in _context.States)
                {
                    if (!string.IsNullOrWhiteSpace(state.StateName))
                    {
                        stateIdByName[state.StateName.Trim()] = state.Id;
                    }
                }

                int totalRows = 0;
                int insertedCount = 0;
                int updatedCount = 0;
                int skippedCount = 0;

                var errors = new List<string>();
                var duplicateRecords = new List<object>();

                var now = DateTime.Now;
                var userName = User?.Identity?.Name ?? "System";

                // ----------------------------------------------------
                // Process Excel rows
                // ----------------------------------------------------

                for (int rowNumber = headerRowNumber + 1;
                     rowNumber <= lastUsedRow.RowNumber();
                     rowNumber++)
                {
                    var row = worksheet.Row(rowNumber);

                    var code = row.Cell(codeColumn)
                        .GetFormattedString()
                        .Trim();

                    var name = row.Cell(nameColumn)
                        .GetFormattedString()
                        .Trim();

                    // Optional State column (blank when the workbook has no State header).
                    var stateName = stateColumn > 0
                        ? row.Cell(stateColumn)
                            .GetFormattedString()
                            .Trim()
                        : string.Empty;

                    // Optional IsSpecialityProduct column (blank when the workbook has no header).
                    var specialityValue = specialityColumn > 0
                        ? row.Cell(specialityColumn)
                            .GetFormattedString()
                            .Trim()
                        : string.Empty;

                    var isSpecialityProduct =
                        ParseSpecialityProduct(specialityValue);

                    // Ignore completely empty rows
                    if (string.IsNullOrWhiteSpace(code) &&
                        string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    // Code validation
                    if (string.IsNullOrWhiteSpace(code))
                    {
                        errors.Add($"Row {rowNumber}: Code is empty.");
                        continue;
                    }

                    // Name validation
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"Row {rowNumber}: Name is empty.");
                        continue;
                    }

                    totalRows++;

                    // Duplicate code inside same uploaded Excel
                    if (!uploadedCodes.Add(code))
                    {
                        skippedCount++;
                        duplicateRecords.Add(new
                        {
                            rowNumber = rowNumber,
                            sapCode = code,
                            warehouseName = name,
                            reason = "Duplicate SAP Code in uploaded file"
                        });
                        errors.Add(
                            $"Row {rowNumber}: Duplicate Code '{code}' found in Excel.");
                        continue;
                    }

                    // ------------------------------------------------
                    // Resolve the optional State column
                    // ------------------------------------------------

// Empty State -> StateId is left unchanged (or stays null for inserts).
				// Unknown State name -> skip the row and report the missing State.
				// State "SP" is a special marker (Speciality Product): it is not a State Master
				// entry, so it is never looked up and never maps a StateId.
				int? resolvedStateId = null;

				bool isSpecialState = stateName.Equals(
					"SP",
					StringComparison.OrdinalIgnoreCase);

				if (!string.IsNullOrWhiteSpace(stateName) &&
					!isSpecialState)
				{
					if (!stateIdByName.TryGetValue(
						stateName,
						out var stateId))
					{
                            skippedCount++;
                            duplicateRecords.Add(new
                            {
                                rowNumber = rowNumber,
                                sapCode = code,
                                warehouseName = name,
                                reason = $"Invalid State: '{stateName}' not found in State Master"
                            });
                            errors.Add(
                                $"Row {rowNumber}: Invalid State: '{stateName}' not found in State Master.");
                            continue;
                        }

                        resolvedStateId = stateId;
                    }

// State "SP" forces IsSpecialityProduct to true; this rule takes priority
				// over the optional Excel IsSpecialityProduct column.
				bool isSpecialityByState = isSpecialState;

                    bool? effectiveSpecialityProduct = isSpecialityByState
                        ? true
                        : isSpecialityProduct;

                    // ------------------------------------------------
                    // Existing SAP Code -> update StateId only
                    // ------------------------------------------------

                    if (existingByCode.TryGetValue(
                        code,
                        out var existingRecord))
                    {
                        // The existing Code and Name are never changed; only the StateId
                        // is mapped when the Excel provides a valid State name.
                        if (resolvedStateId.HasValue &&
                            existingRecord.StateId != resolvedStateId.Value)
                        {
                            existingRecord.StateId = resolvedStateId.Value;
                            existingRecord.UpdatedAt = now;
                            existingRecord.UpdatedBy = userName;
                        }

                        // IsSpecialityProduct is updated when the Excel column is present, or when the
                        // State "SP" rule applies. The State rule takes priority over the Excel value.
                        if ((specialityColumn > 0 || isSpecialityByState) &&
                            existingRecord.IsSpecialityProduct != effectiveSpecialityProduct)
                        {
                            existingRecord.IsSpecialityProduct = effectiveSpecialityProduct;
                            existingRecord.UpdatedAt = now;
                            existingRecord.UpdatedBy = userName;
                        }

                        updatedCount++;
                    }
                    else
                    {
                        // ------------------------------------------------
                        // Insert new record
                        // ------------------------------------------------

                        var newRecord = new PVTMaster
                        {
                            Code = code,
                            Name = name,
                            StateId = resolvedStateId,
                            IsSpecialityProduct = effectiveSpecialityProduct,
                            IsActive = true,
                            CreatedAt = now,
                            UpdatedAt = now,
                            CreatedBy = userName,
                            UpdatedBy = userName
                        };

                        _context.PVTMasters.Add(newRecord);

                        existingByCode[code] = newRecord;

                        insertedCount++;
                    }
                }

                // ----------------------------------------------------
                // No usable data
                // ----------------------------------------------------

                if (totalRows == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            $"No data was found below the Code and Name headers in sheet '{worksheet.Name}'."
                    });
                }

                if (insertedCount == 0 &&
                    updatedCount == 0)
                {
                    return Ok(new
                    {
                        message =
                            "Excel contains records, but no valid records could be processed.",
                        totalRows,
                        inserted = insertedCount,
                        updated = updatedCount,
                        skipped = skippedCount,
                        errors,
                        duplicateRecords
                    });
                }

                // ----------------------------------------------------
                // Save database
                // ----------------------------------------------------

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "PVT Master Excel uploaded successfully.",
                    fileName = file.FileName,
                    sheetName = worksheet.Name,
                    totalRows,
                    inserted = insertedCount,
                    updated = updatedCount,
                    skipped = skippedCount,
                    errors,
                    duplicateRecords
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        $"Excel upload failed. Please make sure the file is a valid .xlsx workbook. Details: {ex.Message}"
                });
            }
        }

        // ============================================================
        // DOWNLOAD SAMPLE EXCEL TEMPLATE
        // ============================================================

        [HttpGet("template")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                using var workbook = new XLWorkbook();

                var worksheet = workbook.Worksheets.Add("PVTMaster");

                // Headers
                worksheet.Cell(1, 1).Value = "Code";
                worksheet.Cell(1, 2).Value = "Name";
                worksheet.Cell(1, 3).Value = "State";
                worksheet.Cell(1, 4).Value = "IsSpecialityProduct";

                // Sample data
                worksheet.Cell(2, 1).Value = "1001";
                worksheet.Cell(2, 2).Value = "PVT GODOWN ARIYALUR";
                worksheet.Cell(2, 3).Value = "Tamil Nadu";
                worksheet.Cell(2, 4).Value = "Yes";

                worksheet.Cell(3, 1).Value = "1002";
                worksheet.Cell(3, 2).Value = "PVT GODOWN PALAYAVOYAL";
                worksheet.Cell(3, 3).Value = "Karnataka";
                worksheet.Cell(3, 4).Value = "No";

                // Header style
                var headerRange = worksheet.Range("A1:D1");

                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                // Keep Code as text
                worksheet.Column(1).Style.NumberFormat.Format = "@";

                // Column widths
                worksheet.Column(1).Width = 15;
                worksheet.Column(2).Width = 40;
                worksheet.Column(3).Width = 25;
                worksheet.Column(4).Width = 22;

                // Borders and filter
                var usedRange = worksheet.RangeUsed();

                if (usedRange != null)
                {
                    usedRange.Style.Border.OutsideBorder =
                        XLBorderStyleValues.Thin;

                    usedRange.Style.Border.InsideBorder =
                        XLBorderStyleValues.Thin;

                    usedRange.SetAutoFilter();
                }

                using var memoryStream = new MemoryStream();

                workbook.SaveAs(memoryStream);

                var bytes = memoryStream.ToArray();

                return File(
                    bytes,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "PVTMaster_Template.xlsx");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        $"Unable to generate template: {ex.Message}"
                });
            }
        }

        private static bool IsSpecialityHeader(string header)
        {
            return header.Equals(
                    "IsSpecialityProduct",
                    StringComparison.OrdinalIgnoreCase) ||
                header.Equals(
                    "Is Speciality Product",
                    StringComparison.OrdinalIgnoreCase);
        }

        private static bool? ParseSpecialityProduct(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("True", StringComparison.OrdinalIgnoreCase) ||
                value == "1";
        }
    }

    // ================================================================
    // SAVE DTO
    // ================================================================

    public class PVTMasterSaveDto
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    // ================================================================
    // MAP STATE DTO
    // ================================================================

    public class PVTMapStateDto
    {
        public int Id { get; set; }

        public int? StateId { get; set; }
    }
}