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
    /// <summary>Kasiyer denetimi: sipariş sahibinin görünen adı (User.Name).</summary>
    public string? CustomerName { get; set; }
    /// <summary>Kasiyer denetimi: e-posta.</summary>
    public string? CustomerEmail { get; set; }
    /// <summary>Varsa öğrenci numarası.</summary>
    public string? StudentNo { get; set; }
    public OrderUserType UserType { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PickupTime { get; set; }
    public string? Note { get; set; }
    public List<OrderItemResponseDto> OrderItems { get; set; } = new();

    /// <summary>Kasiyer listesi: müşterinin tüm NotPaid sipariş adedi (Ready dahil değil).</summary>
    public int CustomerNotPaidCount { get; set; }
    /// <summary>Kasiyer listesi: müşterinin NotPaid siparişlerinin toplam tutarı.</summary>
    public decimal CustomerNotPaidTotal { get; set; }
}

