using ApiProject.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OccupancyController : ControllerBase
{

    [HttpPost("update")]
    public async Task<ActionResult> UpdateOccupancy([FromBody] OccupancyUpdateDto dto)
    {
        // OccupancyLogs tablosu veritabanında mevcut değil
        return StatusCode(503, new { message = "Bu özellik şu anda kullanılamıyor. OccupancyLogs tablosu veritabanında mevcut değil." });
    }

    [HttpGet("{zoneName}")]
    public async Task<ActionResult> GetOccupancy(string zoneName)
    {
        // OccupancyLogs tablosu veritabanında mevcut değil
        return StatusCode(503, new { message = "Bu özellik şu anda kullanılamıyor. OccupancyLogs tablosu veritabanında mevcut değil." });
    }
}

