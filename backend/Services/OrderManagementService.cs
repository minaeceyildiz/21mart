using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface IOrderManagementService
{
    Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId);
    Task<List<OrderResponseDto>> GetCashierOrdersAsync(OrderStatus? status, bool? isPaid);
    Task<OrderResponseDto?> ApproveAsync(int orderId);
    Task<OrderResponseDto?> PreparingAsync(int orderId);
    Task<OrderResponseDto?> ReadyAsync(int orderId);
    Task<OrderResponseDto?> PaidAsync(int orderId);
}

public class OrderManagementService : IOrderManagementService
{
    private readonly AppDbContext _context;

    public OrderManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId)
    {
        var orders = await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapOrder).ToList();
    }

    public async Task<List<OrderResponseDto>> GetCashierOrdersAsync(OrderStatus? status, bool? isPaid)
    {
        var query = _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        if (isPaid.HasValue)
            query = query.Where(o => o.IsPaid == isPaid.Value);

        // Aktif sipariş mantığı: iptal dışındakiler
        query = query.Where(o => o.Status != OrderStatus.Cancelled);

        var orders = await query
            .OrderBy(o => o.CreatedAt)
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
        return MapOrder(order);
    }

    private async Task<Order?> FindOrder(int orderId)
    {
        return await _context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.MenuItem)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    private static void EnsureNotCancelled(Order order)
    {
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("İptal edilmiş siparişin durumu değiştirilemez.");
    }

    private static OrderResponseDto MapOrder(Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            UserType = order.UserType,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            IsPaid = order.IsPaid,
            CreatedAt = order.CreatedAt,
            ApprovedAt = order.ApprovedAt,
            ReadyAt = order.ReadyAt,
            PaidAt = order.PaidAt,
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

