using ApiProject.Models.DTOs;
using ApiProject.Services;
using ApiProject.Data;
using ApiProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AuthController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto registerDto)
    {
        // Request body null kontrolü
        if (registerDto == null)
        {
            return BadRequest(new { message = "Request body boş olamaz." });
        }

        // ModelState validation kontrolü
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Akıllı rol kontrolü backend koruması:
        // Eğer kullanıcı adı bir rakamla başlıyorsa (öğrenci numarası gibi),
        // Rol kesinlikle Student olmalıdır.
        if (!string.IsNullOrWhiteSpace(registerDto.Username))
        {
            char firstChar = registerDto.Username.Trim()[0];
            if (char.IsDigit(firstChar))
            {
                if (registerDto.Role != UserRole.Student)
                {
                    return BadRequest(new { message = "Öğrenci numarası ile başlayan kullanıcı adları sadece 'Öğrenci' rolü ile kayıt olabilir." });
                }
            }
            else
            {
                // Rakamla BAŞLAMIYORSA (Harf vb.), Rol 'Student' OLAMAZ.
                if (registerDto.Role == UserRole.Student)
                {
                    return BadRequest(new { message = "Harf ile başlayan kullanıcı adları 'Öğrenci' rolü ile kayıt olamaz. Lütfen 'Akademik Personel' rolünü seçiniz." });
                }
            }
        }

        try
        {
            var result = await _authService.RegisterAsync(registerDto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateException dbEx)
        {
            // Veritabanı güncelleme hatalarını daha detaylı göster
            var errorMessage = dbEx.Message;
            if (dbEx.InnerException != null)
            {
                errorMessage += " | Inner: " + dbEx.InnerException.Message;
            }
            // PostgreSQL hata kodunu ve mesajını göster
            return StatusCode(500, new { 
                message = "Kayıt işlemi sırasında veritabanı hatası oluştu.", 
                error = errorMessage,
                innerError = dbEx.InnerException?.Message,
                stackTrace = dbEx.StackTrace
            });
        }
        catch (Exception ex)
        {
            // Inner exception varsa onu da göster
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += " | Inner: " + ex.InnerException.Message;
            }
            return StatusCode(500, new { message = "Kayıt işlemi sırasında bir hata oluştu.", error = errorMessage, innerError = ex.InnerException?.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto loginDto)
    {
        // Request body null kontrolü
        if (loginDto == null)
        {
            return BadRequest(new { message = "Request body boş olamaz." });
        }

        // ModelState validation kontrolü
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
                return Unauthorized(new { message = "Kullanıcı adı/e-posta veya şifre hatalı." });

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Giriş işlemi sırasında bir hata oluştu.", error = ex.Message });
        }
    }

    /// <summary>
    /// Şifre sıfırlama talebi. E-posta yoksa bile aynı genel mesaj (enumeration azaltma).
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "İstek gövdesi boş olamaz." });
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _authService.RequestPasswordResetAsync(dto.Email);
            return Ok(new
            {
                message =
                    "Bu e-posta adresi sistemde kayıtlıysa, şifre sıfırlama bağlantısı gönderilmiştir. Gelen kutunuzu ve spam klasörünü kontrol edin."
            });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "İşlem sırasında bir hata oluştu.", error = ex.Message });
        }
    }

    /// <summary>
    /// E-postadaki token ile yeni şifre belirleme.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto dto)
    {
        if (dto == null)
            return BadRequest(new { message = "İstek gövdesi boş olamaz." });
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ok = await _authService.ResetPasswordWithTokenAsync(dto.Token, dto.NewPassword);
            if (!ok)
            {
                return BadRequest(new
                {
                    message =
                        "Bağlantı geçersiz veya süresi dolmuş. Lütfen giriş sayfasından yeni bir şifre sıfırlama talebi oluşturun."
                });
            }

            return Ok(new { message = "Şifreniz başarıyla güncellendi." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Şifre güncellenirken bir hata oluştu.", error = ex.Message });
        }
    }

    /// <summary>
    /// Tüm kullanıcıları listeler (Email ve şifre gösterilmez)
    /// </summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<UserResponseDto>>> GetAllUsers()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Role = u.Role.ToString(),
                    StudentNo = u.StudentNo
                })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Kullanıcılar getirilirken bir hata oluştu.", error = ex.Message });
        }
    }

    /// <summary>
    /// Tüm öğretmenleri listeler
    /// </summary>
    [HttpGet("teachers")]
    public async Task<ActionResult<List<UserResponseDto>>> GetAllTeachers()
    {
        try
        {
            var teachers = await _context.Users
                .Where(u => u.Role == UserRole.Teacher)
                .Select(u => new UserResponseDto
                {
                    Id = u.Id,
                    Name = u.Name,
                    Role = u.Role.ToString(),
                    StudentNo = u.StudentNo
                })
                .OrderBy(u => u.Name)
                .ToListAsync();

            return Ok(teachers);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Öğretmenler getirilirken bir hata oluştu.", error = ex.Message });
        }
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromQuery] int userId)
    {
        // Frontend URL'ini environment variable'dan al (Docker için)
        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";
        
        if (string.IsNullOrEmpty(token) || userId <= 0)
        {
            var errorHtml = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Doğrulama Hatası</title>
                    <style>
                        body { font-family: 'Segoe UI', Arial, sans-serif; text-align: center; padding: 50px; background: #f5f5f5; }
                        .container { max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                        .error-icon { font-size: 64px; color: #ff4444; margin-bottom: 20px; }
                        h1 { color: #ff4444; margin-bottom: 20px; }
                        p { color: #666; line-height: 1.6; font-size: 16px; }
                        .button { display: inline-block; margin-top: 20px; padding: 12px 30px; background: #d71920; color: white; text-decoration: none; border-radius: 5px; }
                        .button:hover { background: #b01519; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='error-icon'>⚠️</div>
                        <h1>Geçersiz Doğrulama İsteği</h1>
                        <p>Doğrulama linki hatalı veya eksik bilgi içeriyor.</p>
                        <p>Lütfen e-postanızdaki doğrulama linkine tekrar tıklayın veya yeni bir doğrulama e-postası isteyin.</p>
                        <a href='{frontendUrl}' class='button'>Giriş Sayfasına Dön</a>
                    </div>
                </body>
                </html>";
            return Content(errorHtml, "text/html; charset=utf-8");
        }

        var result = await _authService.VerifyEmailAsync(token, userId);

        if (result)
        {
            var successHtml = @"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>E-posta Doğrulandı</title>
                    <style>
                        body { font-family: 'Segoe UI', Arial, sans-serif; text-align: center; padding: 50px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); }
                        .container { max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 10px 30px rgba(0,0,0,0.2); }
                        .success-icon { font-size: 64px; margin-bottom: 20px; }
                        h1 { color: #4CAF50; margin-bottom: 20px; }
                        p { color: #666; line-height: 1.6; font-size: 16px; margin: 15px 0; }
                        .button { display: inline-block; margin-top: 30px; padding: 15px 40px; background: #d71920; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }
                        .button:hover { background: #b01519; transform: translateY(-2px); transition: all 0.3s; }
                        .info-box { background: #f0f8ff; padding: 20px; border-radius: 8px; margin-top: 20px; border-left: 4px solid #4CAF50; }
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='success-icon'>✅</div>
                        <h1>E-posta Başarıyla Doğrulandı!</h1>
                        <p>Tebrikler! Hesabınız başarıyla aktifleştirildi.</p>
                        <div class='info-box'>
                            <p><strong>Artık giriş yapabilirsiniz!</strong></p>
                            <p>Kullanıcı adınız ve şifrenizle sisteme giriş yaparak tüm özellikleri kullanmaya başlayabilirsiniz.</p>
                        </div>
                        <a href='{frontendUrl}' class='button'>Giriş Yap</a>
                    </div>
                </body>
                </html>";
            return Content(successHtml, "text/html; charset=utf-8");
        }
        
        var failureHtml = @"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Doğrulama Başarısız</title>
                <style>
                    body { font-family: 'Segoe UI', Arial, sans-serif; text-align: center; padding: 50px; background: #f5f5f5; }
                    .container { max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                    .error-icon { font-size: 64px; color: #ff9800; margin-bottom: 20px; }
                    h1 { color: #ff9800; margin-bottom: 20px; }
                    p { color: #666; line-height: 1.6; font-size: 16px; }
                    .button { display: inline-block; margin-top: 20px; padding: 12px 30px; background: #d71920; color: white; text-decoration: none; border-radius: 5px; }
                    .button:hover { background: #b01519; }
                    ul { text-align: left; display: inline-block; margin-top: 15px; }
                    li { margin: 10px 0; color: #666; }
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='error-icon'>❌</div>
                    <h1>E-posta Doğrulanamadı</h1>
                    <p>Doğrulama işlemi başarısız oldu.</p>
                    <p><strong>Olası Sebepler:</strong></p>
                    <ul>
                        <li>Doğrulama linki geçersiz veya süresi dolmuş olabilir</li>
                        <li>Bu hesap daha önce doğrulanmış olabilir</li>
                        <li>Sunucu yeniden başlatılmış olabilir</li>
                    </ul>
                    <p style='margin-top: 20px;'><strong>Çözüm:</strong> Lütfen tekrar kayıt olun veya destek ekibiyle iletişime geçin.</p>
                    <a href='{frontendUrl}' class='button'>Giriş Sayfasına Dön</a>
                </div>
            </body>
            </html>";
        return Content(failureHtml, "text/html; charset=utf-8");
    }
    [HttpGet("debug-schema")]
    public IActionResult DebugSchema()
    {
        try 
        {
            var columns = new List<string>();
            var connection = _context.Database.GetDbConnection();
            connection.Open();
            
            using (var command = connection.CreateCommand())
            {
                // Enum değerlerini sorgula
                command.CommandText = "SELECT unnest(enum_range(NULL::login_type))";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                         // Enum değerini string olarak oku (GetFieldValue<string> veya GetString)
                         // Postgres enum -> string mapping bazen direkt GetString ile çalışır
                         columns.Add($"ENUM VALUE: {reader.GetValue(0)}");
                    }
                }
            }
            connection.Close();
            
            return Ok(columns);
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}

