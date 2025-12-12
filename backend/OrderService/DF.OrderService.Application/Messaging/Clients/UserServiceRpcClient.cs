using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Clients;

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
                    case TaskCompletionSource<GetBusinessAccountResponse> tcs2:
                        var response2 = JsonSerializer.Deserialize<GetBusinessAccountResponse>(json);
                        if (response2 != null) tcs2.SetResult(response2);
                        break;
                    case TaskCompletionSource<GetCustomerAccountResponse> tcs2:
                        var response3 = JsonSerializer.Deserialize<GetCustomerAccountResponse>(json);
                        if (response3 != null) tcs2.SetResult(response3);
                        break;
                    case TaskCompletionSource<GetCourierAccountResponse> tcs2:
                        var response4 = JsonSerializer.Deserialize<GetCourierAccountResponse>(json);
                        if (response4 != null) tcs2.SetResult(response4);
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

    public Task<GetBusinessAccountResponse> GetBusinessAccountAsync(GetBusinessAccountRequest request)
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
        var tcs = new TaskCompletionSource<GetBusinessAccountResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "user.getbussinessaccount",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);

        return tcs.Task;
    }
    
    public Task<GetCustomerAccountResponse> GetCustomerAccountAsync(GetCustomerAccountRequest request)
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
        var tcs = new TaskCompletionSource<GetCustomerAccountResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "user.getcustomeraccount",
            mandatory: false,
            basicProperties: props,
            body: messageBytes);

        return tcs.Task;
    }

    public Task<GetCourierAccountResponse> GetCourierAccountAsync(GetCourierAccountRequest request)
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
        var tcs = new TaskCompletionSource<GetCourierAccountResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        callbackMapper[correlationId] = tcs;

        channel.BasicPublishAsync(
            exchange: "",
            routingKey: "user.getcourieraccount",
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
