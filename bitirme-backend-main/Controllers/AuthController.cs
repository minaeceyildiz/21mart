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
}

