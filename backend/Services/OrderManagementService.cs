using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface IOrderManagementService
{
    Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId);
    /// <param name="userSearch">Dolu ve 2+ karakter ise: eşleşen kullanıcı(lar)ın tüm sipariş geçmişi (iptal/ödendi dahil). Boşsa: kasiyer kuyruğu.</param>
    Task<List<OrderResponseDto>> GetCashierOrdersAsync(OrderStatus? status, bool? isPaid, string? userSearch);
    Task<OrderResponseDto?> ApproveAsync(int orderId);
    Task<OrderResponseDto?> PreparingAsync(int orderId);
    Task<OrderResponseDto?> ReadyAsync(int orderId);
    Task<OrderResponseDto?> PaidAsync(int orderId);
    Task<OrderResponseDto?> NotPaidAsync(int orderId);
    Task<OrderResponseDto?> CancelAsync(int orderId);
}

public class OrderManagementService : IOrderManagementService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public OrderManagementService(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<OrderResponseDto>> GetCashierOrdersAsync(OrderStatus? status, bool? isPaid, string? userSearch)
    {
        var term = userSearch?.Trim();
        if (!string.IsNullOrEmpty(term) && term.Length >= 2)
            return await GetCashierOrdersByUserSearchAsync(term);

        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);
        else
            // Genel kuyruk: "Hepsi" durumunda iptalleri gösterme (aktif iş listesi)
            query = query.Where(o => o.Status != OrderStatus.Cancelled);

        if (isPaid.HasValue)
            query = query.Where(o => o.IsPaid == isPaid.Value);

        var orders = await query
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapOrder).ToList();
    }

    /// <summary>
    /// Ad, e-posta veya öğrenci numarası ile eşleşen kullanıcıların tüm siparişleri (tamamlanmış, iptal, ödenmedi vb.).
    /// </summary>
    private async Task<List<OrderResponseDto>> GetCashierOrdersByUserSearchAsync(string term)
    {
        var t = term.ToLowerInvariant();
        var userIds = await _context.Users
            .AsNoTracking()
            .Where(u =>
                u.Name.ToLower().Contains(t) ||
                u.Email.ToLower().Contains(t) ||
                (u.StudentNo != null && u.StudentNo.ToLower().Contains(t)))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync();

        if (userIds.Count == 0)
            return new List<OrderResponseDto>();

        var orders = await _context.Orders
            .Where(o => userIds.Contains(o.UserId))
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapOrder).ToList();
    }

    public async Task<OrderResponseDto?> ApproveAsync(int orderId)
    {
        var order = await FindOrder(orderId);
        if (order == null) return null;

        EnsureNotCancelled(order);
        if (order.Status != OrderStatus.Received)
            throw new InvalidOperationException("Sipariş sadece RECEIVED durumundayken onaylanabilir.");

        order.Status = OrderStatus.Approved;
        order.ApprovedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SendOrderNotificationAsync(order, NotificationType.OrderApproved,
            "Sipariş Onaylandı",
            $"#{order.OrderNumber} numaralı siparişiniz onaylandı.");

        return MapOrder(order);
    }

    public async Task<OrderResponseDto?> PreparingAsync(int orderId)
    {
        var order = await FindOrder(orderId);
        if (order == null) return null;

        EnsureNotCancelled(order);
        if (order.Status != OrderStatus.Received && order.Status != OrderStatus.Approved)
            throw new InvalidOperationException("Sipariş RECEIVED/APPROVED durumundayken PREPARING yapılabilir.");

        order.Status = OrderStatus.Preparing;
        if (order.ApprovedAt == null)
            order.ApprovedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await SendOrderNotificationAsync(order, NotificationType.OrderPreparing,
            "Sipariş Hazırlanıyor",
            $"#{order.OrderNumber} numaralı siparişiniz hazırlanmaya başlandı.");

        return MapOrder(order);
    }

    public async Task<OrderResponseDto?> ReadyAsync(int orderId)
    {
        var order = await FindOrder(orderId);
        if (order == null) return null;

        EnsureNotCancelled(order);
        if (order.Status != OrderStatus.Preparing)
            throw new InvalidOperationException("Sipariş sadece PREPARING durumundayken READY yapılabilir.");

        order.Status = OrderStatus.Ready;
        order.ReadyAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SendOrderNotificationAsync(order, NotificationType.OrderReady,
            "Sipariş Hazır",
            $"#{order.OrderNumber} numaralı siparişiniz hazır! Teslim alabilirsiniz.");

        return MapOrder(order);
    }

    public async Task<OrderResponseDto?> PaidAsync(int orderId)
    {
        var order = await FindOrder(orderId);
        if (order == null) return null;

        EnsureNotCancelled(order);
        if (order.Status != OrderStatus.Ready)
            throw new InvalidOperationException("Sipariş PAID durumuna geçmeden önce READY olmalıdır.");

        order.Status = OrderStatus.Paid;
        order.IsPaid = true;
        order.PaidAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await SendOrderNotificationAsync(order, NotificationType.OrderPaid,
            "Sipariş Teslim Edildi",
            $"#{order.OrderNumber} numaralı siparişiniz teslim edildi. Afiyet olsun!");

        return MapOrder(order);
    }

    public async Task<OrderResponseDto?> NotPaidAsync(int orderId)
    {
        var order = await FindOrder(orderId);
        if (order == null) return null;

        EnsureNotCancelled(order);
        if (order.Status != OrderStatus.Ready)
            throw new InvalidOperationException("Sipariş NOT_PAID durumuna geçmeden önce READY olmalıdır.");

        order.Status = OrderStatus.NotPaid;
        order.IsPaid = false;
        await _context.SaveChangesAsync();

        await SendOrderNotificationAsync(order, NotificationType.OrderNotPaid,
            "Sipariş Ödenmedi",
            $"#{order.OrderNumber} numaralı siparişiniz ödenmedi olarak işaretlendi.");

        return MapOrder(order);
    }

    public async Task<OrderResponseDto?> CancelAsync(int orderId)
    {
        var order = await FindOrder(orderId);
        if (order == null) return null;

        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Sipariş zaten iptal edilmiş.");

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();

        await SendOrderNotificationAsync(order, NotificationType.OrderCancelled,
            "Sipariş İptal Edildi",
            $"#{order.OrderNumber} numaralı siparişiniz iptal edildi.");

        return MapOrder(order);
    }

    private async Task<Order?> FindOrder(int orderId)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    private static void EnsureNotCancelled(Order order)
    {
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("İptal edilmiş siparişin durumu değiştirilemez.");
    }

    private async Task SendOrderNotificationAsync(Order order, NotificationType type, string title, string message)
    {
        try
        {
            var email = order.User?.Email ?? "";
            if (string.IsNullOrEmpty(email)) return;

            await _notificationService.SendNotificationAsync(
                title, message, type, email, order.UserId);
        }
        catch
        {
            // Bildirim gönderilemese bile sipariş işlemi başarısız olmasın
        }
    }

    private static OrderResponseDto MapOrder(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.User?.Name,
            CustomerEmail = order.User?.Email,
            StudentNo = order.User?.StudentNo,
            UserType = order.UserType,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            IsPaid = order.IsPaid,
            CreatedAt = order.CreatedAt,
            ApprovedAt = order.ApprovedAt,
            ReadyAt = order.ReadyAt,
            PaidAt = order.PaidAt,
            PickupTime = order.PickupTime,
            Note = order.Note,
            OrderItems = order.OrderItems.Select(oi => new OrderItemResponseDto
            {
                Id = oi.Id,
                MenuItemId = oi.MenuItemId,
                MenuItemName = oi.MenuItem?.Name ?? string.Empty,
                Quantity = oi.Quantity,
                Price = oi.Price
            }).ToList()
        };
    }
}

