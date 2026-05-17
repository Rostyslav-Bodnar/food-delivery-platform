using DF.Contracts.EventDriven;
using DF.PaymentService.Application.CommandHandlers;
using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DF.PaymentService.Infrastructure.Messaging.Consumers;

public class OrderCreatedConsumer(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<OrderCreatedEvent>(async order =>
        {
            using var scope = scopeFactory.CreateScope();

            // CreatePaymentCommandHandler is now the single writer: it persists the Payment row
            // AND (for Online orders) stages a PaymentTask in the same DB transaction. The
            // StripeTaskProcessor (idempotent on payment.StripePaymentIntentId) is the only
            // place that actually calls Stripe — so message redelivery never creates a second PI.
            var createPayment = scope.ServiceProvider.GetRequiredService<CreatePaymentCommandHandler>();
            await createPayment.Handle(new CreatePaymentCommand(
                order.OrderId, order.TotalPrice, order.Currency,
                Enum.Parse<PaymentMethod>(order.PaymentMethod)), stoppingToken);
        });


        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
