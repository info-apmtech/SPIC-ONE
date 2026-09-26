using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;

namespace Spic.Infrastructure.Services.LabReports;

/// <summary>
/// Creates the LabReport rows of a batch (called by LabController when the coordinator completes
/// the batch). One report per sample of the batch's consignments, two for a SoilAndWater sample
/// (Soil and Water layouts). Codes RPT-SAS-{yyyy}-{001} (calendar year, numbered from the highest
/// code of the year); FinancialYearStart = April to March; the batch gets ReportCode
/// REP-SAS-{yyyy}-{001} and ReportGeneratedAt; a LabActivity "Report Generated" row is written.
///
/// Idempotent: samples that already have their report are skipped, and nothing is written when
/// every report and the batch code exist. Several API instances share the database, so a unique
/// violation (code, batch code or the (SampleItemId, SampleType) index) re-reads and retries.
/// The service shares the caller's scoped DbContext: SaveChanges also saves the caller's pending
/// changes (e.g. the batch status), and a retry only rolls back this service's own changes.
/// </summary>
public sealed class LabReportService : ILabReportService
{
    private const int MaxAttempts = 6;

    private readonly AppDbContext _db;
    private readonly ILogger<LabReportService> _logger;

    private readonly List<object> _added = new();
    private SampleBatch? _batch;
    private (string? Code, DateTime? At, DateTime UpdatedAt) _batchBefore;

    public LabReportService(AppDbContext db, ILogger<LabReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task GenerateForBatchAsync(int batchId, string? byUserId, string? byName, CancellationToken ct = default)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await GenerateOnceAsync(batchId, byUserId, byName, ct);
                return;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex) && attempt < MaxAttempts)
            {
                _logger.LogInformation("LabReportService: code clash for batch {BatchId} (attempt {Attempt}); retrying.", batchId, attempt);
                Undo();
                await Task.Delay(Random.Shared.Next(20, 120) * attempt, ct);
            }
            catch
            {
                Undo();
                throw;
            }
        }
    }

    private void Undo()
    {
        foreach (var entity in _added) _db.Entry(entity).State = EntityState.Detached;
        _added.Clear();
        if (_batch != null)
        {
            _batch.ReportCode = _batchBefore.Code;
            _batch.ReportGeneratedAt = _batchBefore.At;
            _batch.UpdatedAt = _batchBefore.UpdatedAt;
        }
    }

    private async Task GenerateOnceAsync(int batchId, string? byUserId, string? byName, CancellationToken ct)
    {
        _added.Clear();
        _batch = null;

        var batch = await _db.SampleBatches.FirstOrDefaultAsync(b => b.Id == batchId && !b.IsDeleted, ct)
            ?? throw new InvalidOperationException($"Batch {batchId} not found.");

        // The tracked batch may be stale (tracked by the caller, or a retry after another
        // instance wrote the batch code): adopt a code already stored in the database.
        if (string.IsNullOrEmpty(batch.ReportCode))
        {
            var fresh = await _db.SampleBatches.AsNoTracking()
                .Where(b => b.Id == batchId)
                .Select(b => new { b.ReportCode, b.ReportGeneratedAt })
                .FirstAsync(ct);
            if (!string.IsNullOrEmpty(fresh.ReportCode))
            {
                batch.ReportCode = fresh.ReportCode;
                batch.ReportGeneratedAt = fresh.ReportGeneratedAt;
                _db.Entry(batch).Property(b => b.ReportCode).IsModified = false;
                _db.Entry(batch).Property(b => b.ReportGeneratedAt).IsModified = false;
            }
        }

        _batch = batch;
        _batchBefore = (batch.ReportCode, batch.ReportGeneratedAt, batch.UpdatedAt);

        var items = await _db.SampleItems.AsNoTracking()
            .Where(i => !i.IsDeleted && !i.Collection!.IsDeleted &&
                        i.Collection.ConsignmentId != null &&
                        _db.SampleConsignments.Any(c => c.Id == i.Collection.ConsignmentId && c.BatchId == batchId && !c.IsDeleted))
            .OrderBy(i => i.CollectionId).ThenBy(i => i.Id)
            .Select(i => new { i.Id, i.SampleType })
            .ToListAsync(ct);

        var itemIds = items.Select(i => i.Id).ToList();
        var existing = await _db.LabReports.AsNoTracking()
            .Where(r => itemIds.Contains(r.SampleItemId))
            .Select(r => new { r.SampleItemId, r.SampleType })
            .ToListAsync(ct);
        var have = existing.Select(e => (e.SampleItemId, e.SampleType)).ToHashSet();

        var missing = items
            .SelectMany(i => LabReportRules.ReportTypesFor(i.SampleType).Select(t => (ItemId: i.Id, Type: t)))
            .Where(x => !have.Contains((x.ItemId, x.Type)))
            .ToList();

        if (missing.Count == 0 && !string.IsNullOrEmpty(batch.ReportCode)) return;

        var now = DateTime.Now;
        var fy = LabReportRules.FinancialYearStart(now);
        var name = string.IsNullOrWhiteSpace(byName) ? null : byName.Trim();

        if (missing.Count > 0)
        {
            var head = $"RPT-SAS-{now.Year}-";
            var next = await MaxSequenceAsync(_db.LabReports.Select(r => r.Code), head, ct) + 1;
            foreach (var m in missing)
            {
                var report = new LabReport
                {
                    Code = head + (next++).ToString("000"),
                    BatchId = batchId,
                    SampleItemId = m.ItemId,
                    SampleType = m.Type,
                    Status = LabReportStatus.Generated,
                    GeneratedAt = now,
                    GeneratedByName = name,
                    FinancialYearStart = fy
                };
                _db.LabReports.Add(report);
                _added.Add(report);
            }
        }

        if (string.IsNullOrEmpty(batch.ReportCode))
        {
            var head = $"REP-SAS-{now.Year}-";
            var next = await MaxSequenceAsync(_db.SampleBatches.Where(b => b.ReportCode != null).Select(b => b.ReportCode!), head, ct) + 1;
            batch.ReportCode = head + next.ToString("000");
        }
        if (missing.Count > 0 || batch.ReportGeneratedAt == null) batch.ReportGeneratedAt = now;
        batch.UpdatedAt = now;

        if (missing.Count > 0)
        {
            var sampleCount = missing.Select(m => m.ItemId).Distinct().Count();
            var activity = new LabActivity
            {
                BatchId = batchId,
                Kind = LabActivityKind.ReportGenerated,
                Title = "Report Generated",
                Description = $"Reports generated for {sampleCount} sample{(sampleCount == 1 ? "" : "s")}" +
                              (missing.Count != sampleCount ? $" ({missing.Count} reports)" : "") +
                              $"; batch report {batch.ReportCode}",
                ByUserId = byUserId,
                ByName = name,
                ByRole = await RoleNameAsync(byUserId, ct),
                At = now
            };
            _db.LabActivities.Add(activity);
            _added.Add(activity);
        }

        await _db.SaveChangesAsync(ct);
        _added.Clear();
        _logger.LogInformation("LabReportService: batch {BatchId} -> {Count} report(s) created, batch code {Code}.",
            batchId, missing.Count, batch.ReportCode);
    }

    private async Task<string?> RoleNameAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;
        var designationId = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.DesignationId)
            .FirstOrDefaultAsync(ct);
        if (designationId is not > 0) return null;
        return await _db.Designations.AsNoTracking()
            .Where(d => d.Id == designationId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(ct);
    }

    private static async Task<int> MaxSequenceAsync(IQueryable<string> codes, string head, CancellationToken ct)
    {
        var list = await codes.Where(c => c.StartsWith(head)).ToListAsync(ct);
        var max = 0;
        foreach (var code in list)
        {
            var tail = code.Length > head.Length ? code[head.Length..] : "";
            if (int.TryParse(tail, out var value) && value > max) max = value;
        }
        return max;
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
