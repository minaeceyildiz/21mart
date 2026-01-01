namespace ApiProject.Models.DTOs;

public class ScheduleSlotDto
{
    public string Slot { get; set; } = string.Empty; // Format: "Pzt-09.00-09.50"
    public string? CourseName { get; set; }
}

public class SaveScheduleDto
{
    public List<ScheduleSlotDto> Slots { get; set; } = new();
}

public class ScheduleSlotResponseDto
{
    public int Id { get; set; }
    public int DayOfWeek { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Slot { get; set; } = string.Empty; // Format: "Pzt-09.00-09.50"
}

