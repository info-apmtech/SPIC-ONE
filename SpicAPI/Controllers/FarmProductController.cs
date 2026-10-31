using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.Entities;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Farm Operations: livestock/orchard product catalog + the Farm Office -> Farm Admin
    /// request/approval workflow. Three client pages consume this:
    ///   - ProductRateManagement.razor (Farm Office): submit/track requests, needs productRateManagement.Entry
    ///   - ProductRateApprovals.razor (Farm Admin): approve/reject, needs ProductRateApprovals.Update
    ///   - FarmProductMaster.razor (Farm Admin): manage the master catalog, needs FarmProductMaster.Update
    /// Every write is enforced server-side via the SAME Designation.RoleAccess primitive the rest of
    /// the API already uses for this purpose (see WelfareSchemeApprovalController.
    /// UserHasDesignationPermissionAsync, GuestHouseMasterController's access filter) -
    /// UserInfo.DesignationId -> Designation.RoleAccess -> RoleAccessPermissions.HasPage/HasAction.
    /// Client-side button hiding is a UX nicety only, never the actual gate.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FarmProductController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ILogger<FarmProductController> _logger;

        public FarmProductController(AppDbContext db, ILogger<FarmProductController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // =====================================================================
        //  DTOs
        // =====================================================================

        public class FarmProductDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Group { get; set; } = "";
            public string Uom { get; set; } = "";
            public decimal CurrentRate { get; set; }
            public bool IsActive { get; set; }
            public DateTime UpdatedAt { get; set; }
        }

        public class FarmRequestDto
        {
            public int Id { get; set; }
            public string RequestType { get; set; } = "";
            public int? FarmProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string Group { get; set; } = "";
            public string Uom { get; set; } = "";
            public decimal? CurrentRate { get; set; }
            public decimal RequestedRate { get; set; }
            public string Reason { get; set; } = "";
            public string Status { get; set; } = "";
            public string RequestedBy { get; set; } = "";
            public DateTime RequestedAt { get; set; }
            public decimal? ApprovedPrice { get; set; }
            public string? ApprovedUom { get; set; }
            public string? AdminRemarks { get; set; }
            public string? RejectionReason { get; set; }
            public string? DecidedBy { get; set; }
            public DateTime? DecidedAt { get; set; }
        }

        public class RateHistoryEntryDto
        {
            public int RequestId { get; set; }
            public decimal Rate { get; set; }
            public string Tag { get; set; } = ""; // "Current" | "Previous"
            public DateTime DecidedAt { get; set; }
        }

        public class SubmitRequestPayload
        {
            public string RequestType { get; set; } = ""; // "NewProduct" | "RateChange"
            public int? FarmProductId { get; set; }        // RateChange only
            public string? ProductName { get; set; }       // NewProduct only
            public string? Group { get; set; }              // NewProduct only: "LiveStock" | "Orchard"
            public string Uom { get; set; } = "";
            public decimal Rate { get; set; }
            public string Reason { get; set; } = "";
        }

        public class UpdateRequestPayload
        {
            public string? ProductName { get; set; }       // NewProduct only
            public string? Group { get; set; }              // NewProduct only: "LiveStock" | "Orchard"
            public string Uom { get; set; } = "";
            public decimal Rate { get; set; }
            public string Reason { get; set; } = "";
        }

        public class DecidePayload
        {
            public decimal? ApprovedPrice { get; set; }
            public string? ApprovedUom { get; set; }
            public string? AdminRemarks { get; set; }
        }

        public class RejectPayload
        {
            public string RejectionReason { get; set; } = "";
        }

        public class FarmProductEditPayload
        {
            public string Name { get; set; } = "";
            public string Uom { get; set; } = "";
            public decimal CurrentRate { get; set; }
            public bool IsActive { get; set; } = true;
        }

        // =====================================================================
        //  GET products
        // =====================================================================

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts([FromQuery] string? group, [FromQuery] bool includeInactive = false)
        {
            if (!await UserHasAnyAsync(PagePermission.productRateManagement, PagePermission.ProductRateApprovals, PagePermission.FarmProductMaster))
                return Forbidden();

            var query = _db.FarmProducts.AsNoTracking().AsQueryable();
            // Only the Product Master page (Farm Admin) may see deactivated products.
            if (!includeInactive || !await UserHasAnyAsync(PagePermission.FarmProductMaster))
                query = query.Where(p => p.IsActive);
            if (!string.IsNullOrWhiteSpace(group) && Enum.TryParse<FarmProductGroup>(group, true, out var g))
                query = query.Where(p => p.Group == g);

            var products = await query.OrderBy(p => p.Name).ToListAsync();
            return Ok(products.Select(ToDto));
        }

        // =====================================================================
        //  GET requests (filtered list for both Farm Office's own tracking and Farm Admin's queue)
        // =====================================================================

        [HttpGet("requests")]
        public async Task<IActionResult> GetRequests(
            [FromQuery] DateTime? fromMonth,
            [FromQuery] DateTime? toMonth,
            [FromQuery] string? requestType,
            [FromQuery] string? group,
            [FromQuery] int? productId,
            [FromQuery] string? status)
        {
            if (!await UserHasAnyAsync(PagePermission.productRateManagement, PagePermission.ProductRateApprovals))
                return Forbidden();

            var query = _db.FarmProductRateRequests.AsNoTracking().Include(r => r.FarmProduct).AsQueryable();

            if (fromMonth.HasValue)
                query = query.Where(r => r.RequestedAt >= new DateTime(fromMonth.Value.Year, fromMonth.Value.Month, 1));
            if (toMonth.HasValue)
                query = query.Where(r => r.RequestedAt < new DateTime(toMonth.Value.Year, toMonth.Value.Month, 1).AddMonths(1));
            if (!string.IsNullOrWhiteSpace(requestType) && Enum.TryParse<FarmRequestType>(requestType, true, out var rt))
                query = query.Where(r => r.RequestType == rt);
            if (!string.IsNullOrWhiteSpace(group) && Enum.TryParse<FarmProductGroup>(group, true, out var g))
                query = query.Where(r => (r.FarmProduct != null && r.FarmProduct.Group == g) || r.ProposedGroup == g);
            if (productId.HasValue)
                query = query.Where(r => r.FarmProductId == productId.Value);
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<FarmRequestStatus>(status, true, out var st))
                query = query.Where(r => r.Status == st);

            var requests = await query.OrderByDescending(r => r.RequestedAt).ToListAsync();
            return Ok(requests.Select(ToDto));
        }

        [HttpGet("requests/{id:int}")]
        public async Task<IActionResult> GetRequest(int id)
        {
            if (!await UserHasAnyAsync(PagePermission.productRateManagement, PagePermission.ProductRateApprovals))
                return Forbidden();

            var request = await _db.FarmProductRateRequests.AsNoTracking()
                .Include(r => r.FarmProduct)
                .FirstOrDefaultAsync(r => r.Id == id);

            return request is null ? NotFound() : Ok(ToDto(request));
        }

        // =====================================================================
        //  POST requests - Farm Office submits (needs productRateManagement.Entry)
        // =====================================================================

        [HttpPost("requests")]
        public async Task<IActionResult> SubmitRequest([FromBody] SubmitRequestPayload payload)
        {
            if (!await UserHasActionAsync(PagePermission.productRateManagement, "Entry"))
                return Forbidden();

            if (!Enum.TryParse<FarmRequestType>(payload.RequestType, true, out var requestType))
                return BadRequest("Invalid request type.");
            if (string.IsNullOrWhiteSpace(payload.Uom))
                return BadRequest("UOM is required.");
            if (payload.Rate <= 0)
                return BadRequest("Rate must be greater than zero.");
            if (string.IsNullOrWhiteSpace(payload.Reason))
                return BadRequest("Reason is required.");

            var request = new FarmProductRateRequest
            {
                RequestType = requestType,
                ProposedUom = payload.Uom,
                RequestedRate = payload.Rate,
                Reason = payload.Reason,
                RequestedBy = await CurrentUserDisplayNameAsync(),
                RequestedAt = DateTime.Now,
                Status = FarmRequestStatus.Pending
            };

            if (requestType == FarmRequestType.NewProduct)
            {
                if (string.IsNullOrWhiteSpace(payload.ProductName))
                    return BadRequest("Product name is required.");
                if (!Enum.TryParse<FarmProductGroup>(payload.Group, true, out var group))
                    return BadRequest("Product group is required.");

                request.ProposedProductName = payload.ProductName.Trim();
                request.ProposedGroup = group;
            }
            else
            {
                if (!payload.FarmProductId.HasValue)
                    return BadRequest("Product is required for a rate change.");

                var product = await _db.FarmProducts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == payload.FarmProductId.Value);
                if (product is null)
                    return BadRequest("Product not found.");

                request.FarmProductId = product.Id;
                request.CurrentRateAtRequestTime = product.CurrentRate;
            }

            _db.FarmProductRateRequests.Add(request);
            await _db.SaveChangesAsync();

            return Ok(ToDto(request));
        }

        // =====================================================================
        //  PUT requests/{id} - Farm Office editing their own still-pending request
        //  (needs productRateManagement.Entry; the request must still be Pending)
        // =====================================================================

        [HttpPut("requests/{id:int}")]
        public async Task<IActionResult> UpdateRequest(int id, [FromBody] UpdateRequestPayload payload)
        {
            if (!await UserHasActionAsync(PagePermission.productRateManagement, "Entry"))
                return Forbidden();

            var request = await _db.FarmProductRateRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();
            if (request.Status != FarmRequestStatus.Pending && request.Status != FarmRequestStatus.Rejected)
                return BadRequest("Only a pending or rejected request can be edited.");

            if (request.Status == FarmRequestStatus.Rejected)
            {
                request.Status = FarmRequestStatus.Pending;
                request.RejectionReason = null;
                request.AdminRemarks = null;
                request.DecidedBy = null;
                request.DecidedAt = null;
                request.RequestedAt = DateTime.Now;
            }

            if (string.IsNullOrWhiteSpace(payload.Uom))
                return BadRequest("UOM is required.");
            if (payload.Rate <= 0)
                return BadRequest("Rate must be greater than zero.");
            if (string.IsNullOrWhiteSpace(payload.Reason))
                return BadRequest("Reason is required.");

            if (request.RequestType == FarmRequestType.NewProduct)
            {
                if (string.IsNullOrWhiteSpace(payload.ProductName))
                    return BadRequest("Product name is required.");
                if (!Enum.TryParse<FarmProductGroup>(payload.Group, true, out var group))
                    return BadRequest("Product group is required.");

                request.ProposedProductName = payload.ProductName.Trim();
                request.ProposedGroup = group;
            }

            request.ProposedUom = payload.Uom;
            request.RequestedRate = payload.Rate;
            request.Reason = payload.Reason;

            await _db.SaveChangesAsync();
            return Ok(ToDto(request));
        }

        // =====================================================================
        //  Approve / Reject - Farm Admin (needs ProductRateApprovals.Update)
        // =====================================================================

        [HttpPost("requests/{id:int}/approve")]
        public async Task<IActionResult> Approve(int id, [FromBody] DecidePayload payload)
        {
            if (!await UserHasActionAsync(PagePermission.ProductRateApprovals, "Update"))
                return Forbidden();

            var request = await _db.FarmProductRateRequests.Include(r => r.FarmProduct).FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();
            if (request.Status != FarmRequestStatus.Pending)
                return BadRequest("This request has already been decided.");

            var decidedBy = await CurrentUserDisplayNameAsync();
            var finalRate = payload.ApprovedPrice ?? request.RequestedRate;
            var finalUom = string.IsNullOrWhiteSpace(payload.ApprovedUom) ? request.ProposedUom : payload.ApprovedUom;

            if (request.RequestType == FarmRequestType.NewProduct)
            {
                var product = new FarmProduct
                {
                    Name = request.ProposedProductName!,
                    Group = request.ProposedGroup!.Value,
                    Uom = finalUom,
                    CurrentRate = finalRate,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = decidedBy
                };
                _db.FarmProducts.Add(product);
                await _db.SaveChangesAsync(); // need product.Id before linking
                request.FarmProductId = product.Id;
            }
            else
            {
                if (request.FarmProduct is null) return BadRequest("Linked product no longer exists.");
                request.FarmProduct.CurrentRate = finalRate;
                request.FarmProduct.UpdatedAt = DateTime.Now;
                request.FarmProduct.UpdatedBy = decidedBy;
            }

            request.ApprovedPrice = finalRate;
            request.ApprovedUom = finalUom;
            request.AdminRemarks = payload.AdminRemarks;
            request.Status = FarmRequestStatus.Approved;
            request.DecidedBy = decidedBy;
            request.DecidedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return Ok(ToDto(request));
        }

        [HttpPost("requests/{id:int}/reject")]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectPayload payload)
        {
            if (!await UserHasActionAsync(PagePermission.ProductRateApprovals, "Update"))
                return Forbidden();

            if (string.IsNullOrWhiteSpace(payload.RejectionReason))
                return BadRequest("A rejection reason is required.");

            var request = await _db.FarmProductRateRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();
            if (request.Status != FarmRequestStatus.Pending)
                return BadRequest("This request has already been decided.");

            request.Status = FarmRequestStatus.Rejected;
            request.RejectionReason = payload.RejectionReason;
            request.DecidedBy = await CurrentUserDisplayNameAsync();
            request.DecidedAt = DateTime.Now;

            await _db.SaveChangesAsync();
            return Ok(ToDto(request));
        }

        [HttpDelete("requests/{id:int}")]
        public async Task<IActionResult> DeleteRequest(int id)
        {
            // Farm Office deleting their own still-pending request, or Farm Admin cleaning up the queue.
            if (!await UserHasActionAsync(PagePermission.productRateManagement, "Entry")
                && !await UserHasActionAsync(PagePermission.ProductRateApprovals, "Update"))
                return Forbidden();

            var request = await _db.FarmProductRateRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (request is null) return NotFound();

            _db.FarmProductRateRequests.Remove(request);
            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        // =====================================================================
        //  Product Master - Farm Admin direct edit (needs FarmProductMaster.Update)
        // =====================================================================

        [HttpPut("products/{id:int}")]
        public async Task<IActionResult> EditProduct(int id, [FromBody] FarmProductEditPayload payload)
        {
            if (!await UserHasActionAsync(PagePermission.FarmProductMaster, "Update"))
                return Forbidden();

            var product = await _db.FarmProducts.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return NotFound();

            product.Name = payload.Name;
            product.Uom = payload.Uom;
            product.CurrentRate = payload.CurrentRate;
            product.IsActive = payload.IsActive;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = await CurrentUserDisplayNameAsync();

            await _db.SaveChangesAsync();
            return Ok(ToDto(product));
        }

        [HttpDelete("products/{id:int}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (!await UserHasActionAsync(PagePermission.FarmProductMaster, "Update"))
                return Forbidden();

            var product = await _db.FarmProducts.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null) return NotFound();

            // Soft delete - consistent with LocationMaster/Agriculture's IsActive convention.
            product.IsActive = false;
            product.UpdatedAt = DateTime.Now;
            product.UpdatedBy = await CurrentUserDisplayNameAsync();

            await _db.SaveChangesAsync();
            return Ok(new { Success = true });
        }

        [HttpGet("products/{id:int}/rate-history")]
        public async Task<IActionResult> GetRateHistory(int id)
        {
            if (!await UserHasAnyAsync(PagePermission.FarmProductMaster, PagePermission.ProductRateApprovals))
                return Forbidden();

            var approved = await _db.FarmProductRateRequests.AsNoTracking()
                .Where(r => r.FarmProductId == id && r.Status == FarmRequestStatus.Approved && r.ApprovedPrice.HasValue)
                .OrderByDescending(r => r.DecidedAt)
                .ToListAsync();

            var history = approved.Select((r, index) => new RateHistoryEntryDto
            {
                RequestId = r.Id,
                Rate = r.ApprovedPrice!.Value,
                Tag = index == 0 ? "Current" : "Previous",
                DecidedAt = r.DecidedAt ?? r.RequestedAt
            }).ToList();

            return Ok(history);
        }

        // =====================================================================
        //  Permission helpers - UserInfo.DesignationId -> Designation.RoleAccess ->
        //  RoleAccessPermissions, the same primitive used by WelfareSchemeApprovalController
        //  (UserHasDesignationPermissionAsync) and GuestHouseMasterController.
        // =====================================================================

        private async Task<Designation?> GetDesignationAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user?.DesignationId is null) return null;

            return await _db.Designations.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == user.DesignationId.Value && d.IsActive);
        }

        private async Task<bool> UserHasAnyAsync(params PagePermission[] pages)
        {
            var designation = await GetDesignationAsync();
            return designation != null && pages.Any(p => RoleAccessPermissions.HasPage(designation.RoleAccess, p));
        }

        private async Task<bool> UserHasActionAsync(PagePermission page, string action)
        {
            var designation = await GetDesignationAsync();
            if (designation is null) return false;

            var pageKey = RoleAccessPermissions.KeyFor(page);
            var tokens = RoleAccessPermissions.ParseTokens(designation.RoleAccess);
            return tokens.Any(t =>
            {
                var dot = t.IndexOf('.');
                if (dot < 0) return false;
                var tokenPage = RoleAccessPermissions.NormalizePageKey(t[..dot]);
                var tokenAction = t[(dot + 1)..];
                return string.Equals(tokenPage, pageKey, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(tokenAction, action, StringComparison.OrdinalIgnoreCase);
            });
        }

        private async Task<string> CurrentUserDisplayNameAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return "Unknown";
            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            return user?.Name ?? user?.UserName ?? "Unknown";
        }

        private ObjectResult Forbidden() =>
            new(new { Success = false, Message = "You do not have permission to perform this action." })
            { StatusCode = StatusCodes.Status403Forbidden };

        // =====================================================================
        //  Mapping
        // =====================================================================

        private static FarmProductDto ToDto(FarmProduct p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Group = p.Group.ToString(),
            Uom = p.Uom,
            CurrentRate = p.CurrentRate,
            IsActive = p.IsActive,
            UpdatedAt = p.UpdatedAt
        };

        private static FarmRequestDto ToDto(FarmProductRateRequest r) => new()
        {
            Id = r.Id,
            RequestType = r.RequestType.ToString(),
            FarmProductId = r.FarmProductId,
            ProductName = r.FarmProduct?.Name ?? r.ProposedProductName ?? "",
            Group = (r.FarmProduct?.Group ?? r.ProposedGroup)?.ToString() ?? "",
            Uom = r.ApprovedUom ?? r.ProposedUom,
            CurrentRate = r.CurrentRateAtRequestTime ?? r.FarmProduct?.CurrentRate,
            RequestedRate = r.RequestedRate,
            Reason = r.Reason,
            Status = r.Status.ToString(),
            RequestedBy = r.RequestedBy,
            RequestedAt = r.RequestedAt,
            ApprovedPrice = r.ApprovedPrice,
            ApprovedUom = r.ApprovedUom,
            AdminRemarks = r.AdminRemarks,
            RejectionReason = r.RejectionReason,
            DecidedBy = r.DecidedBy,
            DecidedAt = r.DecidedAt
        };
    }
}
