// SPIC Sales Audit: single-file Dealer Code importer + dealer-list reader.
// Requires ClosedXML, Microsoft.EntityFrameworkCore and existing PostgreSQL EF DbContext.
// Call UploadAsync from the EXISTING SalesAuditController upload route. No duplicate route.
// Actual database identifiers are based on the supplied names and must be checked against migrations.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace SpicAPI.Services;

public static class DealerCodeUploadSingleFile
{
    private const long MaxBytes = 5L * 1024 * 1024;
    public sealed record RowError(int RowNo, string DealerCode, string Reason);
    public sealed record ImportResponse(int ImportedRows, int InvalidRows, List<RowError> ValidationErrors);
    public sealed record DealerListRow(int Id, string DealerCode, string DealerName,
        string StateName, string RegionName, string HeadquartersName,
        decimal PlannedCommitmentRs, string? MappedByName, string UploadFileName, DateTime UploadedAt);

    private sealed record CodeRow(int RowNo, string Code);
    private sealed record DealerMaster(string DealerCode, string FirmName, int StateId, int RegionId, string HQ);
    private sealed record UserScope(int StateId, int RegionId, string HQ);

    public static async Task<ImportResponse> UploadAsync(DbContext db, int csr2Id,
        IFormFile file, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (csr2Id <= 0) throw new ArgumentException("Valid CSR-2 ID is required.");
        if (user.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException("Sign in to upload dealers.");
        if (file == null || file.Length == 0 || file.Length > MaxBytes ||
            !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Upload a non-empty .xlsx file up to 5 MB.");

        // FAIL CLOSED: align these claim names with the actual authenticated employee identity.
        var scope = GetScope(user);
        var errors = new List<RowError>();
        var candidates = new List<CodeRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using (var upload = new MemoryStream())
        {
            await file.CopyToAsync(upload, ct);
            upload.Position = 0;
            XLWorkbook workbook;
            try { workbook = new XLWorkbook(upload); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { throw new ArgumentException("Invalid Excel workbook.", ex); }
            using (workbook)
            {
                var sheet = workbook.Worksheets.FirstOrDefault()
                    ?? throw new ArgumentException("Excel worksheet is missing.");
                int header = sheet.FirstRowUsed()?.RowNumber() ?? 0;
                if (header == 0 || !string.Equals(sheet.Cell(header, 1).GetString().Trim(),
                        "Dealer Code", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Excel column A must be named 'Dealer Code'.");
                if (sheet.Row(header).CellsUsed().Count() != 1)
                    throw new ArgumentException("Excel must contain only the Dealer Code column.");
                int last = sheet.LastRowUsed()?.RowNumber() ?? header;
                if (last - header > 25000) throw new ArgumentException("Maximum 25,000 dealer rows allowed.");
                for (int i = header + 1; i <= last; i++)
                {
                    string code = sheet.Cell(i, 1).GetFormattedString().Trim();
                    if (code.Length == 0) { errors.Add(new(i, "", "Dealer Code is empty.")); continue; }
                    if (code.Length > 50 || !Regex.IsMatch(code, "^[A-Za-z0-9/_-]+$"))
                    { errors.Add(new(i, code, "Invalid Dealer Code format.")); continue; }
                    if (!seen.Add(code)) { errors.Add(new(i, code, "Duplicate Dealer Code.")); continue; }
                    candidates.Add(new(i, code));
                }
            }
        }
        if (candidates.Count == 0)
            throw new ArgumentException("File has no valid dealer codes. Existing mappings were not changed.");

        // Important: Audit authorization, lookup, replacement happen on the server in ONE transaction.
        var connection = db.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync(ct);
        try
        {
            using var transaction = await connection.BeginTransactionAsync(ct);
            try
            {
                using (var audit = Command(connection, transaction,
                    "SELECT \"DocumentStatus\" FROM \"SalesAuditRecords\" WHERE \"CSR2Id\"=@csr AND \"IsDeleted\"=FALSE FOR UPDATE"))
                {
                    Param(audit, "@csr", csr2Id);
                    var status = Convert.ToString(await audit.ExecuteScalarAsync(ct));
                    if (status == null) throw new ArgumentException("Sales Audit record not found.");
                    if (string.Equals(status, "Plan Confirmed", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Sales Plan is confirmed and locked.");
                }

                var accepted = new List<DealerMaster>();
                foreach (var candidate in candidates)
                {
                    using var command = Command(connection, transaction,
                        "SELECT \"DealerCode\", \"FirmName\", \"StateId\", \"RegionId\", \"HQ\" " +
                        "FROM \"DealerRegistrations\" WHERE UPPER(TRIM(\"DealerCode\"))=UPPER(@code) LIMIT 2");
                    Param(command, "@code", candidate.Code);
                    using var reader = await command.ExecuteReaderAsync(ct);
                    DealerMaster? dealer = null;
                    int count = 0;
                    while (await reader.ReadAsync(ct))
                    {
                        count++;
                        if (count == 1)
                            dealer = new DealerMaster(reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Convert.ToInt32(reader.GetValue(2)), Convert.ToInt32(reader.GetValue(3)),
                                reader.IsDBNull(4) ? "" : Convert.ToString(reader.GetValue(4)) ?? "");
                    }
                    if (count == 0 || dealer is null)
                    { errors.Add(new(candidate.RowNo, candidate.Code, "Dealer Code not found in DealerRegistrations.")); continue; }
                    if (count > 1)
                    { errors.Add(new(candidate.RowNo, candidate.Code, "Duplicate Dealer Code in DealerRegistrations.")); continue; }
                    if (dealer.StateId != scope.StateId || dealer.RegionId != scope.RegionId ||
                        !string.Equals(dealer.HQ.Trim(), scope.HQ.Trim(), StringComparison.OrdinalIgnoreCase))
                    { errors.Add(new(candidate.RowNo, candidate.Code, "Dealer State, Region or Headquarters is outside login scope.")); continue; }
                    accepted.Add(dealer);
                }
                if (accepted.Count == 0)
                    throw new ArgumentException("No authorized dealers found. Existing mappings were not changed.");

                // Preserve edited commitments of dealer codes that remain after replacement.
                var previous = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                using (var select = Command(connection, transaction,
                    "SELECT \"DealerCode\", \"Commitment\" FROM \"SalesAuditDealerMappings\" WHERE \"CSR2Id\"=@csr"))
                {
                    Param(select, "@csr", csr2Id);
                    using var rdr = await select.ExecuteReaderAsync(ct);
                    while (await rdr.ReadAsync(ct))
                    {
                        string code = Convert.ToString(rdr.GetValue(0)) ?? "";
                        if (!previous.ContainsKey(code)) previous[code] = rdr.IsDBNull(1) ? 0m : Convert.ToDecimal(rdr.GetValue(1));
                    }
                }
                using (var delete = Command(connection, transaction,
                    "DELETE FROM \"SalesAuditDealerMappings\" WHERE \"CSR2Id\"=@csr"))
                {
                    Param(delete, "@csr", csr2Id);
                    await delete.ExecuteNonQueryAsync(ct);
                }
                foreach (var dealer in accepted)
                {
                    // Existing schema observed: CSR2Id, DealerCode, DealerName, District, HQName, Commitment.
                    // District is unknown in the DealerRegistrations fields supplied, so leave blank.
                    using var insert = Command(connection, transaction,
                        "INSERT INTO \"SalesAuditDealerMappings\" (\"CSR2Id\", \"DealerCode\", \"DealerName\", " +
                        "\"District\", \"HQName\", \"Commitment\") VALUES (@csr,@code,@name,@district,@hq,@commitment)");
                    Param(insert, "@csr", csr2Id);
                    Param(insert, "@code", dealer.DealerCode);
                    Param(insert, "@name", dealer.FirmName);
                    Param(insert, "@district", "");
                    Param(insert, "@hq", dealer.HQ);
                    Param(insert, "@commitment", previous.TryGetValue(dealer.DealerCode, out var amount) ? amount : 0m);
                    await insert.ExecuteNonQueryAsync(ct);
                }
                await transaction.CommitAsync(ct);
                return new ImportResponse(accepted.Count, errors.Count, errors);
            }
            catch { await transaction.RollbackAsync(ct); throw; }
        }
        finally { if (openedHere) await connection.CloseAsync(); }
    }

    // Can be used by existing GET action to present uploaded dealer details.
    // StateName/RegionName currently show IDs: join actual State/Region master tables to show names.
    public static async Task<List<DealerListRow>> GetRowsAsync(DbContext db, int csr2Id,
        ClaimsPrincipal user, CancellationToken ct = default)
    {
        var scope = GetScope(user);
        var connection = db.Database.GetDbConnection();
        bool openedHere = connection.State != ConnectionState.Open;
        if (openedHere) await connection.OpenAsync(ct);
        try
        {
            using var command = Command(connection, null,
                "SELECT m.\"Id\", m.\"DealerCode\", d.\"FirmName\", d.\"StateId\", " +
                "d.\"RegionId\", d.\"HQ\", m.\"Commitment\" " +
                "FROM \"SalesAuditDealerMappings\" m " +
                "JOIN \"DealerRegistrations\" d ON UPPER(TRIM(m.\"DealerCode\"))=UPPER(TRIM(d.\"DealerCode\")) " +
                "WHERE m.\"CSR2Id\"=@csr AND d.\"StateId\"=@state AND d.\"RegionId\"=@region " +
                "AND UPPER(TRIM(d.\"HQ\"))=UPPER(@hq) ORDER BY m.\"Id\"");
            Param(command, "@csr", csr2Id); Param(command, "@state", scope.StateId);
            Param(command, "@region", scope.RegionId); Param(command, "@hq", scope.HQ);
            var rows = new List<DealerListRow>();
            using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                rows.Add(new DealerListRow(Convert.ToInt32(reader.GetValue(0)), reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2),
                    Convert.ToString(reader.GetValue(3)) ?? "", Convert.ToString(reader.GetValue(4)) ?? "",
                    reader.IsDBNull(5) ? "" : Convert.ToString(reader.GetValue(5)) ?? "",
                    reader.IsDBNull(6) ? 0m : Convert.ToDecimal(reader.GetValue(6)), null, "", DateTime.MinValue));
            return rows;
        }
        finally { if (openedHere) await connection.CloseAsync(); }
    }

    private static UserScope GetScope(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException("Authentication required.");
        var state = user.FindFirst("StateId")?.Value;
        var region = user.FindFirst("RegionId")?.Value;
        var hq = user.FindFirst("HQ")?.Value;
        if (!int.TryParse(state, out var stateId) || !int.TryParse(region, out var regionId) || string.IsNullOrWhiteSpace(hq))
            throw new UnauthorizedAccessException("Employee StateId, RegionId and HQ scope must be resolved server-side before importing.");
        return new UserScope(stateId, regionId, hq);
    }

    private static DbCommand Command(DbConnection connection, DbTransaction? transaction, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }
    private static void Param(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
