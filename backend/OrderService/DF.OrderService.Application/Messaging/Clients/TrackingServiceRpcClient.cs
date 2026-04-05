using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Clients;

public class TrackingServiceRpcClient : IDisposable
{
    private readonly IConnection connection;
    private readonly IChannel channel;
    private readonly string replyQueueName;
    private readonly AsyncEventingBasicConsumer consumer;
    private readonly ConcurrentDictionary<string, object> callbackMapper = new();

    public TrackingServiceRpcClient(IConnection connection)
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
                    case TaskCompletionSource<GetLocationsResponse> tcs:
                        var response1 = JsonSerializer.Deserialize<GetLocationsResponse>(json);
                        if (response1 != null)
                            tcs.SetResult(response1);
                        else
                            tcs.SetException(new InvalidOperationException("Failed to deserialize GetLocationsResponse."));
                        break;
                    case TaskCompletionSource<GetBusinessLocationsResponse> tcs:
                        var response2 = JsonSerializer.Deserialize<GetBusinessLocationsResponse>(json);
                        if (response2 != null)
                            tcs.SetResult(response2);
                        else
                            tcs.SetException(new InvalidOperationException("Failed to deserialize GetBusinessLocationsResponse."));
                        break;
                }
            }

            await Task.Yield();
        };

        channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: consumer).GetAwaiter().GetResult();
    }

    public Task<GetLocationsResponse> GetLocationsAsync(GetLocationRequest request)
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
        var tcs = new TaskCompletionSource<GetLocationsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "tracking.getlocations",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);

        return tcs.Task;
    }
    
    public async Task<GetBusinessLocationsResponse> GetBusinessLocationsAsync(GetBusinessLocationsRequest request)
    {
        var correlationId = Guid.NewGuid().ToString();
        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = replyQueueName
        };

        var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        var tcs = new TaskCompletionSource<GetBusinessLocationsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        await channel.BasicPublishAsync(
            exchange: "",
            routingKey: "tracking.getbusinesslocations",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);
        
        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        if (completedTask != tcs.Task)
        {
            callbackMapper.TryRemove(correlationId, out _);
            throw new TimeoutException("RPC call timed out");
        }

        return await tcs.Task;
    }

    
    public void Dispose()
    {
        channel?.Dispose();
        connection?.Dispose();
    }
}
