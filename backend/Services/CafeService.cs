using ApiProject.Data;
using ApiProject.Models;
using ApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Services;

public interface ICafeService
{
    Task<List<MenuItem>> GetMenuItemsAsync();
    Task<Order> CreateOrderAsync(int studentId, CreateOrderDto createOrderDto);
    Task<List<Order>> GetStudentOrdersAsync(int studentId);
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

    public async Task<Order> CreateOrderAsync(int studentId, CreateOrderDto createOrderDto)
    {
        // Öğrencinin var olduğunu kontrol et
        var student = await _context.Users.FindAsync(studentId);
        if (student == null)
            throw new InvalidOperationException("Öğrenci bulunamadı.");

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
            StudentId = studentId,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
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

    public async Task<List<Order>> GetStudentOrdersAsync(int studentId)
    {
        return await _context.Orders
            .Where(o => o.StudentId == studentId)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}

