using System.Text;
using System.Text.Json;
using DF.PaymentService.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.PaymentService.Infrastructure.Messaging;

public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _exchangeName;
    private readonly string _retryExchangeName;
    private readonly string _deadExchangeName;
    private readonly ushort _prefetchCount;
    private readonly int _maxRetries;

    private readonly int[] _retryDelaysMs = new[] { 2000, 4000, 8000, 16000 };

    public RabbitMQEventBus(
        IConnection connection,
        IServiceScopeFactory scopeFactory, 
        string exchangeName = "df.events",
        ushort prefetchCount = 32,
        int maxRetries = 3)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _exchangeName = exchangeName;
        _retryExchangeName = $"{exchangeName}.retry";
        _deadExchangeName = $"{exchangeName}.dlx";
        _prefetchCount = prefetchCount;
        _maxRetries = maxRetries;

        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(_exchangeName, ExchangeType.Topic, durable: true, autoDelete: false)
            .GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(_retryExchangeName, ExchangeType.Topic, durable: true, autoDelete: false)
            .GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(_deadExchangeName, ExchangeType.Topic, durable: true, autoDelete: false)
            .GetAwaiter().GetResult();

        _channel.BasicQosAsync(0, _prefetchCount, false).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
    {
        var eventName = typeof(TEvent).Name;
        var message = JsonSerializer.Serialize(@event);
        await PublishAsync(eventName, message, cancellationToken);
    }

    public async Task PublishAsync(string eventName, string message, CancellationToken cancellationToken = default)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var msgId = Guid.NewGuid().ToString("N");

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = msgId,
            CorrelationId = msgId,
            Headers = new Dictionary<string, object?>()
            {
                ["x-event-name"] = eventName,
                ["x-retry-count"] = 0
            }
        };

        await _channel.BasicPublishAsync(
            exchange: _exchangeName,
            routingKey: eventName,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: cancellationToken);
    }

    public void Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        _ = SubscribeAsync(handler);
    }

    // Dedicated channels per subscriber so a handler crash that kills the channel
    // can't take publish + every other consumer down with it.
    private readonly List<IChannel> _subscriberChannels = new();

    public async Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, string? queueName = null, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        var eventName = typeof(TEvent).Name;
        queueName ??= $"{eventName}.queue";

        var subscriberChannel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await subscriberChannel.BasicQosAsync(0, _prefetchCount, false, cancellationToken: cancellationToken);
        lock (_subscriberChannels) { _subscriberChannels.Add(subscriberChannel); }

        await subscriberChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await subscriberChannel.QueueBindAsync(queueName, _exchangeName, routingKey: eventName, cancellationToken: cancellationToken);

        var retryQueue = $"{queueName}.retry";
        await subscriberChannel.QueueDeclareAsync(retryQueue, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = _exchangeName,
            ["x-dead-letter-routing-key"] = eventName
        }, cancellationToken: cancellationToken);
        await subscriberChannel.QueueBindAsync(retryQueue, _retryExchangeName, routingKey: eventName, cancellationToken: cancellationToken);

        var dlq = $"{queueName}.dlq";
        await subscriberChannel.QueueDeclareAsync(dlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken);
        await subscriberChannel.QueueBindAsync(dlq, _deadExchangeName, routingKey: eventName, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(subscriberChannel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var messageId = ea.BasicProperties?.MessageId ?? ComputeMessageId(ea.Body.ToArray());
            var headers = ea.BasicProperties?.Headers ?? new Dictionary<string, object?>();
            var retryCount = GetRetryCount(headers);

            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IProcessedMessageStore>();

            try
            {
                if (await store.ExistsAsync(messageId, cancellationToken))
                {
                    await subscriberChannel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var @event = JsonSerializer.Deserialize<TEvent>(json);
                if (@event is null)
                {
                    await PublishToDeadAsync(eventName, json, ea.BasicProperties);
                    await subscriberChannel.BasicAckAsync(ea.DeliveryTag, false);
                    return;
                }

                await handler(@event);

                await store.MarkProcessedAsync(messageId, cancellationToken);
                await subscriberChannel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch
            {
                var nextRetry = retryCount + 1;
                if (nextRetry <= _maxRetries)
                {
                    var delayMs = ComputeDelay(nextRetry);
                    await PublishToRetryAsync(eventName, ea.Body.ToArray(), ea.BasicProperties, nextRetry, delayMs);
                    await subscriberChannel.BasicAckAsync(ea.DeliveryTag, false);
                }
                else
                {
                    await PublishToDeadAsync(eventName, Encoding.UTF8.GetString(ea.Body.ToArray()), ea.BasicProperties);
                    await subscriberChannel.BasicAckAsync(ea.DeliveryTag, false);
                }
            }
        };

        await subscriberChannel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);
    }

    private static string ComputeMessageId(byte[] body)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var hash = sha.ComputeHash(body);
        return Convert.ToHexString(hash);
    }

    private static int GetRetryCount(IDictionary<string, object?> headers)
    {
        if (headers.TryGetValue("x-retry-count", out var value))
        {
            try
            {
                return value switch
                {
                    byte b => b,
                    sbyte sb => sb,
                    short s => s,
                    ushort us => us,
                    int i => i,
                    uint ui => (int)ui,
                    long l => (int)l,
                    ulong ul => (int)ul,
                    byte[] bytes => int.Parse(Encoding.UTF8.GetString(bytes)),
                    _ => 0
                };
            }
            catch { return 0; }
        }
        return 0;
    }

    private int ComputeDelay(int retryAttempt)
    {
        if (retryAttempt - 1 < _retryDelaysMs.Length) return _retryDelaysMs[retryAttempt - 1];
        var last = _retryDelaysMs[^1];
        return last * (int)Math.Pow(2, retryAttempt - _retryDelaysMs.Length);
    }

    private async Task PublishToRetryAsync(string eventName, byte[] body, IReadOnlyBasicProperties? originalProps, int nextRetry, int delayMs)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = originalProps?.ContentType ?? "application/json",
            MessageId = originalProps?.MessageId ?? Guid.NewGuid().ToString("N"),
            CorrelationId = originalProps?.CorrelationId ?? originalProps?.MessageId,
            Headers = new Dictionary<string, object?>()
            {
                ["x-event-name"] = eventName,
                ["x-retry-count"] = nextRetry
            },
            Expiration = delayMs.ToString()
        };

        await _channel.BasicPublishAsync(_retryExchangeName, eventName, false, props, body);
    }

    private async Task PublishToDeadAsync(string eventName, string message, IReadOnlyBasicProperties? originalProps)
    {
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = originalProps?.ContentType ?? "application/json",
            MessageId = originalProps?.MessageId ?? Guid.NewGuid().ToString("N"),
            CorrelationId = originalProps?.CorrelationId ?? originalProps?.MessageId,
            Headers = new Dictionary<string, object?>()
            {
                ["x-event-name"] = eventName,
                ["x-dead-letter"] = true,
                ["x-retry-count"] = GetRetryCount(originalProps?.Headers ?? new Dictionary<string, object?>())
            }
        };

        await _channel.BasicPublishAsync(_deadExchangeName, eventName, false, props, Encoding.UTF8.GetBytes(message));
    }

    public void Dispose()
    {
        try
        {
            lock (_subscriberChannels)
            {
                foreach (var ch in _subscriberChannels)
                {
                    try { ch.CloseAsync().GetAwaiter().GetResult(); } catch { }
                    try { ch.Dispose(); } catch { }
                }
                _subscriberChannels.Clear();
            }

            _channel?.CloseAsync().GetAwaiter().GetResult();
            _channel?.Dispose();
            // IConnection is registered as Singleton and owned by DI — do NOT dispose it here
            // or every other consumer/publisher dies on bus shutdown.
        }
        catch { /* best-effort */ }
    }
}