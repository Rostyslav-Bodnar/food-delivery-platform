using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.OrderService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Consumers;

public class CourierPayoutCompletedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory
) : IConsumer
{
    private const string ExchangeName = "df.events";
    private const string QueueName = "orders.courier-payout-completed";
    private const string RoutingKey = "CourierPayoutCompletedEvent";

    public void Start()
    {
        var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true
        ).GetAwaiter().GetResult();

        channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        ).GetAwaiter().GetResult();

        channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey
        ).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<CourierPayoutCompletedEvent>(json);

                if (evt is not null)
                {
                    using var scope = scopeFactory.CreateScope();
                    var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                    var order = await orderRepository.Get(evt.OrderId);

                    if (order is not null && !order.CourierPaid)
                    {
                        order.CourierPaid = true;
                        await orderRepository.Update(order);
                    }
                }

                await channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch
            {
                await channel.BasicNackAsync(ea.DeliveryTag, false, true);
            }
        };

        channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        ).GetAwaiter().GetResult();

        Console.WriteLine("CourierPayoutCompletedConsumer started");
    }
}
