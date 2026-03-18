using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProject.Models;

[Table("OccupancyLogs")]
public class OccupancyLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string ZoneName { get; set; } = string.Empty; // kütüphane_kat1, otopark_a vb.
    
    [Required]
    public int Count { get; set; }
    
    [Required]
    public int Capacity { get; set; }
    
    [Required]
    public DateTime LogTime { get; set; }
}

