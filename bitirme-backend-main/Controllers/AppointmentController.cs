using ApiProject.Models.DTOs;
using ApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(IAppointmentService appointmentService, ILogger<AppointmentController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm randevuları getirir (sadece admin)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<AppointmentResponseDto>>> GetAllAppointments()
    {
        try
        {
            var appointments = await _appointmentService.GetAllAppointmentsAsync();
            var response = appointments.Select(a => MapToDto(a)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevular getirilirken hata oluştu");
            return StatusCode(500, "Randevular getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// ID'ye göre randevu getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<AppointmentResponseDto>> GetAppointmentById(int id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            // Kullanıcının kendi randevusu olduğunu kontrol et
            var userEmail = GetCurrentUserEmail();
            var userRole = GetCurrentUserRole();
            if (!string.IsNullOrEmpty(userEmail) && userRole != "Admin")
            {
                if (appointment.Student?.Email.ToLower() != userEmail.ToLower() && 
                    appointment.Teacher?.Email.ToLower() != userEmail.ToLower())
                {
                    return Forbid("Bu randevuya erişim yetkiniz yok");
                }
            }

            return Ok(MapToDto(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu getirilirken hata oluştu");
            return StatusCode(500, "Randevu getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Giriş yapmış kullanıcının randevularını getirir (öğrenci veya öğretmen)
    /// </summary>
    [HttpGet("my-appointments")]
    public async Task<ActionResult<List<AppointmentResponseDto>>> GetMyAppointments()
    {
        try
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var userRole = GetCurrentUserRole();
            List<Models.Appointment> appointments;

            // Case-insensitive role kontrolü
            if (string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase))
            {
                appointments = await _appointmentService.GetAppointmentsByTeacherEmailAsync(userEmail);
                _logger.LogInformation("Hoca randevuları getiriliyor. Email: {Email}, Role: {Role}, Bulunan randevu sayısı: {Count}", userEmail, userRole, appointments.Count);
            }
            else
            {
                appointments = await _appointmentService.GetAppointmentsByStudentEmailAsync(userEmail);
                _logger.LogInformation("Öğrenci randevuları getiriliyor. Email: {Email}, Role: {Role}, Bulunan randevu sayısı: {Count}", userEmail, userRole, appointments.Count);
            }

            var response = appointments.Select(a => MapToDto(a)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevular getirilirken hata oluştu");
            return StatusCode(500, "Randevular getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Yeni randevu oluşturur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AppointmentResponseDto>> CreateAppointment([FromBody] object requestBody)
    {
        try
        {
            AppointmentCreateDto dto;
            
            // Frontend'in gönderdiği formatı kontrol et
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
            
            // Eğer "dto" field'ı varsa wrapper kullan
            if (jsonDoc.RootElement.TryGetProperty("dto", out var dtoElement))
            {
                dto = System.Text.Json.JsonSerializer.Deserialize<AppointmentCreateDto>(dtoElement.GetRawText()) 
                    ?? throw new ArgumentException("dto field'ı geçersiz format");
            }
            else
            {
                // Direkt DTO formatında gönderilmiş
                dto = System.Text.Json.JsonSerializer.Deserialize<AppointmentCreateDto>(json) 
                    ?? throw new ArgumentException("Request body geçersiz format");
            }

            // Öğretmen bilgisi kontrolü - daha açıklayıcı hata mesajı
            if (!dto.TeacherId.HasValue && string.IsNullOrWhiteSpace(dto.TeacherName) && string.IsNullOrWhiteSpace(dto.TeacherEmail))
            {
                return BadRequest(new { 
                    message = "Öğretmen bilgisi gereklidir. Lütfen teacherId, teacherName veya teacherEmail alanlarından birini gönderin.",
                    receivedData = new { 
                        date = dto.Date, 
                        time = dto.TimeString, 
                        subject = dto.Subject,
                        teacherId = dto.TeacherId,
                        teacherName = dto.TeacherName,
                        teacherEmail = dto.TeacherEmail
                    }
                });
            }

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // JWT token'dan kullanıcı ID'sini al (öğrenci için)
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var appointment = await _appointmentService.CreateAppointmentAsync(dto, currentUserId);
            return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, MapToDto(appointment));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Randevu oluşturulurken validasyon hatası: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Inner exception'ı logla - gerçek hatayı görmek için
            var innerException = ex.InnerException?.Message ?? ex.Message;
            var fullException = ex.ToString();
            
            _logger.LogError(ex, "Randevu oluşturulurken hata oluştu. Inner Exception: {InnerException}, Full: {FullException}", 
                innerException, fullException);
            
            // Gerçek hatayı frontend'e gönder
            return BadRequest(new { 
                message = $"Randevu oluşturulurken bir hata oluştu: {innerException}",
                innerException = innerException,
                fullException = fullException
            });
        }
    }

    /// <summary>
    /// Randevu günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<AppointmentResponseDto>> UpdateAppointment(int id, [FromBody] AppointmentUpdateDto dto)
    {
        try
        {
            var appointment = await _appointmentService.UpdateAppointmentAsync(id, dto);
            if (appointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            // Kullanıcının kendi randevusu olduğunu kontrol et
            var userEmail = GetCurrentUserEmail();
            var userRole = GetCurrentUserRole();
            if (!string.IsNullOrEmpty(userEmail) && userRole != "Admin")
            {
                if (appointment.Teacher?.Email.ToLower() != userEmail.ToLower() && 
                    appointment.Student?.Email.ToLower() != userEmail.ToLower())
                {
                    return Forbid("Bu randevuyu güncelleme yetkiniz yok");
                }
            }

            return Ok(MapToDto(appointment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu güncellenirken hata oluştu");
            return StatusCode(500, "Randevu güncellenirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Randevu siler
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAppointment(int id)
    {
        try
        {
            var result = await _appointmentService.DeleteAppointmentAsync(id);
            if (!result)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu silinirken hata oluştu");
            return StatusCode(500, "Randevu silinirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Hocanın bekleyen randevu taleplerini getirir
    /// </summary>
    [HttpGet("pending-requests")]
    public async Task<ActionResult<List<AppointmentResponseDto>>> GetPendingRequests()
    {
        try
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var userRole = GetCurrentUserRole();
            _logger.LogInformation("GetPendingRequests çağrıldı. Email: {Email}, Role: {Role}", userEmail, userRole);
            
            if (!string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("GetPendingRequests - Yetkisiz erişim. Email: {Email}, Role: {Role}", userEmail, userRole);
                return Forbid("Bu işlem sadece öğretmenler için geçerlidir.");
            }

            var appointments = await _appointmentService.GetPendingAppointmentsByTeacherEmailAsync(userEmail);
            _logger.LogInformation("GetPendingRequests - Bulunan randevu sayısı: {Count} (Email: {Email})", appointments.Count, userEmail);
            
            var response = appointments.Select(a => MapToDto(a)).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bekleyen randevu talepleri getirilirken hata oluştu");
            return StatusCode(500, "Bekleyen randevu talepleri getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Hoca randevu talebini onaylar
    /// </summary>
    [HttpPost("{id}/approve")]
    public async Task<ActionResult<AppointmentResponseDto>> ApproveAppointment(int id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            // Sadece hoca kendi randevularını onaylayabilir
            var userEmail = GetCurrentUserEmail();
            var userRole = GetCurrentUserRole();
            
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            if (!string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Forbid("Bu işlem sadece öğretmenler için geçerlidir.");

            if (appointment.Teacher?.Email.ToLower() != userEmail.ToLower())
                return Forbid("Bu randevuyu onaylama yetkiniz yok. Sadece kendi randevularınızı onaylayabilirsiniz.");

            var updatedAppointment = await _appointmentService.ApproveAppointmentAsync(id);
            if (updatedAppointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            return Ok(MapToDto(updatedAppointment));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Randevu onaylanırken validasyon hatası: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu onaylanırken hata oluştu");
            return StatusCode(500, "Randevu onaylanırken bir hata oluştu");
        }
    }

    /// <summary>
    /// Hoca randevu talebini reddeder
    /// </summary>
    [HttpPost("{id}/reject")]
    public async Task<ActionResult<AppointmentResponseDto>> RejectAppointment(int id, [FromBody] RejectAppointmentDto? dto = null)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
            if (appointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            // Sadece hoca kendi randevularını reddedebilir
            var userEmail = GetCurrentUserEmail();
            var userRole = GetCurrentUserRole();
            
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            if (!string.Equals(userRole, "Teacher", StringComparison.OrdinalIgnoreCase))
                return Forbid("Bu işlem sadece öğretmenler için geçerlidir.");

            if (appointment.Teacher?.Email.ToLower() != userEmail.ToLower())
                return Forbid("Bu randevuyu reddetme yetkiniz yok. Sadece kendi randevularınızı reddedebilirsiniz.");

            var rejectionReason = dto?.RejectionReason;
            var updatedAppointment = await _appointmentService.RejectAppointmentAsync(id, rejectionReason);
            if (updatedAppointment == null)
                return NotFound($"ID: {id} olan randevu bulunamadı");

            return Ok(MapToDto(updatedAppointment));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Randevu reddedilirken validasyon hatası: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Randevu reddedilirken hata oluştu");
            return StatusCode(500, "Randevu reddedilirken bir hata oluştu");
        }
    }

    private AppointmentResponseDto MapToDto(Models.Appointment appointment)
    {
        // Debug: RequestReason değerini logla
        _logger.LogInformation("MapToDto - Appointment ID: {Id}, RequestReason: '{RequestReason}'", 
            appointment.Id, appointment.RequestReason ?? "null");
        
        return new AppointmentResponseDto
        {
            Id = appointment.Id,
            StudentId = appointment.StudentId,
            StudentName = appointment.Student?.Name ?? "Bilinmiyor",
            StudentNo = appointment.Student?.StudentNo,
            TeacherId = appointment.TeacherId,
            TeacherName = appointment.Teacher?.Name ?? "Bilinmiyor",
            Date = appointment.Date,
            Time = appointment.Time,
            Subject = appointment.Subject,
            RequestReason = appointment.RequestReason ?? "other", // Öğrencinin yazdığı metin veya enum değeri
            Status = appointment.Status.ToString(),
            RejectionReason = appointment.RejectionReason,
            CreatedAt = appointment.CreatedAt,
            UpdatedAt = appointment.UpdatedAt
        };
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Debug endpoint - JWT token'dan user bilgilerini gösterir
    /// </summary>
    [HttpGet("debug")]
    public IActionResult Debug()
    {
        var id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        return Ok(new { 
            id, 
            email, 
            role, 
            name,
            allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
        });
    }
}

