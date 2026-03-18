using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ApiProject.Models.DTOs;

public class AppointmentCreateDto
{
    // Öğrenci ID (JWT token'dan alınacak, opsiyonel)
    [JsonPropertyName("studentId")]
    public int? StudentId { get; set; }

    // Öğretmen ID veya adı/email'i
    [JsonPropertyName("teacherId")]
    public int? TeacherId { get; set; }
    
    [JsonPropertyName("teacherName")]
    public string? TeacherName { get; set; }
    
    [JsonPropertyName("teacherEmail")]
    public string? TeacherEmail { get; set; }

    [Required(ErrorMessage = "Randevu tarihi gereklidir")]
    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Randevu saati gereklidir")]
    [JsonPropertyName("time")]
    public string TimeString { get; set; } = string.Empty;

    [JsonIgnore]
    public TimeSpan Time
    {
        get
        {
            if (TimeSpan.TryParse(TimeString, out var timeSpan))
                return timeSpan;
            
            // "HH:mm" formatını destekle (örn: "14:30")
            if (TimeString.Contains(":") && TimeString.Split(':').Length == 2)
            {
                var parts = TimeString.Split(':');
                if (int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
                    return new TimeSpan(hours, minutes, 0);
            }
            
            return TimeSpan.Zero;
        }
    }

    [Required(ErrorMessage = "Ders/Konu gereklidir")]
    [MaxLength(200)]
    [JsonPropertyName("subject")]
    public string Subject { get; set; } = string.Empty;

    [JsonPropertyName("requestReason")]
    public string? RequestReason { get; set; }
}

