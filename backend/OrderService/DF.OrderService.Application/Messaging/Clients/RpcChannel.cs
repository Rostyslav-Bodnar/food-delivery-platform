using System.Collections.Concurrent;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Clients;

/// <summary>
/// Shared backbone for the OrderService's RPC clients. Owns one dedicated channel,
/// one exclusive reply queue, and a generic correlation-id dispatcher with timeout
/// + cancellation. Does NOT dispose the shared IConnection on dispose.
/// </summary>
public sealed class RpcChannel : IAsyncDisposable
{
    private readonly IChannel _channel;
    private readonly string _replyQueueName;
    private readonly ConcurrentDictionary<string, IRpcCallback> _pending = new();
    private readonly TimeSpan _defaultTimeout;
    private readonly string _name;

    private RpcChannel(IChannel channel, string replyQueueName, TimeSpan defaultTimeout, string name)
    {
        _channel = channel;
        _replyQueueName = replyQueueName;
        _defaultTimeout = defaultTimeout;
        _name = name;
    }

    public static async Task<RpcChannel> CreateAsync(
        IConnection connection,
        string name,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var queueOk = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false, exclusive: true, autoDelete: true,
            cancellationToken: cancellationToken);

        var rpc = new RpcChannel(channel, queueOk.QueueName, timeout ?? TimeSpan.FromSeconds(10), name);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, ea) =>
        {
            var id = ea.BasicProperties.CorrelationId;
            if (id is not null && rpc._pending.TryRemove(id, out var cb))
                cb.Complete(ea.Body);
            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(rpc._replyQueueName, autoAck: true, consumer: consumer,
            cancellationToken: cancellationToken);

        return rpc;
    }

    public Task<TResponse> CallAsync<TRequest, TResponse>(
        string routingKey, TRequest request, CancellationToken cancellationToken = default)
        where TResponse : class
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<TResponse?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new RpcCallback<TResponse>(tcs, $"{_name}:{routingKey}");
        _pending[correlationId] = callback;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_defaultTimeout);

        var reg = cts.Token.Register(() =>
        {
            if (_pending.TryRemove(correlationId, out var orphan))
            {
                orphan.Fail(cancellationToken.IsCancellationRequested
                    ? new OperationCanceledException(cancellationToken)
                    : new TimeoutException($"RPC '{_name}:{routingKey}' timed out after {_defaultTimeout.TotalSeconds:N0}s"));
            }
        });

        tcs.Task.ContinueWith(_ => { reg.Dispose(); cts.Dispose(); }, TaskScheduler.Default);

        var props = new BasicProperties
        {
            CorrelationId = correlationId,
            ReplyTo = _replyQueueName
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(request);

        _ = PublishAsync(routingKey, props, body, correlationId);

        // Convert TResponse? back to TResponse — the underlying TCS holds nullable so we can
        // hand back null for sentinel/not-found responses, then materialize in the caller.
        return tcs.Task!;
    }

    private async Task PublishAsync(string routingKey, BasicProperties props, byte[] body, string correlationId)
    {
        try
        {
            await _channel.BasicPublishAsync(
                exchange: string.Empty, routingKey: routingKey,
                mandatory: false, basicProperties: props, body: body);
        }
        catch (Exception ex)
        {
            if (_pending.TryRemove(correlationId, out var cb))
                cb.Fail(ex);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var (id, cb) in _pending)
            if (_pending.TryRemove(id, out _))
                cb.Fail(new ObjectDisposedException(nameof(RpcChannel)));

        try { await _channel.CloseAsync(); } catch { /* best-effort */ }
        await _channel.DisposeAsync();
    }

    private interface IRpcCallback
    {
        void Complete(ReadOnlyMemory<byte> body);
        void Fail(Exception exception);
    }

    private sealed class RpcCallback<T>(TaskCompletionSource<T?> tcs, string label) : IRpcCallback where T : class
    {
        public void Complete(ReadOnlyMemory<byte> body)
        {
            try
            {
                var result = JsonSerializer.Deserialize<T>(body.Span);
                // result == null means the consumer replied with an empty payload —
                // treated as "not found" sentinel rather than a hang.
                tcs.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(new InvalidOperationException(
                    $"Failed to deserialize RPC response for {label}", ex));
            }
        }

        public void Fail(Exception exception) => tcs.TrySetException(exception);
    }
}
