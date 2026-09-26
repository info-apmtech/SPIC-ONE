using Microsoft.AspNetCore.Mvc;
using SPIC.Core.DTOs;

[ApiController]
[Route("api/[controller]")]
public class ProgramTypeController : ControllerBase
{
    [HttpGet("all")]
    public IActionResult GetAll()
    {
        return Ok(new List<ProgramTypeDto>());
    }
}
