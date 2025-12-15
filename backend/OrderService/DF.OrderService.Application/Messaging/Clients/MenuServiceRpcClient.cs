using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Clients;

public class MenuServiceRpcClient : IDisposable
{
    private readonly IConnection connection;
    private readonly IChannel channel;
    private readonly string replyQueueName;
    private readonly AsyncEventingBasicConsumer consumer;
    private readonly ConcurrentDictionary<string, object> callbackMapper = new();

    public MenuServiceRpcClient(IConnection connection)
    {
        this.connection = connection;
        channel = this.connection.CreateChannelAsync().GetAwaiter().GetResult();

        var queueOk = channel.QueueDeclareAsync(queue: "",
            durable: false,
            exclusive: true,
            autoDelete: true).GetAwaiter().GetResult();
        replyQueueName = queueOk.QueueName;

        consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (correlationId != null && callbackMapper.TryRemove(correlationId, out var tcsObj))
            {
                var body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);

                switch (tcsObj)
                {
                    case TaskCompletionSource<GetDishesResponse> tcs:
                        var response1 = JsonSerializer.Deserialize<GetDishesResponse>(json);
                        if (response1 != null) tcs.SetResult(response1);
                        break;

                    case TaskCompletionSource<GetDishResponse> tcs2:
                        var response2 = JsonSerializer.Deserialize<GetDishResponse>(json);
                        if (response2 != null) tcs2.SetResult(response2);
                        break;
                }
            }

            await Task.Yield();
        };

        channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: consumer).GetAwaiter().GetResult();
    }

    public Task<GetDishesResponse> GetDishesAsync(GetDishesRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = replyQueueName
        };
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;

        var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        var tcs = new TaskCompletionSource<GetDishesResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "menu.getdishes",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);

        return tcs.Task;
    }

    public Task<GetDishResponse> GetDishAsync(GetDishRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = replyQueueName
        };
        props.CorrelationId = correlationId;
        props.ReplyTo = replyQueueName;

        var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        var tcs = new TaskCompletionSource<GetDishResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "menu.getdish",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);

        return tcs.Task;
    }
    
    public void Dispose()
    {
        channel?.Dispose();
        connection?.Dispose();
    }
}
