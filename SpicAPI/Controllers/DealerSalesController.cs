using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;

namespace SpicAPI.Controllers;

[Authorize]
[ApiController]
[Route("api/dealer-sales")]
public sealed class DealerSalesController : ControllerBase
{
    private const long MaxFileBytes = 5L * 1024 * 1024;
    private readonly AppDbContext _db;

    public DealerSalesController(AppDbContext db) => _db = db;

    public sealed record ImportIssue(int RowNo, string Field, string Reason);
    private sealed record Parsed(int RowNo, SalesAuditDealerSale Value, string InvoiceKey);
    private sealed record SaleListItem(
        long Id,
        string BillingDocument,
        string Item,
        DateTime BillingDate,
        string CustomerNumber,
        string CustomerName,
        string ProductCode,
        string ProductName,
        decimal Quantity,
        string UnitOfMeasure,
        decimal GrossAmount);

    private static string Normalize(string value) => new string(
        (value ?? "").Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static string ToCellText(object? value) => value switch
    {
        null => "",
        DateTime d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        double d => d.ToString("0.################", CultureInfo.InvariantCulture),
        float f => f.ToString("0.########", CultureInfo.InvariantCulture),
        decimal m => m.ToString(CultureInfo.InvariantCulture),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? ""
    };

    private static bool TryDate(object? raw, out DateTime date)
    {
        date = default;
        if (raw is DateTime dt) { date = dt.Date; return true; }
        if (raw is double n && n >= 1 && n < 2958466)
        {
            try { date = DateTime.FromOADate(n).Date; return true; }
            catch (ArgumentException) { return false; }
        }
        return DateTime.TryParseExact(ToCellText(raw),
            new[] { "yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy", "d/M/yyyy", "d-M-yyyy", "MM/dd/yyyy", "M/d/yyyy" },
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static bool TryAmount(object? raw, out decimal amount)
    {
        amount = default;
        if (raw is decimal d) { amount = d; return true; }
        if (raw is double n && !double.IsNaN(n) && !double.IsInfinity(n))
        {
            try { amount = Convert.ToDecimal(n); return true; }
            catch (OverflowException) { return false; }
        }

        var text = raw is JsonElement e ? e.ToString() : ToCellText(raw);
        return decimal.TryParse((text ?? "").Replace(",", "").Replace("₹", ""),
            NumberStyles.Number | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out amount);
    }

    private static string Get(JsonElement obj, string name)
    {
        if (obj.ValueKind != JsonValueKind.Object) return "";
        if (obj.TryGetProperty(name, out var direct)) return direct.ToString();
        foreach (var property in obj.EnumerateObject())
            if (Normalize(property.Name) == Normalize(name)) return property.Value.ToString();
        return "";
    }

    private static string Key(string invoice, string item, string product) =>
        invoice.Trim().ToUpperInvariant() + "\u001f" +
        item.Trim().ToUpperInvariant() + "\u001f" +
        product.Trim().ToUpperInvariant();

    private static DbParameter Param(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
        return parameter;
    }

    private static string EscapeLikePattern(string input) =>
        input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    private const string FilterSql = @"(@q = '' OR
              COALESCE(""SourceRowJson""->>'Billing Document','') ILIKE @pattern OR
              COALESCE(""SourceRowJson""->>'Customer Number','') ILIKE @pattern OR
              COALESCE(""SourceRowJson""->>'Customer Name','') ILIKE @pattern OR
              COALESCE(""SourceRowJson""->>'Product Code','') ILIKE @pattern OR
              COALESCE(""SourceRowJson""->>'Product Name','') ILIKE @pattern)";

    private const string SafeGrossSql = @"CASE WHEN
              REPLACE(REPLACE(COALESCE(""SourceRowJson""->>'Gross Amount',''), ',', ''), '₹', '')
              ~ '^[0-9]+(\.[0-9]+)?$'
              THEN REPLACE(REPLACE(""SourceRowJson""->>'Gross Amount', ',', ''), '₹', '')::numeric
              ELSE 0 END";

    private async Task<(long Count, decimal Gross)> GetSummaryAsync(string term, CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync(ct);
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*), COALESCE(SUM(" + SafeGrossSql + "),0) FROM \"SalesAuditDealerSales\" WHERE " + FilterSql;
            Param(cmd, "@q", term);
            Param(cmd, "@pattern", "%" + EscapeLikePattern(term) + "%");
            using var reader = await cmd.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);
            var count = reader.GetInt64(0);
            var gross = Convert.ToDecimal(reader.GetValue(1), CultureInfo.InvariantCulture);
            return (count, gross);
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }

    private async Task<List<SaleListItem>> GetRowsAsync(string term, int? take, int? skip, CancellationToken ct)
    {
        var rows = new List<SaleListItem>();
        var connection = _db.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync(ct);
        try
        {
            using var cmd = connection.CreateCommand();
            var paging = take.HasValue && skip.HasValue ? " LIMIT @take OFFSET @skip" : "";
            cmd.CommandText = "SELECT \"Id\", \"SourceRowJson\"::text FROM \"SalesAuditDealerSales\" WHERE " + FilterSql +
                              " ORDER BY \"ImportedAtUtc\" DESC, \"Id\" DESC" + paging;
            Param(cmd, "@q", term);
            Param(cmd, "@pattern", "%" + EscapeLikePattern(term) + "%");
            if (take.HasValue) Param(cmd, "@take", take.Value);
            if (skip.HasValue) Param(cmd, "@skip", skip.Value);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                using var json = JsonDocument.Parse(reader.GetString(1));
                var obj = json.RootElement;
                TryDate(Get(obj, "Billing Date"), out var billingDate);
                TryAmount(Get(obj, "Quantity"), out var quantity);
                TryAmount(Get(obj, "Gross Amount"), out var amount);
                rows.Add(new SaleListItem(
                    reader.GetInt64(0),
                    Get(obj, "Billing Document"),
                    Get(obj, "Item"),
                    billingDate,
                    Get(obj, "Customer Number"),
                    Get(obj, "Customer Name"),
                    Get(obj, "Product Code"),
                    Get(obj, "Product Name"),
                    quantity,
                    Get(obj, "Unit of Measure"),
                    amount));
            }

            return rows;
        }
        finally
        {
            if (openedHere) await connection.CloseAsync();
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1,
        [FromQuery] int pageSize = 16, [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = (search ?? "").Trim();
        if (term.Length > 100)
            return BadRequest(new { message = "Search limited to 100 characters." });

        var summary = await GetSummaryAsync(term, ct);
        var pages = Math.Max(1, (int)Math.Ceiling(summary.Count / (double)pageSize));
        page = Math.Min(page, pages);
        var rows = await GetRowsAsync(term, pageSize, (page - 1) * pageSize, ct);

        return Ok(new
        {
            TotalRows = summary.Count,
            TotalGrossAmountRs = summary.Gross,
            Page = page,
            PageSize = pageSize,
            Rows = rows
        });
    }

    [HttpGet("export/csv")]
    public async Task<IActionResult> ExportCsv([FromQuery] string? search = null, CancellationToken ct = default)
    {
        var term = (search ?? "").Trim();
        if (term.Length > 100)
            return BadRequest(new { message = "Search limited to 100 characters." });

        var rows = await GetRowsAsync(term, null, null, ct);
        var sb = new StringBuilder();
        sb.AppendLine("Billing Date,Billing Document,Item,Dealer Code,Dealer Name,Product Code,Product Name,Quantity,Unit,Gross Amount");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.BillingDate == default ? "" : row.BillingDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
                Csv(row.BillingDocument),
                Csv(row.Item),
                Csv(row.CustomerNumber),
                Csv(row.CustomerName),
                Csv(row.ProductCode),
                Csv(row.ProductName),
                Csv(row.Quantity.ToString("0.###", CultureInfo.InvariantCulture)),
                Csv(row.UnitOfMeasure),
                Csv(row.GrossAmount.ToString("0.00", CultureInfo.InvariantCulture))
            }));
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", $"DealerSales_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportExcel([FromQuery] string? search = null, CancellationToken ct = default)
    {
        var term = (search ?? "").Trim();
        if (term.Length > 100)
            return BadRequest(new { message = "Search limited to 100 characters." });

        var rows = await GetRowsAsync(term, null, null, ct);
        var html = new StringBuilder();
        html.Append("<html><head><meta charset='utf-8' /></head><body>");
        html.Append("<table border='1'><thead><tr>");
        foreach (var h in new[] { "Billing Date", "Billing Document", "Item", "Dealer Code", "Dealer Name", "Product Code", "Product Name", "Quantity", "Unit", "Gross Amount" })
            html.Append("<th>").Append(System.Net.WebUtility.HtmlEncode(h)).Append("</th>");
        html.Append("</tr></thead><tbody>");
        foreach (var row in rows)
        {
            html.Append("<tr>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.BillingDate == default ? "" : row.BillingDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture))).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.BillingDocument)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.Item)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.CustomerNumber)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.CustomerName)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.ProductCode)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.ProductName)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.Quantity.ToString("0.###", CultureInfo.InvariantCulture))).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.UnitOfMeasure)).Append("</td>")
                .Append("<td>").Append(System.Net.WebUtility.HtmlEncode(row.GrossAmount.ToString("0.00", CultureInfo.InvariantCulture))).Append("</td>")
                .Append("</tr>");
        }
        html.Append("</tbody></table></body></html>");
        return File(Encoding.UTF8.GetBytes(html.ToString()), "application/vnd.ms-excel", $"DealerSales_{DateTime.Now:yyyyMMdd_HHmmss}.xls");
    }

    [HttpPost("upload")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] IFormFile? file, CancellationToken ct = default)
    {
        if (file is null || file.Length == 0 || file.Length > MaxFileBytes)
            return BadRequest(new { message = "Upload a nonempty Excel file up to 5 MB." });
        if (Path.GetExtension(file.FileName).ToLowerInvariant() is not (".xlsx" or ".xls"))
            return BadRequest(new { message = "Only .xlsx and .xls are supported." });

        var required = new[]
        {
            "billingdocument", "billingdate", "item", "customernumber", "customername",
            "productcode", "productname", "quantity", "unitofmeasure", "grossamount"
        };

        var issues = new List<ImportIssue>();
        var skippedInFile = 0;
        var parsed = new List<Parsed>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var batchId = Guid.NewGuid();
        var uploadName = Path.GetFileName(file.FileName);
        var importedBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "";

        if (uploadName.Length > 250)
            return BadRequest(new { message = "File name too long." });
        if (importedBy.Length > 100)
            return BadRequest(new { message = "User identifier too long." });

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using (var stream = file.OpenReadStream())
        {
            IExcelDataReader reader;
            try { reader = ExcelReaderFactory.CreateReader(stream); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return BadRequest(new { message = "Invalid Excel workbook." });
            }

            using (reader)
            {
                if (!reader.Read())
                    return BadRequest(new { message = "Excel header row missing." });

                var headers = Enumerable.Range(0, reader.FieldCount)
                    .Select(i => (Name: ToCellText(reader.GetValue(i)), Key: Normalize(ToCellText(reader.GetValue(i))), Index: i))
                    .Where(x => x.Key.Length > 0)
                    .ToArray();

                if (headers.GroupBy(x => x.Key).Any(g => g.Count() > 1))
                    return BadRequest(new { message = "Duplicate Excel headers." });

                var positions = headers.ToDictionary(x => x.Key, x => x.Index);
                var missing = required.Where(x => !positions.ContainsKey(x)).ToArray();
                if (missing.Length > 0)
                    return BadRequest(new { message = "Missing columns: " + string.Join(", ", missing) });

                object? Cell(string name) => positions.TryGetValue(Normalize(name), out var index) ? reader.GetValue(index) : null;
                string Value(string name) => ToCellText(Cell(name));

                int rowNo = 1;
                while (reader.Read())
                {
                    rowNo++;
                    if (rowNo > 10001)
                        return BadRequest(new { message = "Maximum 10,000 invoice rows allowed." });

                    if (Enumerable.Range(0, reader.FieldCount).All(i => string.IsNullOrWhiteSpace(ToCellText(reader.GetValue(i)))))
                        continue;

                    var invoice = Value("Billing Document");
                    var item = Value("Item");
                    var customer = Value("Customer Number");
                    var product = Value("Product Code");

                    if (string.IsNullOrWhiteSpace(invoice) ||
                        string.IsNullOrWhiteSpace(item) ||
                        string.IsNullOrWhiteSpace(customer) ||
                        string.IsNullOrWhiteSpace(product) ||
                        string.IsNullOrWhiteSpace(Value("Customer Name")) ||
                        string.IsNullOrWhiteSpace(Value("Product Name")))
                    {
                        issues.Add(new ImportIssue(rowNo, "Identification", "Invoice, item, customer and product fields required."));
                        continue;
                    }

                    if (!TryDate(Cell("Billing Date"), out _))
                    {
                        issues.Add(new ImportIssue(rowNo, "Billing Date", "Invalid billing date."));
                        continue;
                    }

                    if (!TryAmount(Cell("Quantity"), out var quantity) || quantity < 0 ||
                        !TryAmount(Cell("Gross Amount"), out var gross) || gross < 0)
                    {
                        issues.Add(new ImportIssue(rowNo, "Quantity/Gross Amount", "Invalid or negative value."));
                        continue;
                    }

                    var key = Key(invoice, item, product);
                    // Weekly SAP exports often repeat earlier invoice lines.
                    // Keep the first occurrence within the uploaded workbook.
                    if (!seen.Add(key))
                    {
                        skippedInFile++;
                        continue;
                    }

                    var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var header in headers)
                        raw[header.Name] = ToCellText(reader.GetValue(header.Index));

                    parsed.Add(new Parsed(rowNo, new SalesAuditDealerSale
                    {
                        ImportBatchId = batchId,
                        UploadFileName = uploadName,
                        ImportedAtUtc = DateTime.UtcNow,
                        ImportedBy = importedBy,
                        SourceRowJson = JsonSerializer.Serialize(raw)
                    }, key));
                }
            }
        }

        if (issues.Count > 0)
            return BadRequest(new
            {
                message = "Excel validation failed; no rows saved.",
                invalidRows = issues.Count,
                validationErrors = issues.Take(100).ToArray()
            });

        if (parsed.Count == 0)
        {
            if (skippedInFile > 0)
                return Ok(new
                {
                    message = "Nothing new to import. Repeated rows skipped.",
                    importedRows = 0,
                    skippedExistingRows = 0,
                    skippedInFileRows = skippedInFile,
                    skippedRows = skippedInFile,
                    importBatchId = (Guid?)null
                });
            return BadRequest(new { message = "No invoice rows found." });
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            var invoiceKeys = parsed.ToArray();
            var invoices = parsed.Select(x =>
            {
                using var doc = JsonDocument.Parse(x.Value.SourceRowJson);
                return Get(doc.RootElement, "Billing Document");
            }).Distinct().ToArray();

            var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var connection = _db.Database.GetDbConnection();
            var openedHere = connection.State != ConnectionState.Open;
            if (openedHere) await connection.OpenAsync(ct);
            try
            {
                using var lookup = connection.CreateCommand();
                lookup.Transaction = _db.Database.CurrentTransaction!.GetDbTransaction();
                lookup.CommandText = @"SELECT
                        ""SourceRowJson""->>'Billing Document',
                        ""SourceRowJson""->>'Item',
                        ""SourceRowJson""->>'Product Code'
                    FROM ""SalesAuditDealerSales""
                    WHERE UPPER(BTRIM(""SourceRowJson""->>'Billing Document')) = ANY(@invoices)";
                Param(lookup, "@invoices", invoices.Select(x => x.Trim().ToUpperInvariant()).ToArray());
                using var reader = await lookup.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    existingKeys.Add(Key(
                        reader.IsDBNull(0) ? "" : reader.GetString(0),
                        reader.IsDBNull(1) ? "" : reader.GetString(1),
                        reader.IsDBNull(2) ? "" : reader.GetString(2)));
                }
            }
            finally
            {
                if (openedHere) await connection.CloseAsync();
            }

            // Only insert invoice keys that are not already in the database.
            // Existing SAP invoice data is left untouched (no delete/update).
            var newRows = invoiceKeys
                .Where(x => !existingKeys.Contains(x.InvoiceKey))
                .ToArray();
            var skippedExisting = invoiceKeys.Length - newRows.Length;

            if (newRows.Length > 0)
            {
                _db.Set<SalesAuditDealerSale>().AddRange(newRows.Select(x => x.Value));
                await _db.SaveChangesAsync(ct);
            }
            await transaction.CommitAsync(ct);

            var totalGross = newRows.Sum(x =>
            {
                using var doc = JsonDocument.Parse(x.Value.SourceRowJson);
                TryAmount(Get(doc.RootElement, "Gross Amount"), out var amount);
                return amount;
            });
            var skippedTotal = skippedExisting + skippedInFile;

            return Ok(new
            {
                message = newRows.Length == 0
                    ? "No new invoice lines found. Existing rows were skipped."
                    : $"Imported {newRows.Length} new invoice lines; skipped {skippedTotal} duplicate rows.",
                importedRows = newRows.Length,
                skippedExistingRows = skippedExisting,
                skippedInFileRows = skippedInFile,
                skippedRows = skippedTotal,
                totalGrossAmountRs = totalGross,
                importBatchId = newRows.Length > 0 ? (Guid?)batchId : null
            });
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new { message = "Database constraint or duplicate prevented upload." });
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState is "23505" or "40001")
        {
            await transaction.RollbackAsync(ct);
            return Conflict(new { message = "Duplicate invoice or concurrent upload conflict. Refresh and retry." });
        }
    }

    private static string Csv(string value)
    {
        value ??= string.Empty;
        return '"' + value.Replace("\"", "\"\"") + '"';
    }
}
