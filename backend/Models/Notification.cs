namespace ApiProject.Models;

public class Notification
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public string RecipientEmail { get; set; } = string.Empty;
    public int? RecipientUserId { get; set; } // 🔥 KRİTİK: Bildirim alacak kullanıcının ID'si
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? ReadAt { get; set; }
    public int? AppointmentId { get; set; } // Randevu ile ilişki
}

public enum NotificationType
{
    AppointmentCreated = 0,
    AppointmentConfirmed = 1,
    AppointmentCancelled = 2,
    AppointmentReminder = 3,
    General = 4
}

