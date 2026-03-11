using DF.PaymentService.Application.CommandHandlers;
using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.IntegrationEvents;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DF.PaymentService.Infrastructure.Messaging.Consumers;

public class OrderCreatedConsumer(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Підпишемося один раз на старті
        eventBus.Subscribe<OrderCreatedIntegrationEvent>(async order =>
        {
            using var scope = scopeFactory.CreateScope();

            var handler = scope.ServiceProvider.GetRequiredService<CreatePaymentCommandHandler>();

            var command = new CreatePaymentCommand(
                order.OrderId, order.Amount, order.Currency,
                Enum.Parse<PaymentMethod>(order.PaymentMethod));

            await handler.Handle(command, stoppingToken);

            if (order.PaymentMethod == PaymentMethod.Online.ToString())
            {
                var repository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                var stripeService = scope.ServiceProvider.GetRequiredService<IStripeService>();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var payment = await repository.GetByOrderIdAsync(order.OrderId, stoppingToken);
                if (payment != null && string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
                {
                    try
                    {
                        var result = await stripeService.CreatePaymentIntentAsync(payment, stoppingToken);
                        payment.SetStripeSecrets(result.PaymentIntentId, result.ClientSecret);
                        payment.SetExpiration(DateTime.UtcNow.AddMinutes(15));
                        await repository.SaveChangesAsync(stoppingToken);
                    }
                    catch
                    {
                        var exists = await db.PaymentTasks
                            .AnyAsync(t => t.PaymentId == payment.Id
                                           && t.Type == PaymentTaskType.CreateStripePaymentIntent
                                           && t.ProcessedOnUtc == null, stoppingToken);

                        if (!exists)
                        {
                            await db.PaymentTasks.AddAsync(
                                PaymentTask.Create(payment.Id, PaymentTaskType.CreateStripePaymentIntent),
                                stoppingToken);
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}