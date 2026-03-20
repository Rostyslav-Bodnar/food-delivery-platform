using DF.Contracts.EventDriven;
using DF.PaymentService.Application.CommandHandlers;
using DF.PaymentService.Application.Commands;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Application.Repositories.Interfaces;
using DF.PaymentService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.Messaging.Consumers;

public class OrderCancelledConsumer(
    IServiceScopeFactory scopeFactory,
    IEventBus eventBus,
    ILogger<OrderCancelledConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        eventBus.Subscribe<OrderCancelledEvent>(async evt =>
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
            var cancelHandler = scope.ServiceProvider.GetRequiredService<CancelPaymentCommandHandler>();
            var refundHandler = scope.ServiceProvider.GetRequiredService<RefundPaymentCommandHandler>();
            var log = scope.ServiceProvider.GetRequiredService<ILogger<OrderCancelledConsumer>>();

            try
            {
                var payment = await repo.GetByOrderIdAsync(evt.OrderId, stoppingToken);
                if (payment is null)
                {
                    log.LogInformation("OrderCancelled: payment not found. OrderId={OrderId}", evt.OrderId);
                    return;
                }

                var method = ResolvePaymentMethod(evt, payment);

                if (payment.Status is PaymentStatus.Refunded or PaymentStatus.Cancelled)
                {
                    log.LogInformation(
                        "OrderCancelled: payment already finalized as {Status}. PaymentId={PaymentId}",
                        payment.Status, payment.Id);
                    return;
                }

                if (method == PaymentMethod.Online)
                {
                    switch (payment.Status)
                    {
                        case PaymentStatus.Succeeded:
                            // Повний рефанд (amount: null) при скасуванні замовлення
                            log.LogInformation(
                                "OrderCancelled: issuing refund for online payment. PaymentId={PaymentId}, OrderId={OrderId}",
                                payment.Id, evt.OrderId);

                            var refundCmd = new RefundPaymentCommand(payment.Id, Amount: null);
                            await refundHandler.Handle(refundCmd, stoppingToken);
                            break;

                        case PaymentStatus.Pending:
                        case PaymentStatus.RequiresAction:
                            log.LogInformation(
                                "OrderCancelled: canceling online payment. PaymentId={PaymentId}, Status={Status}",
                                payment.Id, payment.Status);

                            var cancelCmd = new CancelPaymentCommand(payment.Id, Reason: "order_canceled");
                            await cancelHandler.Handle(cancelCmd, stoppingToken);
                            break;

                        case PaymentStatus.Failed:
                            log.LogInformation(
                                "OrderCancelled: online payment is Failed. No action. PaymentId={PaymentId}", payment.Id);
                            break;

                        default:
                            log.LogInformation(
                                "OrderCancelled: online payment in status {Status}. No automatic action. PaymentId={PaymentId}",
                                payment.Status, payment.Id);
                            break;
                    }
                }
                else
                {
                    // Cash on Delivery (або інший не-Online)
                    switch (payment.Status)
                    {
                        case PaymentStatus.AwaitingCashCollection:
                        case PaymentStatus.Pending:
                            log.LogInformation(
                                "OrderCancelled: canceling non-online payment. PaymentId={PaymentId}, Status={Status}",
                                payment.Id, payment.Status);

                            var cancelCmd = new CancelPaymentCommand(payment.Id, Reason: "order_canceled_cod");
                            await cancelHandler.Handle(cancelCmd, stoppingToken);
                            break;

                        case PaymentStatus.Succeeded:
                            // Для CoD немає Stripe‑дій; бізнес-правило зазвичай — no‑op
                            log.LogInformation(
                                "OrderCancelled: CoD payment is Succeeded. No automatic action. PaymentId={PaymentId}",
                                payment.Id);
                            break;

                        case PaymentStatus.Failed:
                            // Немає що робити
                            log.LogInformation(
                                "OrderCancelled: CoD payment is Failed. No action. PaymentId={PaymentId}", payment.Id);
                            break;

                        default:
                            // Cancelled / Refunded або інші — no‑op
                            log.LogInformation(
                                "OrderCancelled: non-online payment in status {Status}. No action. PaymentId={PaymentId}",
                                payment.Status, payment.Id);
                            break;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Нормальне завершення сервісу
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OrderCancelled: unhandled error. OrderId={OrderId}", evt.OrderId);
            }
        });

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private static PaymentMethod ResolvePaymentMethod(OrderCancelledEvent evt, Payment payment)
    {
        if (!string.IsNullOrWhiteSpace(evt.PaymentMethod)
            && Enum.TryParse<PaymentMethod>(evt.PaymentMethod, ignoreCase: true, out var fromEvent))
        {
            return fromEvent;
        }
        return payment.Method;
    }
}