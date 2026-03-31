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
    private static readonly TimeSpan NotPaidCutoffLocalTime = new(17, 45, 0); // 17:45

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
                await MarkOrdersAsNotPaidAfterCutoffAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderCleanupService hatası oluştu");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task MarkOrdersAsNotPaidAfterCutoffAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // İş kuralı:
        // 17:45'e kadar "ödendi" yapılmayan siparişler, 17:45 sonrasında otomatik "ödenmedi" (NotPaid) olur.
        // Limit tanımına uyum için yalnızca READY durumundaki (ve hâlâ ödenmemiş) siparişleri dönüştürüyoruz.
        var nowUtc = DateTime.UtcNow;
        var turkeyTz = ResolveTurkeyTimeZone();

        var candidateReadyOrders = await context.Orders
            .Where(o => o.Status == OrderStatus.Ready && !o.IsPaid)
            .ToListAsync();

        if (!candidateReadyOrders.Any())
            return;

        var affectedCount = 0;
        foreach (var order in candidateReadyOrders)
        {
            var createdLocal = TimeZoneInfo.ConvertTimeFromUtc(order.CreatedAt, turkeyTz);
            var cutoffLocalDateTime = createdLocal.Date + NotPaidCutoffLocalTime;
            var cutoffUtc = TimeZoneInfo.ConvertTimeToUtc(cutoffLocalDateTime, turkeyTz);

            if (nowUtc >= cutoffUtc)
            {
                order.Status = OrderStatus.NotPaid;
                order.IsPaid = false;
                affectedCount++;
                _logger.LogInformation(
                    "Sipariş otomatik NotPaid yapıldı. OrderId: {OrderId}, UserId: {UserId}, CreatedAtUtc: {CreatedAtUtc}, CutoffUtc: {CutoffUtc}",
                    order.Id, order.UserId, order.CreatedAt, cutoffUtc);
            }
        }

        if (affectedCount > 0)
        {
            await context.SaveChangesAsync();
            _logger.LogInformation("{Count} adet READY sipariş, 17:45 kuralı ile NotPaid durumuna geçirildi.", affectedCount);
        }
    }

    private static TimeZoneInfo ResolveTurkeyTimeZone()
    {
        // Windows: "Turkey Standard Time", Linux/macOS: "Europe/Istanbul"
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
    }
}
