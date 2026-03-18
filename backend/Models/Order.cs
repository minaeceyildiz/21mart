using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProject.Models;

public class Order
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(32)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public OrderUserType UserType { get; set; } = OrderUserType.Student;

    [Required]
    public DateTime CreatedAt { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public DateTime? ReadyAt { get; set; }

    public DateTime? PaidAt { get; set; }
    
    [Required]
    [MaxLength(50)]
    public OrderStatus Status { get; set; } = OrderStatus.Received;

    [Required]
    public bool IsPaid { get; set; } = false;
    
    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    [MaxLength(10)]
    public string? PickupTime { get; set; }
    
    // Navigation Properties
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
    
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}

public enum OrderStatus
{
    Received = 0,
    Approved = 1,
    Preparing = 2,
    Ready = 3,
    Paid = 4,
    Cancelled = 5
}

public enum OrderUserType
{
    Student = 0,
    Staff = 1
}

