namespace ApiProject.Models;

public class InstructorSchedule
{
    public int Id { get; set; }
    public int InstructorId { get; set; }
    public int DayOfWeek { get; set; } // 1=Pazartesi, 2=Salı, ..., 5=Cuma
    public TimeSpan StartTime { get; set; }
    public string CourseName { get; set; } = string.Empty;
    
    // Navigation property
    public User? Instructor { get; set; }
}

