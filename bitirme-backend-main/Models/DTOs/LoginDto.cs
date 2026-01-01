using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ApiProject.Models;

namespace ApiProject.Models.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "Kullanıcı adı veya e-posta gereklidir")]
    [JsonPropertyName("usernameOrEmail")]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir")]
    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol gereklidir")]
    [JsonPropertyName("role")]
    public string RoleString { get; set; } = "Student";

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

