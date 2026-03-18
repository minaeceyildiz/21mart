namespace ApiProject.Models.DTOs;

public class AppointmentCreateWrapperDto
{
    public AppointmentCreateDto? Dto { get; set; }
    
    // Eğer dto wrapper yoksa direkt field'ları da kabul et
    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }
    public DateTime? Date { get; set; }
    public string? Time { get; set; }
    public string? Subject { get; set; }
    
    // DTO'ya dönüştür
    public AppointmentCreateDto ToAppointmentCreateDto()
    {
        if (Dto != null)
            return Dto;
            
        return new AppointmentCreateDto
        {
            StudentId = StudentId ?? 0,
            TeacherId = TeacherId ?? 0,
            Date = Date ?? DateTime.Now,
            TimeString = Time ?? string.Empty,
            Subject = Subject ?? string.Empty
        };
    }
}

