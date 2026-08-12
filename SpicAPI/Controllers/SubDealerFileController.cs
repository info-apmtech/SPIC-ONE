using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spic.Infrastructure.Data;
using SPIC.Core.DTOs;

namespace SPIC.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubDealerFileController : ControllerBase
{
	private const long MaxFileSize = 5 * 1024 * 1024;

	private readonly IWebHostEnvironment _environment;
	private readonly AppDbContext _db;

	public SubDealerFileController(
		IWebHostEnvironment environment,
		AppDbContext db)
	{
		_environment = environment;
		_db = db;
	}

	[HttpPost("upload/{subDealerId:int}/GST")]
	[RequestSizeLimit(MaxFileSize)]
	public async Task<ActionResult<SubDealerFileUploadResponse>> UploadGst(
		int subDealerId,
		IFormFile file,
		CancellationToken cancellationToken)
	{
		if (file == null || file.Length == 0)
			return BadRequest("File is required.");

		if (file.Length > MaxFileSize)
			return BadRequest("File size must be less than 5 MB.");

		if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
			return BadRequest("Only PDF files are allowed for GST Certificate.");

		var exists = await _db.SubDealerRegistrations
			.AsNoTracking()
			.AnyAsync(x => x.Id == subDealerId, cancellationToken);

		if (!exists)
			return NotFound($"Sub Dealer with Id {subDealerId} was not found.");

		var webRoot = _environment.WebRootPath;
		if (string.IsNullOrWhiteSpace(webRoot))
			webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

		var relativeDirectory = Path.Combine("uploads", "subdealers", subDealerId.ToString(), "gst");
		var physicalDirectory = Path.Combine(webRoot, relativeDirectory);
		Directory.CreateDirectory(physicalDirectory);

		var safeFileName = $"{Guid.NewGuid():N}.pdf";
		var physicalPath = Path.Combine(physicalDirectory, safeFileName);

		await using (var output = System.IO.File.Create(physicalPath))
		{
			await file.CopyToAsync(output, cancellationToken);
		}

		var relativePath = "/" + Path
			.Combine(relativeDirectory, safeFileName)
			.Replace("\\", "/");

		return Ok(new SubDealerFileUploadResponse
		{
			FilePath = relativePath
		});
	}

	[HttpDelete("delete")]
	public IActionResult Delete([FromQuery] string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return BadRequest("filePath is required.");

		var normalizedRelative = filePath.Trim().Replace("\\", "/").TrimStart('/');

		if (!normalizedRelative.StartsWith("uploads/subdealers/", StringComparison.OrdinalIgnoreCase))
			return BadRequest("Invalid Sub Dealer file path.");

		var webRoot = _environment.WebRootPath;
		if (string.IsNullOrWhiteSpace(webRoot))
			webRoot = Path.Combine(_environment.ContentRootPath, "wwwroot");

		var allowedRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "subdealers"));
		var requestedFile = Path.GetFullPath(Path.Combine(webRoot, normalizedRelative));

		if (!requestedFile.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
			return BadRequest("Invalid file path.");

		if (System.IO.File.Exists(requestedFile))
			System.IO.File.Delete(requestedFile);

		return NoContent();
	}
}