using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SPIC.Core.Interfaces;
using System.Security.Claims;

namespace SpicAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExcelBulkUploadController : ControllerBase
    {
        private readonly IExcelBulkUploadService _uploadService;

        public ExcelBulkUploadController(IExcelBulkUploadService uploadService)
        {
            _uploadService = uploadService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> Import(IFormFile file, [FromForm] string categoryId)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "No file uploaded" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls" && ext != ".csv")
                return BadRequest(new { Success = false, Message = "Only Excel (.xlsx/.xls) and CSV (.csv) files are supported" });

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";
            using var stream = file.OpenReadStream();

            var result = await _uploadService.ImportAsync(stream, currentUserId, ext, categoryId);

            if (result.Success)
            {
                return Ok(result);
            }
            return BadRequest(result);
        }
    }
}
