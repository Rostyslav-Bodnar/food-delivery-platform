using DF.Contracts.EventDriven;
using DF.PaymentService.Application.CommandHandlers;
using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Common.Interfaces;
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
        
        eventBus.Subscribe<OrderCreatedEvent>(async order =>
        {
            using var scope = scopeFactory.CreateScope();

            var createPayment = scope.ServiceProvider.GetRequiredService<CreatePaymentCommandHandler>();
            await createPayment.Handle(new CreatePaymentCommand(
                order.OrderId, order.TotalPrice, order.Currency,
                Enum.Parse<PaymentMethod>(order.PaymentMethod)), stoppingToken);

            if (order.PaymentMethod == PaymentMethod.Online.ToString())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
                var stripe = scope.ServiceProvider.GetRequiredService<IStripeService>();
                var payment = await repo.GetByOrderIdAsync(order.OrderId, stoppingToken);
                if (payment != null && string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
                {
                    // 🔽 якщо 1 ресторан — використовуємо Destination
                    if (!string.IsNullOrWhiteSpace(order.BusinessStripeAccountId))
                    {
                        var result = await stripe.CreateDestinationPaymentIntentAsync(
                            payment,
                            order.BusinessStripeAccountId,
                            platformFeePercent: 0.05m,
                            ct: stoppingToken);

                        payment.SetStripeSecrets(result.PaymentIntentId, result.ClientSecret);
                        payment.MarkDestinationFlow();            // <— позначаємо
                        payment.SetExpiration(DateTime.UtcNow.AddMinutes(15));

                        await repo.SaveChangesAsync(stoppingToken);
                    }
                    else
                    {
                        // fallback: старий шлях (звичайний PI на платформу)
                        var result = await stripe.CreatePaymentIntentAsync(payment, stoppingToken);
                        payment.SetStripeSecrets(result.PaymentIntentId, result.ClientSecret);
                        payment.SetExpiration(DateTime.UtcNow.AddMinutes(15));
                        await repo.SaveChangesAsync(stoppingToken);
                    }
                }
            }
        });


        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}