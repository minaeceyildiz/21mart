using System.ComponentModel.DataAnnotations;

namespace ApiProject.Models.DTOs;

public class OccupancyUpdateDto
{
    [Required(ErrorMessage = "Bölge adı gereklidir")]
    [MaxLength(100)]
    public string ZoneName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kişi sayısı gereklidir")]
    [Range(0, int.MaxValue, ErrorMessage = "Kişi sayısı 0 veya pozitif olmalıdır")]
    public int Count { get; set; }

    [Required(ErrorMessage = "Kapasite gereklidir")]
    [Range(1, int.MaxValue, ErrorMessage = "Kapasite en az 1 olmalıdır")]
    public int Capacity { get; set; }
}

