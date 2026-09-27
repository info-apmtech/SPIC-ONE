using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;
using SPIC.Core.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace SpicAPI.Controllers
{
    /// <summary>
    /// Digital Library content: AI videos, product information pages and brochures.
    /// The three kinds share one table (LibraryContent); the wizard's step 2/3 state
    /// (related content, detail layout) and custom sections are stored as JSON columns.
    ///
    /// Authorization
    ///   * Reads of PUBLISHED content, the lookups and the stats are open to every signed-in user.
    ///   * Draft/Archived reads and every write need Admin/CorporateAdmin, or a Designation
    ///     whose RoleAccess grants the DigitalLibrary page (same CSV parsing LoginState uses).
    ///   * The category master (api/Library/categories, table LibraryCategories) follows the same
    ///     rule: every signed-in user reads it, writes need the DigitalLibrary page.
    ///
    /// Files live under Uploads/Library/{id}/ and are served by GET api/Library/file/{*path},
    /// which is on the ?access_token= allowlist in Program.cs so &lt;img&gt;/&lt;video&gt; tags work.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LibraryController : ControllerBase
    {
        private const long MaxCoverBytes = 5L * 1024 * 1024;
        private const long MaxVideoBytes = 200L * 1024 * 1024;
        private const long MaxDocumentBytes = 25L * 1024 * 1024;

        private static readonly string[] CoverExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] VideoExtensions = { ".mp4", ".webm", ".mov" };
        private static readonly string[] DocumentExtensions = { ".pdf" };

        // Categories and sub-categories come from the LibraryCategories master (V5r seeds the
        // defaults that used to be hard-coded here); see the "Category master" region.
        private static readonly string[] DefaultFormats = { "Video", "PDF", "Article" };
        private static readonly string[] DefaultVisibilities = { "Everyone", "Staff", "Dealers" };

        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LibraryController> _logger;

        public LibraryController(AppDbContext db, IWebHostEnvironment env, ILogger<LibraryController> logger)
        {
            _db = db;
            _env = env;
            _logger = logger;
        }

        // =====================================================================
        //  Permission
        // =====================================================================

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
        private string CurrentUserName => User.Identity?.Name ?? "system";

        private bool IsAdminRole()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return string.Equals(role, nameof(AppRole.Admin), StringComparison.OrdinalIgnoreCase)
                || string.Equals(role, nameof(AppRole.CorporateAdmin), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Admin/CorporateAdmin, or a designation that grants the DigitalLibrary page.</summary>
        private async Task<bool> CanManageAsync()
        {
            if (IsAdminRole()) return true;

            var userId = CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId)) return false;

            var designationId = await _db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.DesignationId)
                .FirstOrDefaultAsync();

            if (!designationId.HasValue || designationId.Value <= 0) return false;

            var roleAccess = await _db.Designations
                .AsNoTracking()
                .Where(d => d.Id == designationId.Value && d.IsActive)
                .Select(d => d.RoleAccess)
                .FirstOrDefaultAsync();

            return RoleAccessPermissions.HasPage(roleAccess, PagePermission.DigitalLibrary);
        }

        private ObjectResult Forbidden(string message = "You are not authorized to manage Digital Library content.") =>
            StatusCode(403, new { Success = false, Message = message });

        // =====================================================================
        //  Stats / lookups
        // =====================================================================

        // GET /api/Library/stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // Readers (no DigitalLibrary page in their designation) only ever see published content,
            // so their counts exclude drafts and archived items.
            var canManage = await CanManageAsync();
            var rows = await _db.LibraryContents
                .AsNoTracking()
                .Where(c => !c.IsDeleted && (canManage || c.Status == LibraryContentStatus.Published))
                .Select(c => new { c.Kind, c.Status, c.Views })
                .ToListAsync();

            var stats = new LibraryStatsDto
            {
                Total = rows.Count,
                Published = rows.Count(r => r.Status == LibraryContentStatus.Published),
                Drafts = rows.Count(r => r.Status == LibraryContentStatus.Draft),
                Videos = rows.Count(r => r.Kind == LibraryContentKind.Video),
                Products = rows.Count(r => r.Kind == LibraryContentKind.Product),
                Brochures = rows.Count(r => r.Kind == LibraryContentKind.Brochure),
                TotalViews = rows.Sum(r => r.Views)
            };

            return Ok(stats);
        }

        // GET /api/Library/lookups
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            var rows = await _db.LibraryContents
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Category, c.SubCategory, c.Format, c.Visibility, c.Tags })
                .ToListAsync();

            static List<string> Merge(IEnumerable<string?> used, IEnumerable<string> defaults) =>
                defaults
                    .Concat(used.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v!.Trim()))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                    .ToList();

            // Categories / sub-categories: the ACTIVE master names in master order (SortOrder, Name),
            // then any other name content already uses (A-Z), so nothing in use disappears.
            static List<string> MergeMaster(IEnumerable<string> master, IEnumerable<string?> used)
            {
                var result = master.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var seen = new HashSet<string>(result, StringComparer.OrdinalIgnoreCase);
                result.AddRange(used
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v!.Trim())
                    .Where(seen.Add)
                    .OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
                return result;
            }

            var tree = await LoadCategoryTreeAsync(includeInactive: false, withUsage: false);

            // Tags: distinct ignoring case (first spelling seen wins), most used first, then A-Z.
            var tags = rows
                .SelectMany(r => SplitTags(r.Tags))
                .GroupBy(t => t, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            return Ok(new LibraryLookupsDto
            {
                Categories = MergeMaster(tree.Select(c => c.Name), rows.Select(r => r.Category)),
                SubCategories = MergeMaster(tree.SelectMany(c => c.SubCategories).Select(s => s.Name), rows.Select(r => r.SubCategory)),
                Formats = Merge(rows.Select(r => r.Format), DefaultFormats),
                Visibilities = Merge(rows.Select(r => r.Visibility), DefaultVisibilities),
                CategoryTree = tree,
                Tags = tags
            });
        }

        // =====================================================================
        //  List / detail
        // =====================================================================

        // GET /api/Library?kind=&status=&q=&page=&pageSize=
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] LibraryContentKind? kind,
            [FromQuery] LibraryContentStatus? status,
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var canManage = await CanManageAsync();

            if (!canManage && status.HasValue && status.Value != LibraryContentStatus.Published)
                return Forbidden("Only published Digital Library content is available to you.");

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var query = _db.LibraryContents.AsNoTracking().Where(c => !c.IsDeleted);

            if (!canManage)
                query = query.Where(c => c.Status == LibraryContentStatus.Published);
            else if (status.HasValue)
                query = query.Where(c => c.Status == status.Value);

            if (kind.HasValue)
                query = query.Where(c => c.Kind == kind.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(c =>
                    c.Title.ToLower().Contains(term) ||
                    (c.ShortDescription != null && c.ShortDescription.ToLower().Contains(term)) ||
                    (c.Keywords != null && c.Keywords.ToLower().Contains(term)) ||
                    (c.Tags != null && c.Tags.ToLower().Contains(term)) ||
                    (c.Category != null && c.Category.ToLower().Contains(term)) ||
                    (c.SubCategory != null && c.SubCategory.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
                .ThenByDescending(c => c.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PageResult<LibraryContentSummaryDto>
            {
                Items = items.Select(MapSummary).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize
            });
        }

        // GET /api/Library/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            var content = await _db.LibraryContents.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (content == null)
                return NotFound(new { Success = false, Message = "Library content not found." });

            if (content.Status != LibraryContentStatus.Published && !await CanManageAsync())
                return Forbidden("This Digital Library item is not published.");

            content.Views += 1;
            await _db.SaveChangesAsync();

            var detail = MapDetail(content);
            await ResolveRelatedAsync(detail);
            return Ok(detail);
        }

        // =====================================================================
        //  Create / update / status / delete
        // =====================================================================

        // POST /api/Library
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] LibraryContentUpsertDto? payload)
        {
            if (!await CanManageAsync()) return Forbidden();

            if (payload == null || string.IsNullOrWhiteSpace(payload.Title))
                return BadRequest(new { Success = false, Message = "Title is required." });

            var now = DateTime.Now;
            var content = new LibraryContent
            {
                AuthorUserId = CurrentUserId,
                AuthorName = CurrentUserName,
                CreatedBy = CurrentUserName,
                CreatedAt = now,
                UpdatedBy = CurrentUserName,
                UpdatedAt = now
            };

            ApplyUpsert(content, payload, now);
            _db.LibraryContents.Add(content);
            await _db.SaveChangesAsync();

            var detail = MapDetail(content);
            await ResolveRelatedAsync(detail);
            return Ok(detail);
        }

        // PUT /api/Library/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] LibraryContentUpsertDto? payload)
        {
            if (!await CanManageAsync()) return Forbidden();

            if (payload == null || string.IsNullOrWhiteSpace(payload.Title))
                return BadRequest(new { Success = false, Message = "Title is required." });

            var content = await _db.LibraryContents.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (content == null)
                return NotFound(new { Success = false, Message = "Library content not found." });

            var now = DateTime.Now;
            ApplyUpsert(content, payload, now);
            content.UpdatedBy = CurrentUserName;
            content.UpdatedAt = now;

            await _db.SaveChangesAsync();

            var detail = MapDetail(content);
            await ResolveRelatedAsync(detail);
            return Ok(detail);
        }

        // PATCH /api/Library/{id}/status?status=Published|Draft|Archived
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> SetStatus(int id, [FromQuery] LibraryContentStatus status)
        {
            if (!await CanManageAsync()) return Forbidden();

            var content = await _db.LibraryContents.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (content == null)
                return NotFound(new { Success = false, Message = "Library content not found." });

            content.Status = status;
            // PublishedAt is stamped on the FIRST transition to Published and never moves after that.
            if (status == LibraryContentStatus.Published && !content.PublishedAt.HasValue)
                content.PublishedAt = DateTime.Now;

            content.UpdatedBy = CurrentUserName;
            content.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(MapSummary(content));
        }

        // DELETE /api/Library/{id}  (soft delete)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await CanManageAsync()) return Forbidden();

            var content = await _db.LibraryContents.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (content == null)
                return NotFound(new { Success = false, Message = "Library content not found." });

            content.IsDeleted = true;
            content.UpdatedBy = CurrentUserName;
            content.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new { Success = true, Message = "Library content deleted." });
        }

        // =====================================================================
        //  Uploads
        // =====================================================================

        // POST /api/Library/{id}/cover
        [HttpPost("{id:int}/cover")]
        [RequestSizeLimit(MaxCoverBytes + (1024 * 1024))]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxCoverBytes + (1024 * 1024))]
        public Task<IActionResult> UploadCover(int id, IFormFile? file) =>
            StoreFileAsync(id, file, "cover", CoverExtensions, MaxCoverBytes);

        // POST /api/Library/{id}/video
        [HttpPost("{id:int}/video")]
        [RequestSizeLimit(MaxVideoBytes + (4L * 1024 * 1024))]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxVideoBytes + (4L * 1024 * 1024))]
        public Task<IActionResult> UploadVideo(int id, IFormFile? file) =>
            StoreFileAsync(id, file, "video", VideoExtensions, MaxVideoBytes);

        // POST /api/Library/{id}/document
        [HttpPost("{id:int}/document")]
        [RequestSizeLimit(MaxDocumentBytes + (1024 * 1024))]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxDocumentBytes + (1024 * 1024))]
        public Task<IActionResult> UploadDocument(int id, IFormFile? file) =>
            StoreFileAsync(id, file, "document", DocumentExtensions, MaxDocumentBytes);

        private async Task<IActionResult> StoreFileAsync(
            int id, IFormFile? file, string kind, string[] allowedExtensions, long maxBytes)
        {
            if (!await CanManageAsync()) return Forbidden();

            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded." });

            var content = await _db.LibraryContents.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (content == null)
                return NotFound(new { Success = false, Message = "Library content not found." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new
                {
                    Success = false,
                    Message = $"Only {string.Join(", ", allowedExtensions.Select(e => e.TrimStart('.').ToUpperInvariant()))} files are allowed."
                });

            if (file.Length > maxBytes)
                return BadRequest(new { Success = false, Message = $"File must be {maxBytes / (1024 * 1024)} MB or less." });

            var folder = Path.Combine(_env.ContentRootPath, "Uploads", "Library", id.ToString());
            Directory.CreateDirectory(folder);

            var storedName = $"{kind}_{DateTime.Now:yyyyMMddHHmmssfff}{ext}";
            var physicalPath = Path.Combine(folder, storedName);

            await using (var stream = new FileStream(physicalPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"Library/{id}/{storedName}";

            // Uploading replaces the previous file of this kind.
            var previous = kind switch
            {
                "cover" => content.CoverImagePath,
                "video" => content.VideoFilePath,
                _ => content.DocumentPath
            };
            DeletePrevious(previous, relativePath);

            switch (kind)
            {
                case "cover":
                    content.CoverImagePath = relativePath;
                    break;
                case "video":
                    content.VideoFilePath = relativePath;
                    break;
                default:
                    content.DocumentPath = relativePath;
                    content.DocumentName = Path.GetFileName(file.FileName);
                    content.DocumentSize = file.Length;
                    break;
            }

            content.UpdatedBy = CurrentUserName;
            content.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Ok(new LibraryFileDto
            {
                Path = relativePath,
                FileName = Path.GetFileName(file.FileName),
                Size = file.Length,
                ContentType = GetContentType(ext)
            });
        }

        private void DeletePrevious(string? previousRelativePath, string newRelativePath)
        {
            if (string.IsNullOrWhiteSpace(previousRelativePath)) return;
            if (string.Equals(previousRelativePath, newRelativePath, StringComparison.OrdinalIgnoreCase)) return;

            var full = ResolveUploadPath(previousRelativePath);
            if (full == null) return;

            try
            {
                if (System.IO.File.Exists(full)) System.IO.File.Delete(full);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete the replaced library file {Path}", previousRelativePath);
            }
        }

        // =====================================================================
        //  File view
        // =====================================================================

        // GET /api/Library/file/{*path}  (also reachable with ?access_token= for <img>/<video>)
        [HttpGet("file/{*path}")]
        public IActionResult GetFile(string path)
        {
            var fullPath = ResolveUploadPath(path);
            if (fullPath == null || !System.IO.File.Exists(fullPath))
                return NotFound(new { Success = false, Message = "File not found." });

            var contentType = GetContentType(Path.GetExtension(fullPath).ToLowerInvariant());
            // Range processing lets the phone/browser seek inside an uploaded video.
            return PhysicalFile(fullPath, contentType, enableRangeProcessing: true);
        }

        /// <summary>Maps a stored relative path to a physical one, refusing anything outside Uploads.</summary>
        private string? ResolveUploadPath(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return null;

            var cleaned = relativePath.Replace('\\', '/').TrimStart('/');
            if (cleaned.Contains("..")) return null;

            var uploadsRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "Uploads"));
            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, cleaned));
            }
            catch
            {
                return null;
            }

            return fullPath.StartsWith(uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                ? fullPath
                : null;
        }

        private static string GetContentType(string ext) => ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".mov" => "video/quicktime",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };

        // =====================================================================
        //  Mapping
        // =====================================================================

        private void ApplyUpsert(LibraryContent content, LibraryContentUpsertDto payload, DateTime now)
        {
            content.Kind = payload.Kind;
            content.Title = payload.Title.Trim();
            content.Category = NullIfEmpty(payload.Category);
            content.SubCategory = NullIfEmpty(payload.SubCategory);
            content.Format = NullIfEmpty(payload.Format);
            content.Visibility = string.IsNullOrWhiteSpace(payload.Visibility) ? "Everyone" : payload.Visibility.Trim();
            content.Keywords = NullIfEmpty(payload.Keywords);
            content.Tags = payload.Tags.Count == 0
                ? null
                : string.Join(",", payload.Tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()));
            content.ShortDescription = NullIfEmpty(payload.ShortDescription);
            content.Overview = NullIfEmpty(payload.Overview);
            content.Features = NullIfEmpty(payload.Features);
            content.Usage = NullIfEmpty(payload.Usage);
            content.AdditionalSectionsJson = Serialize(payload.AdditionalSections);
            content.VideoUrl = NullIfEmpty(payload.VideoUrl);
            content.DurationSeconds = payload.DurationSeconds;
            content.RelatedVideosJson = Serialize(payload.RelatedVideos);
            content.RelatedBrochuresJson = Serialize(payload.RelatedBrochures);
            content.LayoutJson = Serialize(payload.Layout);

            // Archived is only reachable through PATCH status; the wizard sends Draft or Published.
            var status = payload.Status == LibraryContentStatus.Archived
                ? content.Status
                : payload.Status;

            content.Status = status;
            if (status == LibraryContentStatus.Published && !content.PublishedAt.HasValue)
                content.PublishedAt = now;
        }

        private static LibraryContentSummaryDto MapSummary(LibraryContent c) => new()
        {
            Id = c.Id,
            Kind = c.Kind,
            Status = c.Status,
            Title = c.Title,
            ShortDescription = c.ShortDescription,
            Category = c.Category,
            SubCategory = c.SubCategory,
            Author = string.IsNullOrWhiteSpace(c.AuthorName) ? c.CreatedBy : c.AuthorName,
            CreatedAt = c.CreatedAt,
            PublishedAt = c.PublishedAt,
            CoverImagePath = c.CoverImagePath,
            Views = c.Views,
            Tags = SplitTags(c.Tags),
            DurationSeconds = c.DurationSeconds
        };

        private static LibraryContentDetailDto MapDetail(LibraryContent c) => new()
        {
            Id = c.Id,
            Kind = c.Kind,
            Status = c.Status,
            Title = c.Title,
            ShortDescription = c.ShortDescription,
            Category = c.Category,
            SubCategory = c.SubCategory,
            Author = string.IsNullOrWhiteSpace(c.AuthorName) ? c.CreatedBy : c.AuthorName,
            CreatedAt = c.CreatedAt,
            PublishedAt = c.PublishedAt,
            CoverImagePath = c.CoverImagePath,
            Views = c.Views,
            Tags = SplitTags(c.Tags),
            DurationSeconds = c.DurationSeconds,

            Format = c.Format,
            Visibility = c.Visibility,
            Keywords = c.Keywords,
            Overview = c.Overview,
            Features = c.Features,
            Usage = c.Usage,
            AdditionalSections = Deserialize<List<LibraryCustomSectionDto>>(c.AdditionalSectionsJson) ?? new(),
            VideoUrl = c.VideoUrl,
            VideoFilePath = c.VideoFilePath,
            DocumentPath = c.DocumentPath,
            DocumentName = c.DocumentName,
            DocumentSize = c.DocumentSize,
            RelatedVideos = Deserialize<LibraryRelatedDto>(c.RelatedVideosJson) ?? new(),
            RelatedBrochures = Deserialize<LibraryRelatedDto>(c.RelatedBrochuresJson) ?? new(),
            Layout = Deserialize<List<LibraryLayoutSectionDto>>(c.LayoutJson) ?? new(),
            UpdatedBy = c.UpdatedBy,
            UpdatedAt = c.UpdatedAt
        };

        /// <summary>Fills RelatedVideoItems/RelatedBrochureItems with published items, in the stored id order.</summary>
        private async Task ResolveRelatedAsync(LibraryContentDetailDto detail)
        {
            var ids = detail.RelatedVideos.Ids
                .Concat(detail.RelatedBrochures.Ids)
                .Distinct()
                .ToList();

            if (ids.Count == 0) return;

            var rows = await _db.LibraryContents
                .AsNoTracking()
                .Where(c => ids.Contains(c.Id) && !c.IsDeleted && c.Status == LibraryContentStatus.Published)
                .ToListAsync();

            var byId = rows.ToDictionary(r => r.Id, MapSummary);

            detail.RelatedVideoItems = detail.RelatedVideos.Ids
                .Where(byId.ContainsKey).Select(i => byId[i]).ToList();
            detail.RelatedBrochureItems = detail.RelatedBrochures.Ids
                .Where(byId.ContainsKey).Select(i => byId[i]).ToList();
        }

        private static List<string> SplitTags(string? csv) =>
            string.IsNullOrWhiteSpace(csv)
                ? new List<string>()
                : csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static string? Serialize<T>(T? value) =>
            value == null ? null : JsonSerializer.Serialize(value, Json);

        private static T? Deserialize<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            try
            {
                return JsonSerializer.Deserialize<T>(json, Json);
            }
            catch (JsonException)
            {
                return default;
            }
        }

        // =====================================================================
        //  Category master (V5r): the category / sub-category options on the content forms
        //  Reads: every signed-in user (inactive rows only for managers).
        //  Writes: same rule as content (CanManageAsync).
        // =====================================================================

        private const int MaxCategoryNameLength = 150;

        // GET /api/Library/categories?includeInactive=
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories([FromQuery] bool includeInactive = false)
        {
            if (includeInactive && !await CanManageAsync()) includeInactive = false;
            return Ok(await LoadCategoryTreeAsync(includeInactive, withUsage: true));
        }

        // POST /api/Library/categories
        [HttpPost("categories")]
        public async Task<IActionResult> CreateCategory([FromBody] LibraryCategoryUpsertDto payload)
        {
            if (!await CanManageAsync()) return Forbidden("You are not authorized to manage Digital Library categories.");
            if (payload is null) return BadRequest(new { Success = false, Message = "Category details are required." });
            if (!Enum.IsDefined(payload.Kind)) return BadRequest(new { Success = false, Message = "Unknown category kind." });

            var (name, error) = CleanCategoryName(payload.Name, payload.Kind);
            if (error is not null) return BadRequest(new { Success = false, Message = error });

            int? parentId = null;
            if (payload.Kind == LibraryCategoryKind.SubCategory)
            {
                var parentError = await ValidateParentAsync(payload.ParentCategoryId);
                if (parentError is not null) return BadRequest(new { Success = false, Message = parentError });
                parentId = payload.ParentCategoryId;
            }

            var clash = await FindCategoryClashAsync(payload.Kind, name!, exceptId: null);
            if (clash is not null) return Conflict(new { Success = false, Message = clash });

            var sortOrder = payload.SortOrder ?? (await _db.LibraryCategories
                .Where(c => c.Kind == payload.Kind && c.ParentCategoryId == parentId)
                .MaxAsync(c => (int?)c.SortOrder) ?? 0) + 1;

            var now = DateTime.Now;
            var entity = new LibraryCategory
            {
                Kind = payload.Kind,
                Name = name!,
                ParentCategoryId = parentId,
                SortOrder = sortOrder,
                IsActive = true,
                CreatedBy = CurrentUserName,
                CreatedAt = now,
                UpdatedBy = CurrentUserName,
                UpdatedAt = now
            };

            _db.LibraryCategories.Add(entity);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Two admins adding the same name at the same moment: the unique index wins.
                _logger.LogWarning(ex, "Library category insert failed for {Name}", name);
                return Conflict(new { Success = false, Message = $"\"{name}\" already exists." });
            }

            return Ok(await LoadCategoryDtoAsync(entity.Id));
        }

        // PUT /api/Library/categories/{id}
        [HttpPut("categories/{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] LibraryCategoryUpsertDto payload)
        {
            if (!await CanManageAsync()) return Forbidden("You are not authorized to manage Digital Library categories.");
            if (payload is null) return BadRequest(new { Success = false, Message = "Category details are required." });

            var entity = await _db.LibraryCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null) return NotFound(new { Success = false, Message = "Category not found." });

            var (name, error) = CleanCategoryName(payload.Name, entity.Kind);
            if (error is not null) return BadRequest(new { Success = false, Message = error });

            if (entity.Kind == LibraryCategoryKind.SubCategory)
            {
                var parentError = await ValidateParentAsync(payload.ParentCategoryId);
                if (parentError is not null) return BadRequest(new { Success = false, Message = parentError });
                entity.ParentCategoryId = payload.ParentCategoryId;
            }

            var clash = await FindCategoryClashAsync(entity.Kind, name!, exceptId: entity.Id);
            if (clash is not null) return Conflict(new { Success = false, Message = clash });

            var oldName = entity.Name;
            var renamed = !string.Equals(oldName, name, StringComparison.Ordinal);

            entity.Name = name!;
            if (payload.SortOrder.HasValue) entity.SortOrder = payload.SortOrder.Value;
            entity.UpdatedBy = CurrentUserName;
            entity.UpdatedAt = DateTime.Now;

            await using var tx = await _db.Database.BeginTransactionAsync();
            await _db.SaveChangesAsync();

            // Content stores the name, so a rename (including a change of case) is carried into
            // every content row that used the old name; otherwise the old spelling would come back
            // through the lookups merge.
            var moved = 0;
            if (renamed)
            {
                var oldLower = oldName.ToLower();
                moved = entity.Kind == LibraryCategoryKind.Category
                    ? await _db.LibraryContents
                        .Where(c => c.Category != null && c.Category.ToLower() == oldLower)
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.Category, name))
                    : await _db.LibraryContents
                        .Where(c => c.SubCategory != null && c.SubCategory.ToLower() == oldLower)
                        .ExecuteUpdateAsync(s => s.SetProperty(c => c.SubCategory, name));
            }

            await tx.CommitAsync();

            if (moved > 0)
                _logger.LogInformation("Library category {Id} renamed from {Old} to {New}; {Count} content rows updated", id, oldName, name, moved);

            return Ok(await LoadCategoryDtoAsync(entity.Id));
        }

        // PATCH /api/Library/categories/{id}/active?value=true|false
        [HttpPatch("categories/{id:int}/active")]
        public async Task<IActionResult> SetCategoryActive(int id, [FromQuery] bool value)
        {
            if (!await CanManageAsync()) return Forbidden("You are not authorized to manage Digital Library categories.");

            var entity = await _db.LibraryCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null) return NotFound(new { Success = false, Message = "Category not found." });

            if (entity.IsActive != value)
            {
                entity.IsActive = value;
                entity.UpdatedBy = CurrentUserName;
                entity.UpdatedAt = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return Ok(await LoadCategoryDtoAsync(entity.Id));
        }

        // DELETE /api/Library/categories/{id}
        [HttpDelete("categories/{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (!await CanManageAsync()) return Forbidden("You are not authorized to manage Digital Library categories.");

            var entity = await _db.LibraryCategories.FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null) return NotFound(new { Success = false, Message = "Category not found." });

            var lower = entity.Name.ToLower();
            var inUse = entity.Kind == LibraryCategoryKind.Category
                ? await _db.LibraryContents.CountAsync(c => !c.IsDeleted && c.Category != null && c.Category.ToLower() == lower)
                : await _db.LibraryContents.CountAsync(c => !c.IsDeleted && c.SubCategory != null && c.SubCategory.ToLower() == lower);

            var noun = entity.Kind == LibraryCategoryKind.Category ? "category" : "sub-category";
            if (inUse > 0)
            {
                return Conflict(new
                {
                    Success = false,
                    Message = $"\"{entity.Name}\" is in use by {inUse} library item{(inUse == 1 ? "" : "s")}, so it cannot be deleted. Deactivate it instead."
                });
            }

            if (entity.Kind == LibraryCategoryKind.Category)
            {
                var children = await _db.LibraryCategories.CountAsync(c => c.ParentCategoryId == entity.Id);
                if (children > 0)
                {
                    return Conflict(new
                    {
                        Success = false,
                        Message = $"\"{entity.Name}\" has {children} sub-categor{(children == 1 ? "y" : "ies")}. Delete or move them first, or deactivate the category instead."
                    });
                }
            }

            _db.LibraryCategories.Remove(entity);
            await _db.SaveChangesAsync();
            return Ok(new { Success = true, Message = $"The {noun} \"{entity.Name}\" was deleted." });
        }

        /// <summary>
        /// Categories (SortOrder, Name) with their sub-categories. Without includeInactive, inactive
        /// rows and the sub-categories of inactive categories are left out.
        /// </summary>
        private async Task<List<LibraryCategoryDto>> LoadCategoryTreeAsync(bool includeInactive, bool withUsage)
        {
            var all = await _db.LibraryCategories
                .AsNoTracking()
                .Where(c => includeInactive || c.IsActive)
                .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                .ToListAsync();

            Dictionary<string, int> catUse = new(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, int> subUse = new(StringComparer.OrdinalIgnoreCase);
            if (withUsage)
            {
                var used = await _db.LibraryContents
                    .AsNoTracking()
                    .Where(c => !c.IsDeleted)
                    .Select(c => new { c.Category, c.SubCategory })
                    .ToListAsync();
                foreach (var g in used.Where(u => !string.IsNullOrWhiteSpace(u.Category)).GroupBy(u => u.Category!.Trim(), StringComparer.OrdinalIgnoreCase))
                    catUse[g.Key] = g.Count();
                foreach (var g in used.Where(u => !string.IsNullOrWhiteSpace(u.SubCategory)).GroupBy(u => u.SubCategory!.Trim(), StringComparer.OrdinalIgnoreCase))
                    subUse[g.Key] = g.Count();
            }

            var categories = all
                .Where(c => c.Kind == LibraryCategoryKind.Category)
                .Select(c => MapCategory(c, null, catUse.GetValueOrDefault(c.Name)))
                .ToList();
            var byId = categories.ToDictionary(c => c.Id);

            foreach (var sub in all.Where(c => c.Kind == LibraryCategoryKind.SubCategory))
            {
                if (sub.ParentCategoryId is int pid && byId.TryGetValue(pid, out var parent))
                    parent.SubCategories.Add(MapCategory(sub, parent.Name, subUse.GetValueOrDefault(sub.Name)));
            }

            return categories;
        }

        private async Task<LibraryCategoryDto?> LoadCategoryDtoAsync(int id)
        {
            var entity = await _db.LibraryCategories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (entity is null) return null;

            var lower = entity.Name.ToLower();
            var usage = entity.Kind == LibraryCategoryKind.Category
                ? await _db.LibraryContents.CountAsync(c => !c.IsDeleted && c.Category != null && c.Category.ToLower() == lower)
                : await _db.LibraryContents.CountAsync(c => !c.IsDeleted && c.SubCategory != null && c.SubCategory.ToLower() == lower);

            var dto = MapCategory(entity, entity.ParentCategory?.Name, usage);
            if (entity.Kind == LibraryCategoryKind.Category)
            {
                dto.SubCategories = (await _db.LibraryCategories
                        .AsNoTracking()
                        .Where(c => c.ParentCategoryId == entity.Id)
                        .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
                        .ToListAsync())
                    .Select(c => MapCategory(c, entity.Name, 0))
                    .ToList();
            }
            return dto;
        }

        private static LibraryCategoryDto MapCategory(LibraryCategory c, string? parentName, int usage) => new()
        {
            Id = c.Id,
            Kind = c.Kind,
            Name = c.Name,
            ParentCategoryId = c.ParentCategoryId,
            ParentName = parentName,
            SortOrder = c.SortOrder,
            IsActive = c.IsActive,
            UsageCount = usage,
            UpdatedBy = c.UpdatedBy,
            UpdatedAt = c.UpdatedAt
        };

        private static (string? Name, string? Error) CleanCategoryName(string? raw, LibraryCategoryKind kind)
        {
            var noun = kind == LibraryCategoryKind.Category ? "Category" : "Sub-category";
            var name = string.Join(' ', (raw ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (name.Length == 0) return (null, $"{noun} name is required.");
            if (name.Length > MaxCategoryNameLength) return (null, $"{noun} name must be {MaxCategoryNameLength} characters or less.");
            return (name, null);
        }

        private async Task<string?> ValidateParentAsync(int? parentId)
        {
            if (parentId is null) return "Choose the category this sub-category belongs to.";
            var ok = await _db.LibraryCategories.AnyAsync(c => c.Id == parentId && c.Kind == LibraryCategoryKind.Category);
            return ok ? null : "The chosen parent category does not exist.";
        }

        private async Task<string?> FindCategoryClashAsync(LibraryCategoryKind kind, string name, int? exceptId)
        {
            var lower = name.ToLower();
            var clash = await _db.LibraryCategories
                .AsNoTracking()
                .Where(c => c.Kind == kind && c.Name.ToLower() == lower && (exceptId == null || c.Id != exceptId))
                .Select(c => new { c.Name, c.IsActive })
                .FirstOrDefaultAsync();
            if (clash is null) return null;

            var noun = kind == LibraryCategoryKind.Category ? "category" : "sub-category";
            return clash.IsActive
                ? $"A {noun} named \"{clash.Name}\" already exists."
                : $"A {noun} named \"{clash.Name}\" already exists but is inactive. Reactivate it instead.";
        }
    }
}
