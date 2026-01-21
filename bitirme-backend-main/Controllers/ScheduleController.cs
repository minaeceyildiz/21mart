using ApiProject.Models.DTOs;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ScheduleController : ControllerBase
{
    private readonly ScheduleService _scheduleService;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(ScheduleService scheduleService, ILogger<ScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _logger = logger;
    }

    [HttpGet("my-schedule")]
    public async Task<ActionResult<List<ScheduleSlotResponseDto>>> GetMySchedule()
    {
        try
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var userRole = GetCurrentUserRole();
            if (!string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Forbid("Bu işlem sadece öğretmenler için geçerlidir.");

            var schedule = await _scheduleService.GetScheduleByInstructorEmailAsync(userEmail);
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ders programı getirilirken hata oluştu");
            return StatusCode(500, "Ders programı getirilirken bir hata oluştu");
        }
    }

    [HttpGet("instructor/{id}")]
    [Authorize] // Öğrenci ve öğretmen erişebilir
    public async Task<ActionResult<List<ScheduleSlotResponseDto>>> GetScheduleByInstructorId(int id)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleByInstructorIdAsync(id);
            return Ok(schedule);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Öğretmen programı getirilirken hata oluştu. ID: {Id}", id);
            return StatusCode(500, "Ders programı getirilirken bir hata oluştu");
        }
    }

    [HttpPost("save")]
    public async Task<ActionResult> SaveSchedule([FromBody] SaveScheduleDto dto)
    {
        try
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var userRole = GetCurrentUserRole();
            if (!string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Forbid("Bu işlem sadece öğretmenler için geçerlidir.");

            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized("Kullanıcı ID bulunamadı");

            await _scheduleService.SaveScheduleAsync(userId.Value, dto);
            return Ok(new { message = "Ders programı başarıyla kaydedildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ders programı kaydedilirken hata oluştu");
            return StatusCode(500, "Ders programı kaydedilirken bir hata oluştu");
        }
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
    }
}

