using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface ICafeService
{
    Task<List<MenuItem>> GetMenuItemsAsync();
    Task<OrderResponseDto> CreateOrderAsync(int studentId, CreateOrderDto createOrderDto);
    Task<List<OrderResponseDto>> GetStudentOrdersAsync(int studentId);
}

public class CafeService : ICafeService
{
    private readonly AppDbContext _context;

    public CafeService(AppDbContext context)
    {
        _context = context;
    }

    private static readonly Dictionary<OrderStatus, string> StatusToTurkish = new()
    {
        { OrderStatus.Pending, "Onaylanması Bekleniyor" },
        { OrderStatus.Preparing, "Hazırlanıyor" },
        { OrderStatus.Ready, "Hazırlandı" },
        { OrderStatus.Delivered, "Teslim Alındı" },
        { OrderStatus.Cancelled, "İptal Edildi" },
    };

    public async Task<List<MenuItem>> GetMenuItemsAsync()
    {
        return await _context.MenuItems
            .Where(m => m.IsAvailable)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<OrderResponseDto> CreateOrderAsync(int studentId, CreateOrderDto createOrderDto)
    {
        var student = await _context.Users.FindAsync(studentId);
        if (student == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı.");

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
            StudentId = studentId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
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

        return MapToResponseDto(order);
    }

    public async Task<List<OrderResponseDto>> GetStudentOrdersAsync(int studentId)
    {
        var orders = await _context.Orders
            .Where(o => o.StudentId == studentId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        return orders.Select(MapToResponseDto).ToList();
    }

    private OrderResponseDto MapToResponseDto(Order order)
    {
        var now = DateTime.UtcNow;
        var diff = now - order.OrderDate;
        string createdAt;

        if (diff.TotalMinutes < 1)
            createdAt = "Az önce";
        else if (diff.TotalMinutes < 60)
            createdAt = $"{(int)diff.TotalMinutes} dk önce";
        else if (diff.TotalHours < 24)
            createdAt = $"Bugün, {order.OrderDate.ToLocalTime():HH:mm}";
        else if (diff.TotalHours < 48)
            createdAt = $"Dün, {order.OrderDate.ToLocalTime():HH:mm}";
        else
            createdAt = order.OrderDate.ToLocalTime().ToString("dd.MM.yyyy, HH:mm");

        return new OrderResponseDto
        {
            Id = order.Id,
            Items = order.OrderItems.Select(oi => new OrderItemResponseDto
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
}
