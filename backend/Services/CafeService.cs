using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface ICafeService
{
    Task<List<MenuItem>> GetMenuItemsAsync();
    Task<Order> CreateOrderAsync(int userId, CreateOrderDto createOrderDto);
    Task<List<Order>> GetUserOrdersAsync(int userId);
}

public class CafeService : ICafeService
{
    private readonly AppDbContext _context;

    public CafeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MenuItem>> GetMenuItemsAsync()
    {
        return await _context.MenuItems
            .Where(m => m.IsAvailable)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<Order> CreateOrderAsync(int userId, CreateOrderDto createOrderDto)
    {
        // Kullanıcının var olduğunu kontrol et
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new InvalidOperationException("Kullanıcı bulunamadı.");

        // Menü öğelerini kontrol et ve toplam tutarı hesapla
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
                Price = menuItem.Price // O anki fiyatı kaydet
            };

            totalAmount += menuItem.Price * itemDto.Quantity;
            orderItems.Add(orderItem);
        }

        // Siparişi oluştur
        var order = new Order
        {
            UserId = userId,
            UserType = user.Role == UserRole.Student ? OrderUserType.Student : OrderUserType.Staff,
            CreatedAt = DateTime.UtcNow,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Received,
            IsPaid = false,
            TotalAmount = totalAmount,
            OrderItems = orderItems
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // İlişkili verileri yükle
        await _context.Entry(order)
            .Collection(o => o.OrderItems)
            .Query()
            .Include(oi => oi.MenuItem)
            .LoadAsync();

        return order;
    }

    public async Task<List<Order>> GetUserOrdersAsync(int userId)
    {
        return await _context.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
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

