using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProject.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int StudentId { get; set; }
    
    [Required]
    public DateTime OrderDate { get; set; }
    
    [Required]
    [MaxLength(50)]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [MaxLength(10)]
    public string? PickupTime { get; set; }
    
    // Navigation Properties
    [ForeignKey("StudentId")]
    public virtual User Student { get; set; } = null!;
    
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Pending = 0,
    Preparing = 1,
    Ready = 2,
    Delivered = 3,
    Cancelled = 4
}

