using DF.Contracts.EventDriven;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DF.PaymentService.Infrastructure.Messaging.Consumers;

public class OrderDeliveredConsumer(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<OrderDeliveredEvent>(async evt =>
        {
            using var scope = scopeFactory.CreateScope();
            var courierPayoutService = scope.ServiceProvider.GetRequiredService<ICourierPayoutService>();

            await courierPayoutService.RegisterDeliveredOrderAsync(
                evt.OrderId,
                evt.CourierId,
                evt.CourierFee,
                evt.Currency,
                evt.DeliveredAtUtc,
                stoppingToken);
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
