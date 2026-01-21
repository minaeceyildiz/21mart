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
    public async Task<List<ScheduleSlotResponseDto>> GetScheduleByInstructorIdAsync(int instructorId)
    {
        var schedules = await _context.InstructorSchedules
            .Where(s => s.InstructorId == instructorId)
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
            var endTime = schedule.StartTime.Add(TimeSpan.FromMinutes(50)); // Ders saati 50 dk varsayımı
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

    public async Task<bool> IsTimeSlotAvailableAsync(int instructorId, DateTime date, TimeSpan time)
    {
        // Tarihin haftanın hangi günü olduğunu bul (1=Pazartesi, ... 5=Cuma)
        // DayOfWeek enum: Sunday=0, Monday=1, ... Friday=5, Saturday=6
        var dayOfWeek = (int)date.DayOfWeek;
        
        // Pazar(0) veya Cumartesi(6) ise direkt false dön
        if (dayOfWeek == 0 || dayOfWeek == 6) return false;

        // InstructorSchedule'da bu gün ve saatte kayıt var mı?
        // Çakışma kontrolü yapmamız lazım. Dersler genelde 50 dk, randevular 30 dk.
        // Eğer 09:00'da ders varsa, 09:00-09:50 doludur.
        // Bu durumda 09:00 ve 09:30 randevuları alınamaz.
        
        var daySchedule = await _context.InstructorSchedules
            .Where(s => s.InstructorId == instructorId && s.DayOfWeek == dayOfWeek)
            .ToListAsync();

        // Appointment aralığı: [time, time + 30dk]
        var appointmentStart = time;
        var appointmentEnd = time.Add(TimeSpan.FromMinutes(30));

        foreach (var course in daySchedule)
        {
            // Course aralığı: [StartTime, StartTime + 50dk]
            var courseStart = course.StartTime;
            var courseEnd = course.StartTime.Add(TimeSpan.FromMinutes(50));

            // Çakışma kuralı: (StartA < EndB) ve (EndA > StartB)
            if (appointmentStart < courseEnd && appointmentEnd > courseStart)
            {
                // Çakışma var, müsait değil
                return false;
            }
        }

        // Çakışma yok, müsait
        return true;
    }
}

