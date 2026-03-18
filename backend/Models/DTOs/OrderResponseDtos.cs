using ApiProject.Models;

namespace ApiProject.Models.DTOs;

public class OrderItemResponseDto
{
    public int Id { get; set; }
    public int MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderResponseDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
    public OrderUserType UserType { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public List<OrderItemResponseDto> OrderItems { get; set; } = new();
}

