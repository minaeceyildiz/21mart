namespace ApiProject.Models.DTOs;

public class CafeteriaOrderResponseDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    /// <summary>Detay tabloları için ISO tarih (UTC).</summary>
    public DateTime CreatedAtUtc { get; set; }
    public List<CafeteriaOrderItemResponseDto> Items { get; set; } = new();
    public decimal TotalPrice { get; set; }
    public string PickupTime { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}

public class CafeteriaOrderItemResponseDto
{
    public int MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// Kullanıcının kasiyerde "Ödenmedi" olarak işaretlenmiş siparişleri (borç takibi).
/// </summary>
public class MyUnpaidOrdersSummaryDto
{
    public int Count { get; set; }
    /// <summary>NotPaid siparişlerin toplam tutarı.</summary>
    public decimal TotalDebt { get; set; }
    /// <summary>İzin verilen maksimum açık NotPaid kayıt sayısı.</summary>
    public int UnpaidLimit { get; set; }
    public List<CafeteriaOrderResponseDto> Orders { get; set; } = new();
}

/// <summary>Kasiyer üst bilgi: limitteki müşteri ve açık borç özeti.</summary>
public class CashierUnpaidRiskOverviewDto
{
    /// <summary>NotPaid sayısı &gt;= limit olan farklı kullanıcı sayısı.</summary>
    public int UsersAtOrOverLimit { get; set; }
    /// <summary>Sistemdeki toplam NotPaid sipariş adedi.</summary>
    public int TotalOpenNotPaidOrders { get; set; }
    public int UnpaidLimit { get; set; }
}
