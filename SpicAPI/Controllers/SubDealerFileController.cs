using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;

namespace SPIC.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class SubDealerFileController : ControllerBase
{
    private const long MaxFileSize = 5 * 1024 * 1024;
    private readonly IWebHostEnvironment _environment;
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubDealerFileController> _logger;

    public SubDealerFileController(IWebHostEnvironment environment, AppDbContext db,
        IConfiguration configuration, ILogger<SubDealerFileController> logger)
    {
        _environment = environment;
        _db = db;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("upload/{subDealerId:int}/GST")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxFileSize + 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize + 1024 * 1024)]
    public async Task<ActionResult<SubDealerFileUploadResponse>> UploadGst(
        int subDealerId, IFormFile file, CancellationToken cancellationToken)
    {
        if (!SubDealerRegistrationController.CanMaintain(User)) return Forbid();
        if (string.IsNullOrWhiteSpace(SubDealerRegistrationController.Actor(User))) return Unauthorized();
        if (file == null || file.Length == 0) return BadRequest("File is required.");
        if (file.Length > MaxFileSize) return BadRequest("GST Certificate must be 5 MB or smaller.");
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only PDF files are allowed for GST Certificate.");

        var entity = await SubDealerRegistrationController.Scope(_db.SubDealerRegistrations.AsNoTracking(), _db, User)
            .SingleOrDefaultAsync(x => x.Id == subDealerId, cancellationToken);
        if (entity == null) return NotFound("Sub Dealer not found in your permitted scope.");
        if (SubDealerFormModel.CodeError(entity.SubDealerCode) != null)
            return BadRequest("Repair the invalid master code before uploading profile documents.");

        var fileName = $"{Guid.NewGuid():N}.pdf";
        string? temporary = null;
        try
        {
            var target = NewPhysicalPath(_environment, _configuration, subDealerId, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            temporary = target + ".uploading";
            await using var input = file.OpenReadStream();
            var header = new byte[5];
            var headerLength = 0;
            while (headerLength < header.Length)
            {
                var read = await input.ReadAsync(header.AsMemory(headerLength), cancellationToken);
                if (read == 0) break;
                headerLength += read;
            }
            if (headerLength != 5 || Encoding.ASCII.GetString(header) != "%PDF-")
                return BadRequest("The selected file is not a recognized PDF. A .pdf extension alone is not sufficient.");

            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                81920, FileOptions.Asynchronous))
            {
                await output.WriteAsync(header.AsMemory(), cancellationToken);
                long total = header.Length;
                var buffer = new byte[81920];
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(), cancellationToken)) > 0)
                {
                    total += read;
                    if (total > MaxFileSize) throw new InvalidDataException("GST Certificate exceeds 5 MB.");
                    await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }
                await output.FlushAsync(cancellationToken);
            }
            System.IO.File.Move(temporary, target); // unique application-generated file, never overwrite another PDF
            temporary = null;

            // Staging only: do not update profile/audit or make an untouched master list-visible here.
            // The profile PUT validates this path and attaches it in its successful database transaction.
            return Ok(new SubDealerFileUploadResponse
            {
                FilePath = LogicalPath(subDealerId, fileName)
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (InvalidDataException ex) { return BadRequest(ex.Message); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GST upload failed for Sub Dealer {SubDealerId}", subDealerId);
            return StatusCode(500, "GST upload failed. No certificate was attached to the profile. Check storage configuration and permissions.");
        }
        finally
        {
            if (temporary != null)
            {
                try { if (System.IO.File.Exists(temporary)) System.IO.File.Delete(temporary); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not remove incomplete GST upload"); }
            }
        }
    }

    [HttpGet("content/{subDealerId:int}/GST/{fileName}")]
    public async Task<IActionResult> DownloadGst(int subDealerId, string fileName, CancellationToken cancellationToken)
    {
        var logical = LogicalPath(subDealerId, fileName);
        if (!TryParse(logical, out _, out _)) return BadRequest("Invalid certificate path.");
        var entity = await SubDealerRegistrationController.Scope(_db.SubDealerRegistrations.AsNoTracking(), _db, User)
            .SingleOrDefaultAsync(x => x.Id == subDealerId, cancellationToken);
        if (entity == null) return NotFound();
        // Detached/unsubmitted files cannot be downloaded through the public API.
        if (!TryParse(entity.GSTFilePath, out var storedId, out var storedName) ||
            storedId != subDealerId || !string.Equals(storedName, fileName, StringComparison.Ordinal)) return NotFound();
        var physical = FindExisting(_environment, _configuration, entity.GSTFilePath, subDealerId);
        if (physical == null) return NotFound("The saved GST Certificate is unavailable. Re-upload the PDF.");
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Cache-Control"] = "no-store";
        return File(new FileStream(physical, FileMode.Open, FileAccess.Read, FileShare.Read),
            "application/pdf", $"SubDealer-{subDealerId}-GST.pdf", enableRangeProcessing: true);
    }

    [HttpDelete("delete")]
    public async Task<IActionResult> Delete([FromQuery] string filePath, CancellationToken cancellationToken)
    {
        if (!SubDealerRegistrationController.CanMaintain(User)) return Forbid();
        if (!TryParse(filePath, out var id, out _)) return BadRequest("Invalid Sub Dealer file path.");
        if (!await SubDealerRegistrationController.Scope(_db.SubDealerRegistrations.AsNoTracking(), _db, User)
                .AnyAsync(x => x.Id == id, cancellationToken)) return NotFound();
        // Avoid deleting a physical file before a validated profile save. Old clients must refresh.
        return Conflict("Immediate physical deletion is disabled. Remove or replace the attachment in the form and submit. Detached files are retained for controlled cleanup and recovery.");
    }

    // Storage/path validation kept inside this original controller. No extra source file.
    internal static string Root(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["SubDealer:PrivateUploadRoot"];
        var root = Path.GetFullPath(string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(environment.ContentRootPath, "App_Data", "SubDealerFiles")
            : Path.IsPathRooted(configured) ? configured : Path.Combine(environment.ContentRootPath, configured));
        var webRoot = Path.GetFullPath(WebRoot(environment));
        if (IsWithin(webRoot, root) || string.Equals(root, webRoot, Comparison))
            throw new InvalidOperationException("SubDealer:PrivateUploadRoot must be outside wwwroot.");
        return root;
    }

    private static string WebRoot(IWebHostEnvironment environment) => string.IsNullOrWhiteSpace(environment.WebRootPath)
        ? Path.Combine(environment.ContentRootPath, "wwwroot") : environment.WebRootPath;

    private static StringComparison Comparison => OperatingSystem.IsWindows()
        ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    private static bool IsWithin(string root, string file) =>
        Path.GetFullPath(file).StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, Comparison);

    internal static bool TryParse(string? logicalPath, out int id, out string fileName)
    {
        id = 0;
        fileName = string.Empty;
        if (string.IsNullOrWhiteSpace(logicalPath) || logicalPath.Contains('\\') ||
            logicalPath.Contains('%') || logicalPath.Contains('?') || logicalPath.Contains('#')) return false;
        var parts = logicalPath.Trim().TrimStart('/').Split('/');
        if (parts.Length != 5 || parts[0] != "uploads" || parts[1] != "subdealers" || parts[3] != "gst" ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out id) || id <= 0) return false;
        fileName = parts[4];
        return fileName.Length == 36 && fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) &&
               Guid.TryParseExact(fileName[..^4], "N", out _);
    }

    internal static string LogicalPath(int id, string fileName) => $"/uploads/subdealers/{id.ToString(CultureInfo.InvariantCulture)}/gst/{fileName}";

    internal static string NewPhysicalPath(IWebHostEnvironment environment, IConfiguration configuration, int id, string fileName)
    {
        if (!TryParse(LogicalPath(id, fileName), out _, out _)) throw new ArgumentException("Invalid certificate identity.");
        var root = Root(environment, configuration);
        var path = Path.Combine(root, id.ToString(CultureInfo.InvariantCulture), "gst", fileName);
        if (!IsWithin(root, path)) throw new ArgumentException("Invalid certificate path.");
        RejectLinkedDirectories(root, Path.GetDirectoryName(path)!);
        return path;
    }

    internal static string? FindExisting(IWebHostEnvironment environment, IConfiguration configuration, string? logicalPath, int expectedId)
    {
        if (!TryParse(logicalPath, out var id, out var fileName) || id != expectedId) return null;
        var primary = NewPhysicalPath(environment, configuration, id, fileName);
        if (System.IO.File.Exists(primary) && !IsLink(primary)) return primary;

        // Read-only compatibility for files saved by the old controller.
        // Block the legacy public uploads route BEFORE static-file serving (see README).
        var legacyRoot = Path.Combine(WebRoot(environment), "uploads", "subdealers");
        var legacy = Path.GetFullPath(Path.Combine(legacyRoot, id.ToString(CultureInfo.InvariantCulture), "gst", fileName));
        if (!IsWithin(legacyRoot, legacy)) return null;
        RejectLinkedDirectories(legacyRoot, Path.GetDirectoryName(legacy)!);
        return System.IO.File.Exists(legacy) && !IsLink(legacy) ? legacy : null;
    }

    private static bool IsLink(string path) => (System.IO.File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
    private static void RejectLinkedDirectories(string root, string directory)
    {
        DirectoryInfo? current = new DirectoryInfo(directory);
        var fullRoot = Path.GetFullPath(root);
        while (current != null)
        {
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Symbolic-link directories are not allowed in the certificate storage path.");
            if (string.Equals(current.FullName, fullRoot, Comparison)) break;
            current = current.Parent;
        }
    }
}
