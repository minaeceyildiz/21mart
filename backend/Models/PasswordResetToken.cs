using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProject.Models;

/// <summary>
/// Şifre sıfırlama talebi. E-postadaki ham token saklanmaz; yalnızca SHA-256 özeti tutulur.
/// </summary>
public class PasswordResetToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>UTF-8 ham token üzerinden hesaplanan SHA-256 (hex).</summary>
    [Required]
    [MaxLength(64)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
