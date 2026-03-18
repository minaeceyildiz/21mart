using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiProject.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        // --- İLİŞKİLER ---
        
        // Öğrenci İlişkisi
        public int StudentId { get; set; }
        [JsonIgnore] // Döngüye girmesin diye
        public User? Student { get; set; }

        // Hoca İlişkisi
        public int TeacherId { get; set; }
        [JsonIgnore]
        public User? Teacher { get; set; }

        // --- DİĞER BİLGİLER ---
        public DateTime Date { get; set; }
        public TimeSpan Time { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string RequestReason { get; set; } = string.Empty;
        
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
        
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
    }
    
    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Completed
    }
}
