using System.ComponentModel.DataAnnotations;
using ApiProject.Models;

namespace ApiProject.Models.DTOs;

public class AppointmentUpdateDto
{
    // Randevu Bilgileri (güncelleme için opsiyonel)
    public DateTime? Date { get; set; }
    public TimeSpan? Time { get; set; }
    
    [MaxLength(200)]
    public string? Subject { get; set; }

    // Durum (Hoca için)
    public AppointmentStatus? Status { get; set; }
    
    // Red nedeni (Rejected durumunda)
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

