using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Data.Common;
using BCrypt.Net;
using Microsoft.Extensions.Logging;

namespace ApiProject.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    Task<bool> VerifyEmailAsync(string token, int userId);
    string GenerateJwtToken(User user);
    /// <summary>E-posta kayıtlı değilse sessizce çıkar; enumeration riskini azaltmak için.</summary>
    Task RequestPasswordResetAsync(string email);
    /// <summary>Geçerli token ile şifreyi günceller; token tek kullanımlıktır.</summary>
    Task<bool> ResetPasswordWithTokenAsync(string plainToken, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    // IN-MEMORY TOKEN STORAGE (Veritabanı şeması değiştirilemediği için)
    // Key: Token, Value: UserId
    private static readonly ConcurrentDictionary<string, int> _verificationTokens = new();

    public AuthService(AppDbContext context, IConfiguration configuration, IEmailService emailService, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto loginDto)
    {
        var usernameOrEmail = loginDto.UsernameOrEmail?.ToLower().Trim() ?? string.Empty;
        
        if (string.IsNullOrEmpty(usernameOrEmail))
            return null;

        // Kullanıcı adı (Name) veya Email ile arama yap
        var user = await _context.Users
            .FirstOrDefaultAsync(u => 
                u.Email.ToLower().Trim() == usernameOrEmail || 
                u.Name.ToLower().Trim() == usernameOrEmail);

        if (user == null)
            return null;

        // BCrypt ile şifre kontrolü
        if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            return null;

        // Email doğrulama kontrolü
        // login_type veritabanında PostgreSQL ENUM (NULL, 'school_email', 'staff_id')
        // Ham SQL ile kontrol yapıyoruz
        //
        // ÖZEL DURUM: Kasiyer hesabı (tek hesap) için email doğrulaması zorunlu değil.
        // Bu yüzden Name = "kasiyer" ve Role = Staff ise bu kontrolü atlıyoruz.
        if (!(user.Name.ToLower().Trim() == "kasiyer" && user.Role == UserRole.Staff))
        {
            try
            {
                // ExecuteSqlRaw yerine doğrudan bağlantı üzerinden scalar değer okuyalım
                using var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT login_type::text FROM users WHERE id = @userId";
                var parameter = command.CreateParameter();
                parameter.ParameterName = "@userId";
                parameter.Value = user.Id;
                command.Parameters.Add(parameter);
                
                var loginType = await command.ExecuteScalarAsync();
                var loginTypeString = loginType?.ToString() ?? string.Empty;
                
                _logger.LogInformation($"Login attempt - UserId: {user.Id}, Email: {user.Email}, LoginType: '{loginTypeString}'");
                
                // login_type NULL veya boş ise doğrulanmamış demektir
                if (string.IsNullOrWhiteSpace(loginTypeString))
                {
                    _logger.LogWarning($"Email doğrulanmamış kullanıcı giriş denemesi: {user.Email}");
                    throw new UnauthorizedAccessException("Lütfen e-posta adresinizi doğrulayın. Kayıt sırasında gönderilen e-postadaki linke tıklayınız.");
                }
                
                _logger.LogInformation($"Email doğrulaması başarılı: {user.Email}");
            }
            catch (UnauthorizedAccessException)
            {
                // UnauthorizedAccessException'ı yukarı fırlat
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"login_type kontrolü sırasında hata: {user.Email}");
                throw new UnauthorizedAccessException("Giriş kontrolü sırasında bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }
        }

        // AppDbContext.cs'de role_id değeri otomatik olarak enum'a çevriliyor (role_id - 1)
        // Bu yüzden user.Role zaten doğru enum değerini içeriyor
        var userRoleFromDb = user.Role;

        // Rol kontrolü: Öğrenci akademik (Teacher) rolü ile giriş yapamamalı
        if (userRoleFromDb == UserRole.Student && loginDto.Role == UserRole.Teacher)
        {
            throw new UnauthorizedAccessException("Öğrenci kullanıcılar akademik personel rolü ile giriş yapamaz.");
        }

        // Kullanıcının gerçek rolü ile giriş yapmaya çalıştığı rol eşleşmeli
        if (userRoleFromDb != loginDto.Role)
        {
            throw new UnauthorizedAccessException("Seçilen rol ile kullanıcı rolü eşleşmiyor.");
        }

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString()
        };
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Kullanıcı adı kontrolü (Name ile kontrol)
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Name.ToLower().Trim() == registerDto.Username.ToLower().Trim());

        if (existingUser != null)
            throw new InvalidOperationException("Bu kullanıcı adı zaten kullanılıyor.");

        // Email kontrolü
        string email;
        if (!string.IsNullOrEmpty(registerDto.Email))
        {
            email = registerDto.Email.Trim();
        }
        else
        {
            // Email gelmediyse kullanıcı adından oluştur (Fallback)
            email = $"{registerDto.Username.ToLower().Trim().Replace(" ", ".")}@baskent.edu.tr";
        }
        
        // Email unique kontrolü
        var existingEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (existingEmail != null)
        {
             throw new InvalidOperationException("Bu email adresi zaten kullanılıyor.");
        }

        // Şifreyi BCrypt ile hashle
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        // role_id foreign key için roles tablosundaki gerçek ID'yi kontrol et
        // AppDbContext.cs'de enum değerini +1 yaparak kaydediyoruz (Student=0 -> role_id=1, Teacher=1 -> role_id=2)
        // Bu yüzden enum değerini direkt kullanıyoruz, AppDbContext otomatik olarak +1 yapacak
        var user = new User
        {
            Name = registerDto.Username,
            Email = email,
            PasswordHash = passwordHash,
            Role = registerDto.Role, // Enum değerini direkt kullan (Student=0, Teacher=1, vb.)
            // AppDbContext.cs'de enum değerini +1 yaparak role_id'ye kaydedecek
            StudentNo = registerDto.StudentNo
            // LoginType ignore edildi, varsayılan olarak NULL kalacak (doğrulanmamış)
        };
        
        // Düzeltme: User kaydedildikten sonra ID oluşacak.

        // ÖNCE login_type NULL olan bir kayıt eklemek için ham SQL kullan
        // Entity Framework ignore ettiği için direkt SQL ile ekliyoruz
        try
        {
            // Önce Entity Framework ile ekle
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Sonra login_type'ı NULL yap (ExecuteSqlRawAsync ile)
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE users SET login_type = CAST(NULL AS login_type) WHERE id = {0}",
                user.Id
            );

            _logger.LogInformation($"Yeni kullanıcı kaydedildi (doğrulanmamış): UserId={user.Id}, Email={user.Email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Kullanıcı kaydı sırasında hata: {user.Email}");
            // Hata varsa kullanıcıyı silmeyi dene
            try
            {
                var userToDelete = await _context.Users.FindAsync(user.Id);
                if (userToDelete != null)
                {
                    _context.Users.Remove(userToDelete);
                    await _context.SaveChangesAsync();
                }
            }
            catch { /* Silme hatası önemsiz */ }
            
            throw new InvalidOperationException($"Kayıt işlemi sırasında bir hata oluştu: {ex.Message}");
        }

        // Email doğrulama token'ı oluştur (GUID)
        var verificationToken = Guid.NewGuid().ToString();
        
        // Token'ı belleğe kaydet
        _verificationTokens.TryAdd(verificationToken, user.Id);
        _logger.LogInformation($"Token belleğe eklendi: Token={verificationToken}, UserId={user.Id}");

        // Email doğrulama maili gönder (ZORUNLU - başarısız olursa kayıt iptal edilir)
        try
        {
            await _emailService.SendVerificationEmailAsync(email, user.Name, verificationToken, user.Id);
        }
        catch (Exception ex)
        {
            // Email gönderilemezse kullanıcıyı sil ve hata fırlat
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogError(ex, $"Email gönderilemedi, kullanıcı kaydı iptal edildi: {email}");
            throw new InvalidOperationException("Email gönderilirken bir hata oluştu. Lütfen SMTP ayarlarınızı kontrol edin ve daha sonra tekrar deneyin.");
        }

        // Kayıt başarılı - Email gönderildi
        // Kullanıcıya email doğrulama mesajı dön
        // Token boş dönüyoruz çünkü email doğrulamadan giriş yapılamaz
        return new AuthResponseDto
        {
            Token = "", // Email doğrulanmadığı için token yok
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString(),
            Message = "Kayıt başarılı! Lütfen email adresinize gelen doğrulama linkine tıklayarak hesabınızı aktifleştirin."
        };
    }

    public string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey bulunamadı.");
        var issuer = jwtSettings["Issuer"] ?? "ApiProject";
        var audience = jwtSettings["Audience"] ?? "ApiProjectUsers";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "1440"); // Varsayılan 24 saat

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> VerifyEmailAsync(string token, int userId)
    {
        _logger.LogInformation($"Email doğrulama isteği: Token={token}, UserId={userId}");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            _logger.LogWarning($"Doğrulama başarısız: Kullanıcı bulunamadı ID={userId}");
            return false;
        }

        // login_type kontrolü (Ham SQL ile - PostgreSQL ENUM)
        var loginTypeResult = await _context.Database
            .SqlQueryRaw<string>("SELECT login_type::text FROM users WHERE id = {0}", userId)
            .ToListAsync();
        
        var currentLoginType = loginTypeResult.FirstOrDefault();
        
        _logger.LogInformation($"Kullanıcı bulundu: {user.Email}, LoginType={currentLoginType}");

        // Zaten doğrulanmışsa true dön (Idempotency)
        if (!string.IsNullOrEmpty(currentLoginType))
        {
            _logger.LogInformation("Kullanıcı zaten doğrulanmış.");
            return true;
        }

        // In-Memory Token Kontrolü
        if (_verificationTokens.TryGetValue(token, out int storedUserId))
        {
            if (storedUserId != userId)
            {
                _logger.LogWarning($"Token userId uyuşmazlığı: TokenUserId={storedUserId}, RequestUserId={userId}");
                return false;
            }
            // Başarılı, tokenı sil
            _verificationTokens.TryRemove(token, out _);
        }
        else
        {
             // Eğer token bellekte yoksa, belki daha önce veritabanına kaydedilmiş eski bir tokendir (Geriye uyumluluk için bakılabilir ama şu anki yapıda LoginType null)
             // Veya sunucu yeniden başlatılmıştır.
             _logger.LogWarning($"Token bellekte bulunamadı: {token}");
             return false;
        }

        // LoginType'ı "school_email" olarak güncelle (Ham SQL ile - PostgreSQL ENUM tipi için)
        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE users SET login_type = 'school_email'::login_type WHERE id = {0}", 
                userId
            );
            _logger.LogInformation("Email başarıyla doğrulandı ve login_type güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "login_type güncellenirken hata oluştu.");
            // Hata olsa bile devam et (token zaten silindi, kullanıcı var)
        }

        return true;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrEmpty(normalized))
            return;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized);

        if (user == null)
        {
            _logger.LogInformation("Şifre sıfırlama: e-posta sistemde yok (genel yanıt verilecek): {Email}", normalized);
            return;
        }

        var expiryMinutes = _configuration.GetValue("PasswordReset:ExpiryMinutes", 20);
        if (expiryMinutes < 15) expiryMinutes = 15;
        if (expiryMinutes > 30) expiryMinutes = 30;

        var pending = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();
        _context.PasswordResetTokens.RemoveRange(pending);

        var plainToken = GenerateSecureUrlToken();
        var tokenHash = HashPasswordResetToken(plainToken);

        var entity = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            CreatedAt = DateTime.UtcNow
        };
        _context.PasswordResetTokens.Add(entity);
        await _context.SaveChangesAsync();

        var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                           ?? _configuration["PasswordReset:FrontendUrl"]
                           ?? "http://localhost:3000";
        var resetLink =
            $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(plainToken)}";

        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetLink, expiryMinutes);
        }
        catch (Exception ex)
        {
            _context.PasswordResetTokens.Remove(entity);
            await _context.SaveChangesAsync();
            _logger.LogError(ex, "Şifre sıfırlama e-postası gönderilemedi, token iptal: UserId={UserId}", user.Id);
            throw;
        }
    }

    public async Task<bool> ResetPasswordWithTokenAsync(string plainToken, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(plainToken) || string.IsNullOrWhiteSpace(newPassword))
            return false;

        var tokenHash = HashPasswordResetToken(plainToken.Trim());
        var row = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow);

        if (row == null)
        {
            _logger.LogWarning("Şifre sıfırlama: geçersiz veya süresi dolmuş token");
            return false;
        }

        row.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        row.UsedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Şifre sıfırlama başarılı: UserId={UserId}", row.UserId);
        return true;
    }

    private static string HashPasswordResetToken(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateSecureUrlToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
