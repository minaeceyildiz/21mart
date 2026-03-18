using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ApiProject.Models;

namespace ApiProject.Models.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
    [MaxLength(200)]
    [JsonPropertyName("name")]
    public string Username { get; set; } = string.Empty;

    // Email artık kullanılmıyor ama frontend gönderiyor, ignore ediyoruz
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Şifre gereklidir")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol gereklidir")]
    [JsonPropertyName("role")]
    public string RoleString { get; set; } = "Student";

    [MaxLength(50)]
    [JsonPropertyName("studentNo")]
    public string? StudentNo { get; set; }

    // Role property'si - string'den enum'a dönüştürülüyor
    [JsonIgnore]
    public UserRole Role
    {
        get
        {
            if (string.IsNullOrEmpty(RoleString))
                return UserRole.Student;

            return RoleString.ToLower() switch
            {
                "teacher" or "instructor" => UserRole.Teacher,
                "student" => UserRole.Student,
                "staff" => UserRole.Staff,
                "admin" => UserRole.Admin,
                _ => UserRole.Student
            };
        }
    }
}

