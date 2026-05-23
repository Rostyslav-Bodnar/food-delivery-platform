using System.Collections.Concurrent;
using System.Text.Json;
using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging;

public sealed class UserServiceRpcClient : IAsyncDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly IChannel _channel;
    private readonly string _replyQueueName;
    private readonly ConcurrentDictionary<string, IRpcCallback> _pending = new();

    private UserServiceRpcClient(IChannel channel, string replyQueueName)
    {
        _channel = channel;
        _replyQueueName = replyQueueName;
    }

    public static async Task<UserServiceRpcClient> CreateAsync(
        IConnection connection,
        CancellationToken cancellationToken = default)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        var queueOk = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: cancellationToken);

        var client = new UserServiceRpcClient(channel, queueOk.QueueName);
        await client.StartConsumingAsync(cancellationToken);
        return client;
    }

    private async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += (_, ea) =>
        {
            var correlationId = ea.BasicProperties.CorrelationId;
            if (correlationId is not null && _pending.TryRemove(correlationId, out var callback))
            {
                callback.Complete(ea.Body);
            }
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(
            queue: _replyQueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    public Task<GetAccountResponse> GetAccountAsync(
        GetAccountRequest request, CancellationToken cancellationToken = default)
        => CallAsync<GetAccountRequest, GetAccountResponse>(
            "user.getaccount", request, cancellationToken);

    public Task<GetBusinessAccountResponse> GetBusinessAccountAsync(
        GetBusinessAccountRequest request, CancellationToken cancellationToken = default)
        => CallAsync<GetBusinessAccountRequest, GetBusinessAccountResponse>(
            "user.getbussinessaccount", request, cancellationToken);

    public Task<GetCustomerAccountResponse> GetCustomerAccountAsync(
        GetCustomerAccountRequest request, CancellationToken cancellationToken = default)
        => CallAsync<GetCustomerAccountRequest, GetCustomerAccountResponse>(
            "user.getcustomeraccount", request, cancellationToken);

    private Task<TResponse> CallAsync<TRequest, TResponse>(
        string routingKey,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new RpcCallback<TResponse>(tcs, routingKey);
        _pending[correlationId] = callback;

        var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(DefaultTimeout);

        var registration = timeoutCts.Token.Register(() =>
        {
            if (_pending.TryRemove(correlationId, out var orphan))
            {
                orphan.Fail(cancellationToken.IsCancellationRequested
                    ? new OperationCanceledException(cancellationToken)
                    : new TimeoutException($"RPC '{routingKey}' timed out after {DefaultTimeout.TotalSeconds:N0}s"));
            }
        });

        tcs.Task.ContinueWith(_ =>
        {
            registration.Dispose();
            timeoutCts.Dispose();
        }, TaskScheduler.Default);

        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(request);

        _ = PublishAsync(routingKey, props, body, correlationId);
        return tcs.Task;
    }

    private async Task PublishAsync(string routingKey, BasicProperties props, byte[] body, string correlationId)
    {
        try
        {
            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body);
        }
        catch (Exception ex)
        {
            if (_pending.TryRemove(correlationId, out var callback))
            {
                callback.Fail(ex);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (id, callback) in _pending)
        {
            if (_pending.TryRemove(id, out _))
            {
                callback.Fail(new ObjectDisposedException(nameof(UserServiceRpcClient)));
            }
        }

        try
        {
            await _channel.CloseAsync();
        }
        catch
        {
            // best-effort close; underlying connection is owned elsewhere
        }
        await _channel.DisposeAsync();
    }

    private interface IRpcCallback
    {
        void Complete(ReadOnlyMemory<byte> body);
        void Fail(Exception exception);
    }

    private sealed class RpcCallback<T>(TaskCompletionSource<T> tcs, string routingKey) : IRpcCallback
    {
        public void Complete(ReadOnlyMemory<byte> body)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(body.Span)
                    ?? throw new InvalidOperationException(
                        $"RPC '{routingKey}' returned an empty or null payload");

                // UserService RPC consumers always reply, using a default-
                // populated response (AccountId == Guid.Empty) as the
                // "not found" sentinel so callers' awaited TCS never hangs.
                // Translate that sentinel back to null here so the existing
                // `result == null` / `?? throw NotFoundException` patterns
                // in DishService continue to detect not-found correctly.
                if (result is GetAccountResponse { AccountId: var id } && id == Guid.Empty)
                {
                    tcs.TrySetResult(default!);
                    return;
                }

                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        public void Fail(Exception exception) => tcs.TrySetException(exception);
    }
}
