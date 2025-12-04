using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests;
using DF.Contracts.RPC.Responses;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging;

public class UserServiceRpcClient : IDisposable
{
    private readonly IConnection connection;
    private readonly IChannel channel;
    private readonly string replyQueueName;
    private readonly AsyncEventingBasicConsumer consumer;
    private readonly ConcurrentDictionary<string, object> callbackMapper = new();

    public UserServiceRpcClient(IConnection connection)
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
                    case TaskCompletionSource<GetAccountResponse> tcs:
                        var response1 = JsonSerializer.Deserialize<GetAccountResponse>(json);
                        if (response1 != null) tcs.SetResult(response1);
                        break;

                    case TaskCompletionSource<GetBusinessAccountDetailsResponse> tcs2:
                        var response2 = JsonSerializer.Deserialize<GetBusinessAccountDetailsResponse>(json);
                        if (response2 != null) tcs2.SetResult(response2);
                        break;
                }
            }

            await Task.Yield();
        };

        channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: consumer).GetAwaiter().GetResult();
    }

    public Task<GetAccountResponse> GetAccountAsync(GetAccountRequest request)
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
        var tcs = new TaskCompletionSource<GetAccountResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "user.getaccount",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);

        return tcs.Task;
    }

    public Task<GetBusinessAccountDetailsResponse> GetBusinessAccountAsync(GetBusinessAccountDetailsRequest request)
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
        var tcs = new TaskCompletionSource<GetBusinessAccountDetailsResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "user.getbussinessaccount",
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
