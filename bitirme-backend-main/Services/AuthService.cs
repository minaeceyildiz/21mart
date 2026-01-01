using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Data.Common;
using BCrypt.Net;

namespace ApiProject.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto loginDto);
    Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
    string GenerateJwtToken(User user);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        // Email otomatik oluştur (kullanıcı adından)
        var email = $"{registerDto.Username.ToLower().Trim().Replace(" ", ".")}@baskent.edu.tr";
        
        // Email kontrolü (eğer aynı email varsa)
        var existingEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (existingEmail != null)
        {
            // Email çakışması durumunda benzersiz bir email oluştur
            var counter = 1;
            var baseEmail = email;
            while (existingEmail != null)
            {
                email = $"{registerDto.Username.ToLower().Trim().Replace(" ", ".")}{counter}@baskent.edu.tr";
                existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email);
                counter++;
            }
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
            // login_type kolonu veritabanında özel bir tip olduğu için mapping'den çıkarıldı
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            Name = user.Name,
            Role = user.Role.ToString()
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
}

