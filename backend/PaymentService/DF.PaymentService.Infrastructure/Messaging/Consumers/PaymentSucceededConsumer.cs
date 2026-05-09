using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DF.PaymentService.Infrastructure.Messaging.Consumers;

public class PaymentSucceededConsumer(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<PaymentSucceededEvent>(async evt =>
        {
            using var scope = scopeFactory.CreateScope();
            var courierPayoutService = scope.ServiceProvider.GetRequiredService<ICourierPayoutService>();
            await courierPayoutService.ReleaseHeldEarningAsync(evt.OrderId, stoppingToken);
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
