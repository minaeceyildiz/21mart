namespace ApiProject.Models.DTOs;

public class OrderResponseDto
{
    public int Id { get; set; }
    public List<OrderItemResponseDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string PickupTime { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class OrderItemResponseDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
