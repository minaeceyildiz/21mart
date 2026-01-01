using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public class ScheduleService
{
    private readonly AppDbContext _context;

    public ScheduleService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<ScheduleSlotResponseDto>> GetScheduleByInstructorEmailAsync(string email)
    {
        var instructor = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == email.ToLower().Trim() && u.Role == UserRole.Teacher);

        if (instructor == null)
            return new List<ScheduleSlotResponseDto>();

        var schedules = await _context.InstructorSchedules
            .Where(s => s.InstructorId == instructor.Id)
            .ToListAsync();

        var result = new List<ScheduleSlotResponseDto>();
        var dayMap = new Dictionary<int, string>
        {
            { 1, "Pzt" },
            { 2, "Sal" },
            { 3, "Çar" },
            { 4, "Per" },
            { 5, "Cum" }
        };

        foreach (var schedule in schedules)
        {
            var dayName = dayMap.GetValueOrDefault(schedule.DayOfWeek, "");
            var timeStr = schedule.StartTime.ToString(@"hh\.mm");
            var endTime = schedule.StartTime.Add(TimeSpan.FromMinutes(50));
            var endTimeStr = endTime.ToString(@"hh\.mm");
            var slot = $"{dayName}-{timeStr}-{endTimeStr}";

            result.Add(new ScheduleSlotResponseDto
            {
                Id = schedule.Id,
                DayOfWeek = schedule.DayOfWeek,
                StartTime = schedule.StartTime.ToString(@"hh\:mm"),
                CourseName = schedule.CourseName,
                Slot = slot
            });
        }

        return result;
    }

    public async Task SaveScheduleAsync(int instructorId, SaveScheduleDto dto)
    {
        // Önce mevcut schedule'ları sil
        var existingSchedules = await _context.InstructorSchedules
            .Where(s => s.InstructorId == instructorId)
            .ToListAsync();
        _context.InstructorSchedules.RemoveRange(existingSchedules);

        // Yeni schedule'ları ekle
        var dayMap = new Dictionary<string, int>
        {
            { "Pzt", 1 },
            { "Sal", 2 },
            { "Çar", 3 },
            { "Per", 4 },
            { "Cum", 5 }
        };

        var newSchedules = new List<InstructorSchedule>();

        foreach (var slotDto in dto.Slots)
        {
            // Slot formatı: "Pzt-09.00-09.50" veya "Pzt-09.00-09.50" (nokta ile)
            var parts = slotDto.Slot.Split('-');
            if (parts.Length >= 2)
            {
                var dayName = parts[0];
                // "09.00" formatını "09:00" formatına çevir
                var timeStr = parts[1].Replace(".", ":");
                
                // Eğer "09:00" formatında değilse, tekrar dene
                if (!TimeSpan.TryParse(timeStr, out var startTime))
                {
                    // "09.00" formatını direkt parse et
                    var timeParts = parts[1].Split('.');
                    if (timeParts.Length == 2 && int.TryParse(timeParts[0], out var hours) && int.TryParse(timeParts[1], out var minutes))
                    {
                        startTime = new TimeSpan(hours, minutes, 0);
                    }
                    else
                    {
                        continue; // Parse edilemezse atla
                    }
                }

                if (dayMap.TryGetValue(dayName, out var dayOfWeek))
                {
                    newSchedules.Add(new InstructorSchedule
                    {
                        InstructorId = instructorId,
                        DayOfWeek = dayOfWeek,
                        StartTime = startTime,
                        CourseName = slotDto.CourseName ?? ""
                    });
                }
            }
        }

        await _context.InstructorSchedules.AddRangeAsync(newSchedules);
        await _context.SaveChangesAsync();
    }
}

