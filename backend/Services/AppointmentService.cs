using ApiProject.Models;
using ApiProject.Models.DTOs;
using ApiProject.Data;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface IAppointmentService
{
    Task<List<Appointment>> GetAllAppointmentsAsync();
    Task<Appointment?> GetAppointmentByIdAsync(int id);
    Task<Appointment> CreateAppointmentAsync(AppointmentCreateDto dto, int? currentUserId = null);
    Task<Appointment?> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto);
    Task<bool> DeleteAppointmentAsync(int id);
    Task<List<Appointment>> GetAppointmentsByStudentEmailAsync(string email);
    Task<List<Appointment>> GetAppointmentsByTeacherEmailAsync(string email);
    Task<List<Appointment>> GetPendingAppointmentsByTeacherEmailAsync(string email);
    Task<Appointment?> ApproveAppointmentAsync(int id);
    Task<Appointment?> RejectAppointmentAsync(int id, string? rejectionReason = null);
}

public class AppointmentService : IAppointmentService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly ScheduleService _scheduleService;

    public AppointmentService(AppDbContext context, INotificationService notificationService, ScheduleService scheduleService)
    {
        _context = context;
        _notificationService = notificationService;
        _scheduleService = scheduleService;
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        var appointments = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .ToListAsync();
        
        if (appointments.Count == 0)
            return appointments;
        
        // Status kolonu olmadığı için, responded_at ve rejection_reason kolonlarına göre status belirle
        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            await connection.OpenAsync();
        }
        try
        {
            foreach (var appointment in appointments)
            {
                var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
                appointment.Date = scheduledAt.Date;
                appointment.Time = scheduledAt.TimeOfDay;
                
                // rejection_reason kontrolü
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT \"rejection_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                var rejectionReasonValue = await checkCmd.ExecuteScalarAsync();
                
                if (!appointment.UpdatedAt.HasValue)
                {
                    appointment.Status = AppointmentStatus.Pending;
                }
                else if (rejectionReasonValue != null && !string.IsNullOrWhiteSpace(rejectionReasonValue.ToString()))
                {
                    appointment.Status = AppointmentStatus.Rejected;
                    appointment.RejectionReason = rejectionReasonValue.ToString();
                }
                else
                {
                    appointment.Status = AppointmentStatus.Approved;
                }
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
        
        return appointments.OrderByDescending(a => a.Date).ToList();
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);
        
        if (appointment != null)
        {
            // scheduled_at'ten Date ve Time'ı yükle
            var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
            appointment.Date = scheduledAt.Date;
            appointment.Time = scheduledAt.TimeOfDay;
            
            // rejection_reason kontrolü yaparak status belirle
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen)
            {
                await connection.OpenAsync();
            }
            try
            {
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT \"rejection_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                var rejectionReasonValue = await checkCmd.ExecuteScalarAsync();
                
                if (!appointment.UpdatedAt.HasValue)
                {
                    appointment.Status = AppointmentStatus.Pending;
                }
                else if (rejectionReasonValue != null && !string.IsNullOrWhiteSpace(rejectionReasonValue.ToString()))
                {
                    appointment.Status = AppointmentStatus.Rejected;
                    appointment.RejectionReason = rejectionReasonValue.ToString();
                }
                else
                {
                    appointment.Status = AppointmentStatus.Approved;
                }
            }
            finally
            {
                if (!wasOpen)
                {
                    await connection.CloseAsync();
                }
            }
        }
        
        return appointment;
    }

    public async Task<Appointment> CreateAppointmentAsync(AppointmentCreateDto dto, int? currentUserId = null)
    {
        // Öğrenci ID'sini belirle (dto'dan veya currentUserId'den)
        int studentId = dto.StudentId ?? currentUserId ?? throw new ArgumentException("Öğrenci ID gereklidir.");
        var student = await _context.Users.FindAsync(studentId);
        if (student == null)
            throw new ArgumentException($"Öğrenci bulunamadı. StudentId: {studentId}");

        // Öğretmen ID'sini belirle (dto'dan, adından veya email'inden)
        User? teacher = null;
        if (dto.TeacherId.HasValue && dto.TeacherId.Value > 0)
        {
            teacher = await _context.Users.FindAsync(dto.TeacherId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(dto.TeacherName))
        {
            var teacherNameLower = dto.TeacherName.ToLower().Trim();
            
            // Önce tam eşleşme dene (case-insensitive)
            teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Name.ToLower().Trim() == teacherNameLower && u.Role == UserRole.Teacher);
            
            // Tam eşleşme yoksa, isim içinde arama yap (partial match)
            if (teacher == null)
            {
                teacher = await _context.Users
                    .FirstOrDefaultAsync(u => 
                        u.Name.ToLower().Contains(teacherNameLower) && 
                        u.Role == UserRole.Teacher);
            }
            
            // Hala bulunamadıysa, ilk kelimeyi eşleştir (örn: "Mehmet Dikmen" -> "Mehmet")
            if (teacher == null)
            {
                var firstWord = teacherNameLower.Split(' ').FirstOrDefault();
                if (!string.IsNullOrEmpty(firstWord))
                {
                    teacher = await _context.Users
                        .FirstOrDefaultAsync(u => 
                            u.Name.ToLower().Trim().StartsWith(firstWord) && 
                            u.Role == UserRole.Teacher);
                }
            }
            
            // Hala bulunamadıysa, ters yönde arama yap
            if (teacher == null)
            {
                var allTeachers = await _context.Users
                    .Where(u => u.Role == UserRole.Teacher)
                    .ToListAsync();
                
                teacher = allTeachers.FirstOrDefault(u => 
                    teacherNameLower.Contains(u.Name.ToLower().Trim()) || 
                    u.Name.ToLower().Trim().Contains(teacherNameLower));
            }
            
            // Debug: Tüm hocaları listele (hata ayıklama için)
            if (teacher == null)
            {
                var allTeachers = await _context.Users
                    .Where(u => u.Role == UserRole.Teacher)
                    .Select(u => u.Name)
                    .ToListAsync();
                
                throw new ArgumentException($"Öğretmen '{dto.TeacherName}' bulunamadı. Mevcut öğretmenler: {string.Join(", ", allTeachers)}");
            }
        }
        else if (!string.IsNullOrWhiteSpace(dto.TeacherEmail))
        {
            teacher = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == dto.TeacherEmail.ToLower().Trim() && u.Role == UserRole.Teacher);
        }

        if (teacher == null)
        {
            var errorMsg = "Öğretmen bulunamadı. ";
            if (dto.TeacherId.HasValue)
                errorMsg += $"TeacherId: {dto.TeacherId} ile eşleşen öğretmen bulunamadı. ";
            if (!string.IsNullOrWhiteSpace(dto.TeacherName))
                errorMsg += $"TeacherName: '{dto.TeacherName}' ile eşleşen öğretmen bulunamadı. ";
            if (!string.IsNullOrWhiteSpace(dto.TeacherEmail))
                errorMsg += $"TeacherEmail: '{dto.TeacherEmail}' ile eşleşen öğretmen bulunamadı. ";
            errorMsg += "Lütfen öğretim elemanı adını kontrol edin.";
            throw new ArgumentException(errorMsg);
        }

        if (teacher.Role != UserRole.Teacher)
            throw new ArgumentException($"Belirtilen kullanıcı öğretmen değil. UserId: {teacher.Id}");

        // Date ve Time'ı birleştirerek scheduled_at oluştur
        var scheduledAt = dto.Date.Date.Add(dto.Time);
        
        // 2. Müsaitlik kontrolü (ScheduleService)
        var isAvailable = await _scheduleService.IsTimeSlotAvailableAsync(teacher.Id, dto.Date, dto.Time);
        if (!isAvailable)
        {
            throw new InvalidOperationException("Seçilen tarih ve saat için öğretim elemanının müsaitliği bulunmamaktadır. Lütfen başka bir saat veya gün seçiniz.");
        }
        
        Appointment appointment;
        
        try
        {
            // reason değerini normalize et (enum için)
            var originalReason = dto.RequestReason ?? "other";
            var reasonValue = originalReason.ToLower().Trim();
            
            // Veritabanındaki enum değerlerini kontrol et
            // Enum'da sadece "question" ve "other" var, "exam" yok
            // Bu yüzden "exam" veya "sınav kağıdına bakma" için "other" kullanıyoruz
            if (reasonValue == "question" || reasonValue == "soru sorma" || reasonValue.Contains("soru"))
            {
                reasonValue = "question";
            }
            else if (reasonValue == "exam" || reasonValue == "sınav kağıdına bakma" || reasonValue.Contains("sınav"))
            {
                // Enum'da "exam" yok, "other" kullan
                // Öğrencinin yazdığı metin request_reason kolonuna kaydedilecek
                reasonValue = "other";
            }
            else
            {
                // Diğer tüm durumlar için "other"
                reasonValue = "other";
            }
            
            // Debug: Normalize edilmiş değeri logla
            System.Diagnostics.Debug.WriteLine($"CreateAppointmentAsync - Original reason: '{originalReason}', Normalized: '{reasonValue}'");
            
            // Öğrencinin yazdığı orijinal metni request_reason kolonuna kaydet
            var originalReasonEscaped = originalReason.Replace("'", "''");
            var subjectEscaped = dto.Subject.Replace("'", "''");
            var scheduledAtFormatted = scheduledAt.ToString("yyyy-MM-dd HH:mm:ss");
            
            // status enum değerini normalize et
            var statusValue = "pending";
            
            // INSERT'i çalıştır ve ID'yi al
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync();
            int newId;
            try
            {
                // Sadece mevcut kolonlara kaydet (reason ve status kolonları ignore edildi)
                string insertSql = $@"
                    INSERT INTO ""appointments"" 
                    (""student_id"", ""instructor_id"", ""course_name"", ""request_reason"", ""scheduled_at"", ""created_at"")
                    VALUES 
                    ({studentId}, {teacher.Id}, '{subjectEscaped}', '{originalReasonEscaped}', '{scheduledAtFormatted}'::timestamp, NOW())
                    RETURNING ""id"";
                ";
                
                using var command = connection.CreateCommand();
                command.CommandText = insertSql;
                var result = await command.ExecuteScalarAsync();
                if (result != null && int.TryParse(result.ToString(), out newId))
                {
                    // ID alındı
                }
                else
                {
                    throw new Exception("Randevu oluşturulamadı - ID alınamadı");
                }
            }
            finally
            {
                await connection.CloseAsync();
            }
            
            // Appointment objesini oluştur (diğer işlemler için)
            appointment = new Appointment
            {
                Id = newId,
                StudentId = studentId,
                TeacherId = teacher.Id,
                Date = dto.Date,
                Time = dto.Time,
                Subject = dto.Subject,
                RequestReason = originalReason, // Öğrencinin yazdığı orijinal metin (diğer seçeneğinde özel metin)
                Status = AppointmentStatus.Pending,
                CreatedAt = DateTime.Now
            };
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            // Inner exception'ı al - PostgreSQL hatası burada
            var innerEx = dbEx.InnerException;
            var errorMessage = innerEx?.Message ?? dbEx.Message;
            var fullError = dbEx.ToString();
            
            // PostgreSQL hata kodu kontrolü
            if (errorMessage.Contains("42703") || errorMessage.Contains("column") || errorMessage.Contains("does not exist"))
            {
                // Hangi kolonun bulunamadığını bulmaya çalış
                var columnMatch = System.Text.RegularExpressions.Regex.Match(errorMessage, @"column\s+""?([^""\s]+)""?\s+does not exist", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var columnName = columnMatch.Success ? columnMatch.Groups[1].Value : "bilinmeyen";
                
                throw new ArgumentException($"Veritabanı kolon hatası: '{columnName}' kolonu bulunamadı. Lütfen veritabanı şemasını kontrol edin. Hata: {errorMessage}. Full: {fullError}");
            }
            
            // Foreign key constraint hatası
            if (errorMessage.Contains("foreign key") || errorMessage.Contains("FOREIGN KEY"))
            {
                throw new ArgumentException($"Veritabanı hatası: Öğrenci (ID: {studentId}) veya Öğretmen (ID: {teacher.Id}) bulunamadı. Lütfen kullanıcı bilgilerini kontrol edin. Detay: {errorMessage}");
            }
            
            // Required field hatası
            if (errorMessage.Contains("NOT NULL") || errorMessage.Contains("required"))
            {
                throw new ArgumentException($"Veritabanı hatası: Zorunlu alanlar eksik. Detay: {errorMessage}");
            }
            
            // Genel veritabanı hatası
            throw new Exception($"Veritabanı hatası: {errorMessage}. StudentId: {studentId}, TeacherId: {teacher.Id}. Full: {fullError}", dbEx);
        }
        catch (Exception ex)
        {
            // Inner exception'ı logla - gerçek hatayı görmek için
            var innerException = ex.InnerException?.Message ?? ex.Message;
            var fullException = ex.ToString();
            
            throw new Exception($"Veritabanı hatası: {innerException}. StudentId: {studentId}, TeacherId: {teacher?.Id}. Full exception: {fullException}", ex);
        }

        // İlişkili verileri yükle (bildirim için)
        // student ve teacher zaten yukarıda yüklenmiş, direkt kullanabiliriz
        // Null check - Student ve Teacher yüklenmiş olmalı
        if (student == null)
            throw new InvalidOperationException($"Öğrenci bilgisi yüklenemedi. StudentId: {studentId}");
        if (teacher == null)
            throw new InvalidOperationException($"Öğretmen bilgisi yüklenemedi. TeacherId bulunamadı.");

        // Tarih ve saat formatını hazırla
        var dateStr = appointment.Date.ToString("dd.MM.yyyy");
        var timeStr = appointment.Time.ToString(@"hh\:mm");

        // Öğrenciye bildirim gönder (SignalR ile canlı bildirim)
        try
        {
            await _notificationService.SendNotificationAsync(
                "Randevu Talebi Oluşturuldu",
                $"Sayın {student.Name}, {dateStr} tarihinde {timeStr} saatinde {teacher.Name} hocasına randevu talebiniz oluşturulmuştur. Hocanızın onayını bekliyor.",
                NotificationType.AppointmentCreated,
                student.Email,
                student.Id,
                appointment.Id
            );
        }
        catch (Exception notifEx)
        {
            // Bildirim hatası randevu oluşturmayı engellemesin
            System.Diagnostics.Debug.WriteLine($"Öğrenciye bildirim gönderilirken hata: {notifEx.Message}");
        }

        // Hocaya bildirim gönder (SignalR ile canlı bildirim)
        try
        {
            await _notificationService.SendNotificationAsync(
                "Yeni Randevu Talebi",
                $"Sayın {teacher.Name}, {student.Name} ({student.StudentNo ?? "N/A"}) öğrencisi {dateStr} tarihinde {timeStr} saatinde randevu talebinde bulunmuştur. Konu: {appointment.Subject}",
                NotificationType.AppointmentCreated,
                teacher.Email,
                teacher.Id,
                appointment.Id
            );
        }
        catch (Exception notifEx)
        {
            // Bildirim hatası randevu oluşturmayı engellemesin
            System.Diagnostics.Debug.WriteLine($"Hocaya bildirim gönderilirken hata: {notifEx.Message}");
        }

        return appointment;
    }

    public async Task<Appointment?> UpdateAppointmentAsync(int id, AppointmentUpdateDto dto)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return null;

        // Randevu bilgileri güncelleme
        if (dto.Date.HasValue)
            appointment.Date = dto.Date.Value;

        if (dto.Time.HasValue)
            appointment.Time = dto.Time.Value;

        if (!string.IsNullOrEmpty(dto.Subject))
            appointment.Subject = dto.Subject;
        
        // Date veya Time değiştiyse scheduled_at'i güncelle
        if (dto.Date.HasValue || dto.Time.HasValue)
        {
            var newScheduledAt = appointment.Date.Date.Add(appointment.Time);
            _context.Entry(appointment).Property("ScheduledAt").CurrentValue = newScheduledAt;
        }

        // Durum değişikliği (Hoca onay/red işlemi)
        if (dto.Status.HasValue)
        {
            var oldStatus = appointment.Status;
            appointment.Status = dto.Status.Value;
            appointment.UpdatedAt = DateTime.Now;

            // Rejected ise rejection_reason'u da güncelle
            if (dto.Status.Value == AppointmentStatus.Rejected)
            {
                var rejectionReason = dto.RejectionReason ?? "Sebep belirtilmedi";
                appointment.RejectionReason = rejectionReason;
                var escapedReason = rejectionReason.Replace("'", "''");
                
                Console.WriteLine($"[DEBUG] UpdateAppointment - Rejecting ID: {appointment.Id}, Reason: {rejectionReason}");
                
                await _context.Database.ExecuteSqlRawAsync(
                    $"UPDATE \"appointments\" SET \"responded_at\" = NOW(), \"rejection_reason\" = '{escapedReason}' WHERE \"id\" = {appointment.Id}");
            }
            else
            {
                // Onaylandı veya başka durum
                appointment.RejectionReason = null;
                
                Console.WriteLine($"[DEBUG] UpdateAppointment - Approving ID: {appointment.Id}");
                
                await _context.Database.ExecuteSqlRawAsync(
                    $"UPDATE \"appointments\" SET \"responded_at\" = NOW(), \"rejection_reason\" = NULL WHERE \"id\" = {appointment.Id}");
            }

            // Durum değişikliğinde bildirim gönder
            var notificationType = dto.Status.Value switch
            {
                AppointmentStatus.Approved => NotificationType.AppointmentConfirmed,
                AppointmentStatus.Rejected => NotificationType.AppointmentCancelled,
                AppointmentStatus.Cancelled => NotificationType.AppointmentCancelled,
                AppointmentStatus.Completed => NotificationType.AppointmentConfirmed,
                _ => NotificationType.General
            };

            var statusMessage = dto.Status.Value switch
            {
                AppointmentStatus.Approved => "onaylanmıştır",
                AppointmentStatus.Rejected => "reddedilmiştir",
                AppointmentStatus.Cancelled => "iptal edilmiştir",
                AppointmentStatus.Completed => "tamamlanmıştır",
                _ => "güncellenmiştir"
            };

            // Öğrenciye bildirim (SignalR ile canlı bildirim)
            if (appointment.Student != null && appointment.Teacher != null)
            {
                await _notificationService.SendNotificationAsync(
                    $"Randevu Talebi {statusMessage}",
                    $"Sayın {appointment.Student.Name}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatindeki {appointment.Teacher.Name} hocasına olan randevu talebiniz {statusMessage}.",
                    notificationType,
                    appointment.Student.Email,
                    appointment.Student.Id,
                    appointment.Id
                );
            }
        }

        await _context.SaveChangesAsync();
        
        // scheduled_at'ten Date ve Time'ı yükle
        var savedScheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
        appointment.Date = savedScheduledAt.Date;
        appointment.Time = savedScheduledAt.TimeOfDay;
        
        return appointment;
    }

    public async Task<bool> DeleteAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
            return false;

        _context.Appointments.Remove(appointment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Appointment>> GetAppointmentsByStudentEmailAsync(string email)
    {
        // Email'i normalize et (küçük harfe çevir, trim yap)
        var normalizedEmail = email?.ToLower().Trim() ?? string.Empty;
        
        var appointments = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => a.Student != null && a.Student.Email.ToLower().Trim() == normalizedEmail)
            .ToListAsync();
        
        if (appointments.Count == 0)
            return appointments;
        
        // Status kolonu olmadığı için, responded_at ve rejection_reason kolonlarına göre status belirle
        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            await connection.OpenAsync();
        }
        try
        {
            // Her randevuyu kontrol et ve status'ü belirle
            foreach (var appointment in appointments)
            {
                var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
                appointment.Date = scheduledAt.Date;
                appointment.Time = scheduledAt.TimeOfDay;
                
                // rejection_reason kolonunu kontrol et
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT \"rejection_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                var rejectionReasonValue = await checkCmd.ExecuteScalarAsync();
                
                // Status belirleme:
                // - responded_at NULL -> Pending
                // - responded_at dolu, rejection_reason dolu -> Rejected
                // - responded_at dolu, rejection_reason NULL/boş -> Approved
                if (!appointment.UpdatedAt.HasValue)
                {
                    appointment.Status = AppointmentStatus.Pending;
                }
                else if (rejectionReasonValue != null && !string.IsNullOrWhiteSpace(rejectionReasonValue.ToString()))
                {
                    appointment.Status = AppointmentStatus.Rejected;
                    appointment.RejectionReason = rejectionReasonValue.ToString();
                }
                else
                {
                    appointment.Status = AppointmentStatus.Approved;
                }
            
                // RequestReason'ı request_reason kolonundan oku
                if (string.IsNullOrWhiteSpace(appointment.RequestReason) || appointment.RequestReason == "other")
                {
                    try
                    {
                        using var reasonCommand = connection.CreateCommand();
                        reasonCommand.CommandText = $"SELECT \"request_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                        var reasonResult = await reasonCommand.ExecuteScalarAsync();
                        if (reasonResult != null && !DBNull.Value.Equals(reasonResult))
                        {
                            var reasonText = reasonResult.ToString();
                            appointment.RequestReason = !string.IsNullOrWhiteSpace(reasonText) 
                                ? reasonText 
                                : "Belirtilmemiş";
                        }
                        else
                        {
                            appointment.RequestReason = "Belirtilmemiş";
                        }
                    }
                    catch
                    {
                        appointment.RequestReason = "Belirtilmemiş";
                    }
                }
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
        
        return appointments.OrderByDescending(a => a.Date).ToList();
    }

    public async Task<List<Appointment>> GetAppointmentsByTeacherEmailAsync(string email)
    {
        // Email'i normalize et (küçük harfe çevir, trim yap)
        var normalizedEmail = email?.ToLower().Trim() ?? string.Empty;
        
        var appointments = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => a.Teacher != null && a.Teacher.Email.ToLower().Trim() == normalizedEmail)
            .ToListAsync();
        
        if (appointments.Count == 0)
            return appointments;
        
        // Status kolonu olmadığı için, responded_at ve rejection_reason kolonlarına göre status belirle
        var filteredAppointments = new List<Appointment>();
        var connection = _context.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;
        if (!wasOpen)
        {
            await connection.OpenAsync();
        }
        try
        {
            // Her randevuyu kontrol et
            foreach (var appointment in appointments)
            {
                var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
                appointment.Date = scheduledAt.Date;
                appointment.Time = scheduledAt.TimeOfDay;
                
                // rejection_reason kolonunu kontrol et
                using var checkCmd = connection.CreateCommand();
                checkCmd.CommandText = $"SELECT \"rejection_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                var rejectionReasonValue = await checkCmd.ExecuteScalarAsync();
                
                // Eğer responded_at NULL ise -> Pending
                // Eğer responded_at dolu ve rejection_reason NULL/boş ise -> Approved (bunu dahil et)
                // Eğer responded_at dolu ve rejection_reason dolu ise -> Rejected (bunu hariç tut)
                if (!appointment.UpdatedAt.HasValue)
                {
                    // Pending - dahil etme (zaten pending requests'te gözükecek)
                    continue;
                }
                else if (rejectionReasonValue == null || string.IsNullOrWhiteSpace(rejectionReasonValue.ToString()))
                {
                    // Approved - dahil et
                    appointment.Status = AppointmentStatus.Approved;
                    
                    // RequestReason'ı request_reason kolonundan oku
                    if (string.IsNullOrWhiteSpace(appointment.RequestReason) || appointment.RequestReason == "other")
                    {
                        try
                        {
                            using var reasonCommand = connection.CreateCommand();
                            reasonCommand.CommandText = $"SELECT \"request_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                            var reasonResult = await reasonCommand.ExecuteScalarAsync();
                            if (reasonResult != null && !DBNull.Value.Equals(reasonResult))
                            {
                                var reasonText = reasonResult.ToString();
                                appointment.RequestReason = !string.IsNullOrWhiteSpace(reasonText) 
                                    ? reasonText 
                                    : "Belirtilmemiş";
                            }
                            else
                            {
                                appointment.RequestReason = "Belirtilmemiş";
                            }
                        }
                        catch
                        {
                            appointment.RequestReason = "Belirtilmemiş";
                        }
                    }
                    
                    filteredAppointments.Add(appointment);
                }
                // Rejected - hariç tut (else bloğu gereksiz)
            }
        }
        finally
        {
            if (!wasOpen)
            {
                await connection.CloseAsync();
            }
        }
        
        return filteredAppointments.OrderByDescending(a => a.Date).ToList();
    }

    public async Task<List<Appointment>> GetPendingAppointmentsByTeacherEmailAsync(string email)
    {
        // Email'i normalize et (küçük harfe çevir, trim yap)
        var normalizedEmail = email?.ToLower().Trim() ?? string.Empty;
        
        // Status kolonu enum tipinde olduğu için raw SQL ile filtreleme yapıyoruz
        // Önce teacher ID'sini bul
        var teacher = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower().Trim() == normalizedEmail && u.Role == UserRole.Teacher);
        
        if (teacher == null)
        {
            // Log: Teacher bulunamadı
            System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync: Teacher bulunamadı. Email: {normalizedEmail}");
            return new List<Appointment>();
        }
        
        // Teacher ID ile direkt sorgu yap (daha güvenli)
        var teacherId = teacher.Id;
        
        // Raw SQL ile sadece responded_at NULL olan (cevaplanmamış) randevuları al
        var appointmentIds = await _context.Database.SqlQueryRaw<int>(
            $@"SELECT a.""id"" 
               FROM ""appointments"" a
               WHERE a.""instructor_id"" = {teacherId}
               AND a.""responded_at"" IS NULL
               ORDER BY a.""created_at"" DESC"
        ).ToListAsync();
        
        System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync: Teacher ID: {teacherId}, Bulunan randevu ID sayısı: {appointmentIds?.Count ?? 0}");
        
        if (appointmentIds == null || appointmentIds.Count == 0)
            return new List<Appointment>();
        
        // ID'lere göre randevuları yükle
        var appointments = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .Where(a => appointmentIds.Contains(a.Id))
            .ToListAsync();
        
        System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync: Yüklenen randevu sayısı: {appointments.Count}");
        
        // scheduled_at'ten Date ve Time'ı yükle ve status'ü set et
        var connection2 = _context.Database.GetDbConnection();
        var wasOpen2 = connection2.State == System.Data.ConnectionState.Open;
        if (!wasOpen2)
        {
            await connection2.OpenAsync();
        }
        try
        {
            foreach (var appointment in appointments)
            {
                var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
                appointment.Date = scheduledAt.Date;
                appointment.Time = scheduledAt.TimeOfDay;
                appointment.Status = AppointmentStatus.Pending; // Model için
                
                // RequestReason'ı request_reason kolonundan oku (öğrencinin yazdığı özel metin)
                // Eğer kolon yoksa veya değer yoksa, reason enum kolonundan oku
                try
                {
                    // request_reason kolonunu okumayı dene
                    using var reasonCommand = connection2.CreateCommand();
                    reasonCommand.CommandText = $"SELECT \"request_reason\" FROM \"appointments\" WHERE \"id\" = {appointment.Id}";
                    var reasonResult = await reasonCommand.ExecuteScalarAsync();
                    System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync - Appointment ID: {appointment.Id}, request_reason raw: {reasonResult}");
                    
                    if (reasonResult != null && !DBNull.Value.Equals(reasonResult))
                    {
                        var reasonText = reasonResult.ToString();
                        System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync - Appointment ID: {appointment.Id}, request_reason text: '{reasonText}'");
                        
                        if (!string.IsNullOrWhiteSpace(reasonText))
                        {
                            appointment.RequestReason = reasonText;
                            System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync - Appointment ID: {appointment.Id}, RequestReason set to: '{reasonText}'");
                        }
                        else
                        {
                            // request_reason boşsa, varsayılan değer
                            appointment.RequestReason = "Belirtilmemiş";
                            System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync - Appointment ID: {appointment.Id}, RequestReason set to default");
                        }
                    }
                    else
                    {
                        // request_reason null ise, varsayılan değer
                        appointment.RequestReason = "Belirtilmemiş";
                        System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync - Appointment ID: {appointment.Id}, request_reason was null, RequestReason set to default");
                    }
                }
                catch (Exception ex)
                {
                    // request_reason kolonu yoksa veya hata olursa, varsayılan değer
                    System.Diagnostics.Debug.WriteLine($"GetPendingAppointmentsByTeacherEmailAsync - Appointment ID: {appointment.Id}, Error reading request_reason: {ex.Message}");
                    appointment.RequestReason = "Belirtilmemiş";
                }
            }
        }
        finally
        {
            if (!wasOpen2)
            {
                await connection2.CloseAsync();
            }
        }
        
        return appointments.OrderByDescending(a => a.CreatedAt).ToList();
    }

    public async Task<Appointment?> ApproveAppointmentAsync(int id)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
            return null;

        // Status kolonu yok, direkt onaylıyoruz
        appointment.Status = AppointmentStatus.Approved;
        appointment.UpdatedAt = DateTime.Now;
        appointment.RejectionReason = null;

        // responded_at kolonunu güncelle
        await _context.Database.ExecuteSqlRawAsync(
            $"UPDATE \"appointments\" SET \"responded_at\" = NOW() WHERE \"id\" = {appointment.Id}");
        
        // scheduled_at'ten Date ve Time'ı yükle
        var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
        appointment.Date = scheduledAt.Date;
        appointment.Time = scheduledAt.TimeOfDay;

        // Öğrenciye bildirim gönder
        if (appointment.Student != null)
        {
            await _notificationService.SendNotificationAsync(
                "Randevu Talebi Onaylandı",
                $"Sayın {appointment.Student.Name}, {appointment.Date:dd.MM.yyyy} tarihinde {appointment.Time:hh\\:mm} saatindeki {appointment.Teacher?.Name ?? "Hoca"} hocasına olan randevu talebiniz onaylanmıştır.",
                NotificationType.AppointmentConfirmed,
                appointment.Student.Email,
                appointment.Student.Id,
                appointment.Id
            );
        }

        return appointment;
    }

    public async Task<Appointment?> RejectAppointmentAsync(int id, string? rejectionReason = null)
    {
        Console.WriteLine($"[DEBUG] RejectAppointmentAsync called - ID: {id}, Reason: {rejectionReason ?? "NULL"}");
        
        var appointment = await _context.Appointments
            .Include(a => a.Student)
            .Include(a => a.Teacher)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appointment == null)
        {
            Console.WriteLine($"[DEBUG] Appointment not found - ID: {id}");
            return null;
        }

        Console.WriteLine($"[DEBUG] Found appointment - ID: {id}, Student: {appointment.Student?.Email}, Teacher: {appointment.Teacher?.Email}");

        // Status kolonu yok, direkt reddediyoruz
        appointment.Status = AppointmentStatus.Rejected;
        appointment.UpdatedAt = DateTime.Now;
        appointment.RejectionReason = rejectionReason ?? string.Empty;

        // responded_at ve rejection_reason kolonlarını güncelle
        var escapedReason = (rejectionReason ?? string.Empty).Replace("'", "''");
        var updateSql = $"UPDATE \"appointments\" SET \"responded_at\" = NOW(), \"rejection_reason\" = '{escapedReason}' WHERE \"id\" = {appointment.Id}";
        Console.WriteLine($"[DEBUG] Executing SQL: {updateSql}");
        
        await _context.Database.ExecuteSqlRawAsync(updateSql);
        
        // scheduled_at'ten Date ve Time'ı yükle
        var scheduledAt = _context.Entry(appointment).Property<DateTime>("ScheduledAt").CurrentValue;
        appointment.Date = scheduledAt.Date;
        appointment.Time = scheduledAt.TimeOfDay;

        // Öğrenciye bildirim gönder
        if (appointment.Student != null && appointment.Teacher != null)
        {
            var dateStr = appointment.Date.ToString("dd.MM.yyyy");
            var timeStr = appointment.Time.ToString(@"hh\:mm");
            
            var message = $"Sayın {appointment.Student.Name}, {dateStr} tarihinde {timeStr} saatindeki {appointment.Teacher.Name} hocasına olan randevu talebiniz reddedilmiştir.";
            if (!string.IsNullOrWhiteSpace(rejectionReason))
            {
                message += $" Sebep: {rejectionReason}";
            }

            try
            {
                await _notificationService.SendNotificationAsync(
                    "Randevu Talebi Reddedildi",
                    message,
                    NotificationType.AppointmentCancelled,
                    appointment.Student.Email,
                    appointment.Student.Id,
                    appointment.Id
                );
            }
            catch (Exception notifEx)
            {
                // Bildirim hatası red işlemini engellemesin
                System.Diagnostics.Debug.WriteLine($"Öğrenciye bildirim gönderilirken hata: {notifEx.Message}");
            }
        }

        return appointment;
    }
}
