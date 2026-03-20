using System.ComponentModel.DataAnnotations;

namespace ApiProject.Models.DTOs;

public class ForgotPasswordRequestDto
{
    [Required(ErrorMessage = "E-posta gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequestDto
{
    [Required(ErrorMessage = "Sıfırlama anahtarı eksik.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre gereklidir.")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string NewPassword { get; set; } = string.Empty;
}
