using ApiProject.Services;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Giriş yapmış kullanıcının bildirimlerini getirir
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<NotificationResponseDto>>> GetMyNotifications()
    {
        try
        {
            // 🔥 KRİTİK: UserId'ye göre çek (email yerine)
            var userId = GetCurrentUserId();
            List<Notification> notifications;

            if (userId.HasValue)
            {
                // UserId'ye göre bildirimleri çek
                notifications = await _notificationService.GetNotificationsByUserIdAsync(userId.Value);
                _logger.LogInformation("Bildirimler UserId'ye göre getiriliyor. UserId: {UserId}, Bulunan bildirim sayısı: {Count}", userId.Value, notifications.Count);
            }
            else
            {
                // Fallback: Email'den user bul
                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized("Kullanıcı bilgisi bulunamadı");

                notifications = await _notificationService.GetNotificationsByEmailAsync(userEmail);
                _logger.LogInformation("Bildirimler Email'e göre getiriliyor. Email: {Email}, Bulunan bildirim sayısı: {Count}", userEmail, notifications.Count);
            }

            var response = notifications.Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                AppointmentId = n.AppointmentId
            }).ToList();
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirimler getirilirken hata oluştu");
            return StatusCode(500, "Bildirimler getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Giriş yapmış kullanıcının okunmamış bildirimlerini getirir
    /// </summary>
    [HttpGet("unread")]
    public async Task<ActionResult<List<NotificationResponseDto>>> GetMyUnreadNotifications()
    {
        try
        {
            // 🔥 KRİTİK: UserId'ye göre çek (email yerine)
            var userId = GetCurrentUserId();
            List<Notification> notifications;

            if (userId.HasValue)
            {
                notifications = await _notificationService.GetNotificationsByUserIdAsync(userId.Value);
            }
            else
            {
                var userEmail = GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized("Kullanıcı bilgisi bulunamadı");

                notifications = await _notificationService.GetNotificationsByEmailAsync(userEmail);
            }

            var unreadNotifications = notifications.Where(n => !n.IsRead).Select(n => new NotificationResponseDto
            {
                Id = n.Id,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type.ToString(),
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                ReadAt = n.ReadAt,
                AppointmentId = n.AppointmentId
            }).ToList();
            return Ok(unreadNotifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Okunmamış bildirimler getirilirken hata oluştu");
            return StatusCode(500, "Okunmamış bildirimler getirilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Bildirimi okundu olarak işaretler (sadece kullanıcının kendi bildirimi)
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<ActionResult<NotificationResponseDto>> MarkAsRead(int id)
    {
        try
        {
            var userEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(userEmail))
                return Unauthorized("Kullanıcı bilgisi bulunamadı");

            var notification = await _notificationService.MarkAsReadAsync(id);
            if (notification == null)
                return NotFound($"ID: {id} olan bildirim bulunamadı");

            // Kullanıcının kendi bildirimi olduğunu kontrol et
            if (notification.RecipientEmail.ToLower() != userEmail.ToLower())
                return Forbid("Bu bildirim size ait değil");

            var response = new NotificationResponseDto
            {
                Id = notification.Id,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type.ToString(),
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt,
                ReadAt = notification.ReadAt,
                AppointmentId = notification.AppointmentId
            };
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirim okundu olarak işaretlenirken hata oluştu");
            return StatusCode(500, "Bildirim okundu olarak işaretlenirken bir hata oluştu");
        }
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
            return userId;
        return null;
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

