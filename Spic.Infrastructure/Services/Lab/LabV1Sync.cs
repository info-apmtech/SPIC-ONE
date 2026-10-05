using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;

namespace Spic.Infrastructure.Services.Lab;

/// <summary>
/// Keeps the version 1 records (collections, consignments, SasStatusEvent timeline) in step with
/// the lab workflow, using the same status names and notes the v1 SasController writes, and
/// builds the v1 ConsignmentDetailDto for the lab consignment view (same shape as
/// GET api/Sas/consignments/{id}). Only adds / changes tracked rows; the caller saves.
/// </summary>
public static class LabV1Sync
{
    public static void AddEvent(AppDbContext db, int? collectionId, int? consignmentId, string status, string? note, string? byName, DateTime at)
    {
        db.SasStatusEvents.Add(new SasStatusEvent
        {
            CollectionId = collectionId,
            ConsignmentId = consignmentId,
            Status = status,
            Note = note,
            ByName = byName,
            At = at
        });
    }

    /// <summary>The v1 PATCH consignments/{id}/status?status=Delivered logic: consignment
    /// Delivered (DeliveredAt), every collection DeliveredToLab with its event, one consignment event.</summary>
    public static void MarkDelivered(AppDbContext db, SampleConsignment consignment, IEnumerable<SampleCollection> collections,
        string byName, string? actor, DateTime now)
    {
        consignment.Status = ConsignmentStatus.Delivered;
        consignment.UpdatedAt = now;
        if (consignment.DeliveredAt == null)
            consignment.DeliveredAt = now;

        foreach (var collection in collections.Where(c => !c.IsDeleted))
        {
            collection.Status = SampleCollectionStatus.DeliveredToLab;
            collection.UpdatedBy = actor;
            collection.UpdatedAt = now;
            AddEvent(db, collection.Id, null, nameof(SampleCollectionStatus.DeliveredToLab),
                $"{consignment.Code} is {ConsignmentStatus.Delivered}.", byName, now);
        }

        AddEvent(db, null, consignment.Id, nameof(ConsignmentStatus.Delivered), null, byName, now);
    }

    /// <summary>Collections of a batch move to TestInProgress when the batch is taken for analysis.</summary>
    public static void MarkTestInProgress(AppDbContext db, string batchCode, IEnumerable<SampleCollection> collections,
        string byName, string? actor, DateTime now)
    {
        foreach (var collection in collections.Where(c => !c.IsDeleted && c.Status < SampleCollectionStatus.TestInProgress))
        {
            collection.Status = SampleCollectionStatus.TestInProgress;
            collection.UpdatedBy = actor;
            collection.UpdatedAt = now;
            AddEvent(db, collection.Id, null, nameof(SampleCollectionStatus.TestInProgress),
                $"Taken for analysis in {batchCode}.", byName, now);
        }
    }

    /// <summary>Batch completed: collections Completed (CompletedAt), consignments v1 Completed.</summary>
    public static void MarkCompleted(AppDbContext db, string batchCode, IEnumerable<SampleConsignment> consignments,
        IEnumerable<SampleCollection> collections, string byName, string? actor, DateTime now)
    {
        foreach (var collection in collections.Where(c => !c.IsDeleted))
        {
            collection.Status = SampleCollectionStatus.Completed;
            collection.UpdatedBy = actor;
            collection.UpdatedAt = now;
            if (collection.CompletedAt == null)
                collection.CompletedAt = now;
            AddEvent(db, collection.Id, null, nameof(SampleCollectionStatus.Completed),
                $"Lab analysis completed in {batchCode}; report generated.", byName, now);
        }

        foreach (var consignment in consignments.Where(c => !c.IsDeleted))
        {
            consignment.Status = ConsignmentStatus.Completed;
            consignment.UpdatedAt = now;
            AddEvent(db, null, consignment.Id, nameof(ConsignmentStatus.Completed), $"Lab analysis completed in {batchCode}.", byName, now);
        }
    }

    // ---------------------------------------------------------------- v1 consignment detail

    public static async Task<ConsignmentDetailDto?> BuildConsignmentDetailAsync(AppDbContext db, int consignmentId, bool canReview,
        CancellationToken ct = default)
    {
        var consignment = await db.SampleConsignments.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == consignmentId && !c.IsDeleted, ct);
        if (consignment == null) return null;

        var collections = await db.SampleCollections.AsNoTracking()
            .Where(c => !c.IsDeleted && c.ConsignmentId == consignmentId)
            .OrderBy(c => c.Id)
            .Include(c => c.Payments)
            .ToListAsync(ct);
        var collectionIds = collections.Select(c => c.Id).ToList();

        var items = await db.SampleItems.AsNoTracking()
            .Where(i => collectionIds.Contains(i.CollectionId) && !i.IsDeleted)
            .OrderBy(i => i.Id)
            .Include(i => i.Farmer)
            .ToListAsync(ct);
        var itemIds = items.Select(i => i.Id).ToList();

        var results = await db.SampleLabResults.AsNoTracking()
            .Where(r => itemIds.Contains(r.SampleItemId))
            .OrderBy(r => r.SortOrder).ThenBy(r => r.Id)
            .ToListAsync(ct);

        var photos = await db.ConsignmentPhotos.AsNoTracking()
            .Where(p => p.ConsignmentId == consignmentId)
            .OrderBy(p => p.Kind).ThenBy(p => p.Id)
            .ToListAsync(ct);

        var timeline = await db.SasStatusEvents.AsNoTracking()
            .Where(e => e.ConsignmentId == consignmentId)
            .OrderBy(e => e.At).ThenBy(e => e.Id)
            .Select(e => new SasStatusEventDto { Status = e.Status, Note = e.Note, ByName = e.ByName, At = e.At })
            .ToListAsync(ct);

        var paid = collections.Where(c => c.PaymentType == SamplePaymentType.Paid).ToList();
        SamplePaymentStatus? paymentStatus = null;
        DateTime? reviewedAt = null;
        if (paid.Count > 0)
        {
            var latest = paid.Select(c => c.Payments.OrderByDescending(p => p.Id).FirstOrDefault()).ToList();
            if (latest.All(p => p != null && p.Status == SamplePaymentStatus.Approved)) paymentStatus = SamplePaymentStatus.Approved;
            else if (latest.Any(p => p != null && p.Status == SamplePaymentStatus.Rejected)) paymentStatus = SamplePaymentStatus.Rejected;
            else paymentStatus = SamplePaymentStatus.Pending;
            reviewedAt = latest.Where(p => p?.ReviewedAt != null).Select(p => p!.ReviewedAt).Max();
        }

        var transactionIds = collections
            .SelectMany(c => c.Payments.Where(p => p.Status == SamplePaymentStatus.Approved))
            .Select(p => p.TransactionId)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .ToList();

        var statusByCollection = collections.ToDictionary(c => c.Id, c => c.Status);
        var lines = items
            .GroupBy(i => i.SampleType)
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
                    Status = g.Select(x => statusByCollection.TryGetValue(x.CollectionId, out var s) ? s : SampleCollectionStatus.Draft).Min()
                };
            })
            .ToList();

        return new ConsignmentDetailDto
        {
            Id = consignment.Id,
            Code = consignment.Code,
            CourierService = consignment.CourierService,
            TrackingNumber = consignment.TrackingNumber,
            TrackingLink = consignment.TrackingLink,
            DispatchedAt = consignment.DispatchedAt,
            DispatchedByName = consignment.DispatchedByName,
            Status = consignment.Status,
            CollectionCount = collections.Count,
            SampleCount = items.Count,
            TotalAmount = collections.Sum(c => c.TotalAmount),
            PaymentStatus = paymentStatus,
            PaymentReviewedAt = reviewedAt,
            TransactionIds = transactionIds.Count == 0 ? null : string.Join(", ", transactionIds),
            PackageCount = consignment.PackageCount,
            SampleLines = lines,
            ExpectedDeliveryDate = consignment.ExpectedDeliveryDate,
            PackageWeightKg = consignment.PackageWeightKg,
            PackagingType = consignment.PackagingType,
            ContactPerson = consignment.ContactPerson,
            ContactMobile = consignment.ContactMobile,
            ContactEmail = consignment.ContactEmail,
            Notes = consignment.Notes,
            DeliveredAt = consignment.DeliveredAt,
            Photos = photos.Select(p => new ConsignmentPhotoDto
            {
                Id = p.Id,
                Kind = p.Kind,
                Title = p.Title,
                FileName = p.FileName,
                Path = p.StoredPath,
                ContentType = p.ContentType,
                Size = p.Size,
                UploadedByName = p.UploadedByName,
                CreatedAt = p.CreatedAt
            }).ToList(),
            Collections = collections.Select(c =>
            {
                var own = items.Where(i => i.CollectionId == c.Id).ToList();
                var payment = c.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
                return new SampleCollectionSummaryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    CollectionDate = c.CollectionDate,
                    CollectedByName = c.CollectedByName,
                    LocationName = c.LocationName,
                    PaymentType = c.PaymentType,
                    PaidCategory = c.PaidCategory,
                    Status = c.Status,
                    TotalAmount = c.TotalAmount,
                    SampleCount = own.Count,
                    FarmerName = own.Select(i => i.Farmer?.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? "",
                    SampleTypes = string.Join(", ", own.Select(i => SampleTypeLabel(i.SampleType)).Distinct()),
                    PaymentStatus = payment?.Status,
                    PaymentReviewedAt = payment?.ReviewedAt,
                    ConsignmentId = c.ConsignmentId,
                    ConsignmentCode = consignment.Code
                };
            }).ToList(),
            Samples = items.Select(i =>
            {
                var own = results.Where(r => r.SampleItemId == i.Id).Select(MapResult).ToList();
                return new SampleItemDto
                {
                    Id = i.Id,
                    CollectionId = i.CollectionId,
                    Code = i.Code,
                    Farmer = i.Farmer == null ? new SasFarmerDto() : MapFarmer(i.Farmer),
                    SampleType = i.SampleType,
                    Crop1 = i.Crop1,
                    Crop2 = i.Crop2,
                    Remarks = i.Remarks,
                    NoOfTests = i.NoOfTests,
                    Amount = i.Amount,
                    HasResults = own.Count > 0,
                    Results = own
                };
            }).ToList(),
            Timeline = timeline,
            CanReview = canReview
        };
    }

    public static string SampleTypeLabel(SampleType type) => type switch
    {
        SampleType.Soil => "Soil",
        SampleType.Water => "Water",
        _ => "Soil & Water"
    };

    private static LabResultDto MapResult(SampleLabResult r) => new()
    {
        Id = r.Id,
        Parameter = r.Parameter,
        NormalRange = r.NormalRange,
        EnteredValue = r.EnteredValue,
        Unit = r.Unit,
        ResultLabel = r.ResultLabel,
        Status = r.Status,
        Hint = r.Hint,
        SortOrder = r.SortOrder,
        EnteredByName = r.EnteredByName,
        EnteredAt = r.EnteredAt
    };

    private static SasFarmerDto MapFarmer(SasFarmer f) => new()
    {
        Id = f.Id,
        Name = f.Name,
        Mobile = f.Mobile,
        Address1 = f.Address1,
        Address2 = f.Address2,
        Village = f.Village,
        Taluk = f.Taluk,
        DistrictId = f.DistrictId,
        DistrictName = f.DistrictName,
        StateId = f.StateId,
        StateName = f.StateName,
        PinCode = f.PinCode,
        Latitude = f.Latitude,
        Longitude = f.Longitude,
        SurveyNumber = f.SurveyNumber,
        UserId = f.UserId
    };
}
