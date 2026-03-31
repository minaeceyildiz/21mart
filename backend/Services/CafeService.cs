using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface ICafeService
{
    Task<List<MenuItem>> GetMenuItemsAsync();
    Task<CafeteriaOrderResponseDto> CreateOrderAsync(int userId, CreateOrderDto createOrderDto);
    Task<List<CafeteriaOrderResponseDto>> GetUserOrdersAsync(int userId);
    /// <summary>Durumu NotPaid (Ödenmedi) olan siparişlerin sayısı ve listesi.</summary>
    Task<MyUnpaidOrdersSummaryDto> GetMyNotPaidOrdersAsync(int userId);
    Task<List<PickupTimeDensityDto>> GetPickupTimeDensityAsync();
}

public class CafeService : ICafeService
{
    /// <summary>Aynı anda en fazla bu kadar "Ödenmedi" siparişe izin verilir; üstünde yeni sipariş engellenir.</summary>
    public const int MaxNotPaidOrdersPerUser = 3;

    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public CafeService(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    private static readonly Dictionary<OrderStatus, string> StatusToTurkish = new()
    {
        { OrderStatus.Received, "Onaylanması Bekleniyor" },
        { OrderStatus.Approved, "Hazırlanıyor" },
        { OrderStatus.Preparing, "Hazırlanıyor" },
        { OrderStatus.Ready, "Hazırlandı" },
        { OrderStatus.Paid, "Teslim Alındı" },
        { OrderStatus.Cancelled, "İptal Edildi" },
        { OrderStatus.NotPaid, "Ödenmedi" },
    };

    public async Task<List<MenuItem>> GetMenuItemsAsync()
    {
        return await _context.MenuItems
            .Where(m => m.IsAvailable)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<CafeteriaOrderResponseDto> CreateOrderAsync(int userId, CreateOrderDto createOrderDto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı.");

        var notPaidCount = await _context.Orders
            .CountAsync(o => o.UserId == userId && o.Status == OrderStatus.NotPaid);

        if (notPaidCount >= MaxNotPaidOrdersPerUser)
        {
            throw new InvalidOperationException(
                "Ödenmemiş sipariş limitine ulaştınız. Yeni sipariş vermeden önce lütfen eski siparişlerinizin ödemesini tamamlayın.");
        }

        decimal totalAmount = 0;
        var orderItems = new List<OrderItem>();

        foreach (var itemDto in createOrderDto.OrderItems)
        {
            var menuItem = await _context.MenuItems.FindAsync(itemDto.MenuItemId);
            if (menuItem == null)
                throw new InvalidOperationException($"Menü öğesi bulunamadı. ID: {itemDto.MenuItemId}");

            if (!menuItem.IsAvailable)
                throw new InvalidOperationException($"{menuItem.Name} şu anda mevcut değil.");

            var orderItem = new OrderItem
            {
                MenuItemId = itemDto.MenuItemId,
                Quantity = itemDto.Quantity,
                Price = menuItem.Price
            };

            totalAmount += menuItem.Price * itemDto.Quantity;
            orderItems.Add(orderItem);
        }

        var order = new Order
        {
            UserId = userId,
            UserType = user.Role == UserRole.Student ? OrderUserType.Student : OrderUserType.Staff,
            CreatedAt = DateTime.UtcNow,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Received,
            IsPaid = false,
            TotalAmount = totalAmount,
            PickupTime = createOrderDto.PickupTime,
            Note = createOrderDto.Note,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        await _context.Entry(order)
            .Collection(o => o.OrderItems)
            .Query()
            .Include(oi => oi.MenuItem)
            .LoadAsync();

        try
        {
            await _notificationService.SendNotificationAsync(
                "Sipariş Alındı",
                $"#{order.OrderNumber} numaralı siparişiniz alındı. Onay bekleniyor.",
                NotificationType.OrderReceived,
                user.Email,
                userId);
        }
        catch { }

        return MapToResponseDto(order);
    }

    public async Task<List<CafeteriaOrderResponseDto>> GetUserOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToResponseDto).ToList();
    }

    public async Task<MyUnpaidOrdersSummaryDto> GetMyNotPaidOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId && o.Status == OrderStatus.NotPaid)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var totalDebt = orders.Sum(o => o.TotalAmount);

        return new MyUnpaidOrdersSummaryDto
        {
            Count = orders.Count,
            TotalDebt = totalDebt,
            UnpaidLimit = MaxNotPaidOrdersPerUser,
            Orders = orders.Select(MapToResponseDto).ToList()
        };
    }

    private CafeteriaOrderResponseDto MapToResponseDto(Order order)
    {
        var now = DateTime.UtcNow;
        var diff = now - order.CreatedAt;
        string createdAt;

        if (diff.TotalMinutes < 1)
            createdAt = "Az önce";
        else if (diff.TotalMinutes < 60)
            createdAt = $"{(int)diff.TotalMinutes} dk önce";
        else if (diff.TotalHours < 24)
            createdAt = $"Bugün, {order.CreatedAt.ToLocalTime():HH:mm}";
        else if (diff.TotalHours < 48)
            createdAt = $"Dün, {order.CreatedAt.ToLocalTime():HH:mm}";
        else
            createdAt = order.CreatedAt.ToLocalTime().ToString("dd.MM.yyyy, HH:mm");

        return new CafeteriaOrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CreatedAtUtc = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new CafeteriaOrderItemResponseDto
            {
                MenuItemId = oi.MenuItemId,
                Name = oi.MenuItem?.Name ?? "Bilinmeyen Ürün",
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList(),
            TotalPrice = order.TotalAmount,
            PickupTime = order.PickupTime ?? "",
            Note = order.Note,
            Status = StatusToTurkish.GetValueOrDefault(order.Status, "Bilinmeyen"),
            CreatedAt = createdAt
        };
    }

    public async Task<List<PickupTimeDensityDto>> GetPickupTimeDensityAsync()
    {
        var today = DateTime.UtcNow.Date;

        var density = await _context.Orders
            .Where(o => o.CreatedAt >= today
                        && o.PickupTime != null
                        && o.Status != OrderStatus.Cancelled
                        && o.Status != OrderStatus.Paid
                        && o.Status != OrderStatus.NotPaid)
            .GroupBy(o => o.PickupTime!)
            .Select(g => new PickupTimeDensityDto
            {
                Time = g.Key,
                OrderCount = g.Count()
            })
            .ToListAsync();

        return density;
    }

    private static string GenerateOrderNumber()
    {
        // Basit, okunabilir ve çakışma riski düşük bir sipariş numarası.
        // Örn: 20260318-143012-4821
        var utcNow = DateTime.UtcNow;
        var suffix = Random.Shared.Next(1000, 9999);
        return $"{utcNow:yyyyMMdd-HHmmss}-{suffix}";
    }
}
