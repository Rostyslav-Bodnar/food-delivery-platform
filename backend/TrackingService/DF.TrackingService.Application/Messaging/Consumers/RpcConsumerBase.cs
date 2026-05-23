using System.Text;
using System.Text.Json;
using DF.TrackingService.Application.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.TrackingService.Application.Messaging.Consumers;

/// <summary>
/// Base for RabbitMQ RPC consumers. Centralizes channel setup, manual ack,
/// uniform exception handling, and the always-reply contract so an RPC caller's
/// awaited TaskCompletionSource never hangs on a server-side error.
///
/// Mirrors the UserService.RpcConsumerBase to keep failure semantics
/// consistent across services.
/// </summary>
public abstract class RpcConsumerBase<TRequest, TResponse>(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string queueName) : IConsumer
    where TRequest : class
    where TResponse : class
{
    private IChannel? _channel;
    private string? _consumerTag;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(_channel, cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(queueName),
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("RPC consumer started for queue {Queue}", queueName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            await _channel.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while stopping RPC consumer for {Queue}", queueName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        TResponse response;

        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var request = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<TRequest>(json);

            if (request is null)
            {
                logger.LogWarning(
                    "Null or unparseable RPC request on {Queue}; replying with default",
                    queueName);
                response = CreateDefaultResponse();
            }
            else
            {
                using var scope = scopeFactory.CreateScope();
                response = await HandleRequestAsync(request, scope.ServiceProvider);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "RPC handler failed on {Queue}; replying with default to release caller",
                queueName);
            response = CreateDefaultResponse();
        }

        try
        {
            await ReplyAsync(ea, response);
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to publish reply on {Queue}; nacking (requeue={Requeue})",
                queueName, !ea.Redelivered);

            try
            {
                await _channel!.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: !ea.Redelivered);
            }
            catch (Exception nackEx)
            {
                logger.LogError(nackEx, "Failed to nack on {Queue}", queueName);
            }
        }
    }

    private async Task ReplyAsync(BasicDeliverEventArgs ea, TResponse response)
    {
        var replyTo = ea.BasicProperties.ReplyTo;
        if (string.IsNullOrWhiteSpace(replyTo))
        {
            return;
        }

        var props = new BasicProperties
        {
            CorrelationId = ea.BasicProperties.CorrelationId
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

        await _channel!.BasicPublishAsync(
            exchange: "",
            routingKey: replyTo,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    /// <summary>Business logic. Should not throw for "not found" — return <see cref="CreateDefaultResponse"/> instead.</summary>
    protected abstract Task<TResponse> HandleRequestAsync(TRequest request, IServiceProvider services);

    /// <summary>Sentinel response sent to the caller when no real one can be produced. Key ids should be Guid.Empty.</summary>
    protected abstract TResponse CreateDefaultResponse();
}
