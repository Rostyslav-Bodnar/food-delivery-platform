using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services;
using DF.OrderService.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Consumers;

public class LocationsCreatedConsumer(IConnection connection, IServiceScopeFactory scopeFactory) : IConsumer
{
    public void Start()
    {
        var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        channel.ExchangeDeclareAsync("trackingservice", ExchangeType.Fanout, durable: true)
            .GetAwaiter().GetResult();

        var queueOk = channel.QueueDeclareAsync("orders.locationscreated", durable: true)
            .GetAwaiter().GetResult();

        channel.QueueBindAsync(queueOk.QueueName, "trackingservice", "")
            .GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += HandleMessage;
        channel.BasicConsumeAsync(queueOk.QueueName, autoAck: true, consumer: consumer)
            .GetAwaiter().GetResult();

        Console.WriteLine("LocationsCreatedConsumer started");
    }

    private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var evt = JsonSerializer.Deserialize<LocationsCreatedForOrder>(json);

        if (evt == null) return;
        
        using var scope = scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var distanceService = scope.ServiceProvider.GetRequiredService<IDistanceService>();

        // 1️⃣ Отримуємо дистанцію між закладом і клієнтом через OSRM
        var distanceKm = await distanceService.GetDistanceKmAsync(
            evt.DeliverFromId.Latitude, evt.DeliverFromId.Longitude,
            evt.DeliverTo.Latitude, evt.DeliverTo.Longitude);
        
        var order = await orderRepository.Get(evt.OrderId);
        if (order == null) return;
            
        var profit = ProfitService.Calculate(order.TotalPrice, distanceKm);

        order.DeliverToId = evt.DeliverTo.Id;
        order.DeliverFromId = evt.DeliverFromId.Id;
        order.Profit = profit;
        await orderRepository.Update(order);
    }
}

