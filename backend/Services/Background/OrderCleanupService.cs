using ApiProject.Data;
using ApiProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiProject.Services.Background;

public class OrderCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); // Her 1 dakikada bir kontrol et

    public OrderCleanupService(IServiceProvider serviceProvider, ILogger<OrderCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldReadyOrdersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderCleanupService hatası oluştu");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CleanupOldReadyOrdersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Durumu 'Ready' olan ve 30 dakikayı geçen siparişleri bul
        var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
        
        var oldReadyOrders = await context.Orders
            .Where(o => o.Status == OrderStatus.Ready && o.OrderDate <= cutoffTime)
            .ToListAsync();

        if (oldReadyOrders.Any())
        {
            foreach (var order in oldReadyOrders)
            {
                order.Status = OrderStatus.Cancelled;
                _logger.LogInformation("Sipariş iptal edildi. OrderId: {OrderId}, ÖğrenciId: {StudentId}, Hazır Olma Tarihi: {OrderDate}", 
                    order.Id, order.StudentId, order.OrderDate);
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("{Count} adet sipariş otomatik olarak iptal edildi.", oldReadyOrders.Count);
        }
    }
}
