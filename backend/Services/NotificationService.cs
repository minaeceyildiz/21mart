using ApiProject.Data;
using ApiProject.Hubs;
using ApiProject.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface INotificationService
{
    Task<Notification> CreateNotificationAsync(string title, string message, NotificationType type, string recipientEmail, int? recipientUserId = null, int? appointmentId = null);
    Task<Notification> SendNotificationAsync(string title, string message, NotificationType type, string recipientEmail, int? recipientUserId = null, int? appointmentId = null);
    Task<List<Notification>> GetNotificationsByEmailAsync(string email);
    Task<List<Notification>> GetNotificationsByUserIdAsync(int userId);
    Task<Notification?> MarkAsReadAsync(int notificationId);
}

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<Notification> CreateNotificationAsync(string title, string message, NotificationType type, string recipientEmail, int? recipientUserId = null, int? appointmentId = null)
    {
        // Eğer recipientUserId null ise, email'den user'ı bul
        if (!recipientUserId.HasValue)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == recipientEmail.ToLower());
            
            if (user != null)
            {
                recipientUserId = user.Id;
            }
            else
            {
                throw new ArgumentException($"RecipientUserId null ve email ile kullanıcı bulunamadı: {recipientEmail}");
            }
        }

        var notification = new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            RecipientEmail = recipientEmail.ToLower(),
            RecipientUserId = recipientUserId, // 🔥 KRİTİK: UserId set edilmeli
            AppointmentId = appointmentId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification;
    }

    public async Task<Notification> SendNotificationAsync(string title, string message, NotificationType type, string recipientEmail, int? recipientUserId = null, int? appointmentId = null)
    {
        // Önce veritabanına kaydet
        var notification = await CreateNotificationAsync(title, message, type, recipientEmail, recipientUserId, appointmentId);

        // SignalR ile canlı bildirim gönder
        // Email'e göre grup adını oluştur (hub'taki format ile aynı)
        var groupName = $"user_{recipientEmail.ToLower().Replace("@", "_at_").Replace(".", "_")}";

        // Grup içindeki tüm client'lara bildirim gönder
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveNotification", new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            type = notification.Type.ToString(),
            appointmentId = notification.AppointmentId,
            isRead = notification.IsRead,
            createdAt = notification.CreatedAt
        });

        return notification;
    }

    public async Task<List<Notification>> GetNotificationsByEmailAsync(string email)
    {
        return await _context.Notifications
            .Where(n => n.RecipientEmail.ToLower() == email.ToLower())
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Notification>> GetNotificationsByUserIdAsync(int userId)
    {
        return await _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<Notification?> MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return notification;
    }
}
