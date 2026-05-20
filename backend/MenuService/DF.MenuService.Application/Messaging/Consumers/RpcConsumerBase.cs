using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace DF.MenuService.Application.Messaging.Consumers;

public abstract class RpcConsumerBase : IConsumer
{
    protected readonly IConnection Connection;
    protected readonly IServiceScopeFactory ScopeFactory;
    protected readonly ILogger Logger;

    protected IChannel? Channel;
    private string? _consumerTag;

    protected abstract string QueueName { get; }

    protected RpcConsumerBase(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger logger)
    {
        Connection = connection;
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Channel = await Connection.CreateChannelAsync(
            cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(
            Channel,
            cancellationToken);

        await Channel.QueueDeclareAsync(
            queue: QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(QueueName),
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(Channel);
        consumer.ReceivedAsync += HandleMessageAsync;

        _consumerTag = await Channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task HandleMessageAsync(
        object sender,
        BasicDeliverEventArgs ea)
    {
        if (Channel is null)
            return;

        try
        {
            await ConsumeAsync(ea);

            await Channel.BasicAckAsync(
                ea.DeliveryTag,
                multiple: false);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex,
                "Failed to process message in queue {QueueName}. CorrelationId={CorrelationId}",
                QueueName,
                ea.BasicProperties.CorrelationId);

            try
            {
                await Channel.BasicNackAsync(
                    ea.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
            catch (Exception nackEx)
            {
                Logger.LogWarning(
                    nackEx,
                    "Failed to nack message {DeliveryTag}",
                    ea.DeliveryTag);
            }
        }
    }

    protected abstract Task ConsumeAsync(BasicDeliverEventArgs ea);

    protected async Task PublishReplyAsync<T>(
        string? replyTo,
        string? correlationId,
        T payload)
    {
        if (Channel is null || string.IsNullOrWhiteSpace(replyTo))
            return;

        var props = new BasicProperties
        {
            CorrelationId = correlationId
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(payload);

        await using var publishChannel =
            await Connection.CreateChannelAsync();

        await publishChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: replyTo,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    protected async Task<GetDishResponse> MapDishAsync(
        Domain.Entities.Dish dish,
        IIngredientRepository ingredientRepository)
    {
        var ingredients =
            await ingredientRepository.GetAllIngredientsByDishId(dish.Id);

        return new GetDishResponse(
            DishId: dish.Id,
            Name: dish.Name,
            Description: dish.Description ?? string.Empty,
            Image: dish.Image ?? string.Empty,
            Price: dish.Price,
            CategoryId: (int)dish.Category,
            CategoryName: dish.Category.ToString(),
            CookingTime: dish.CookingTime,
            BusinessId: dish.BusinessId,
            Ingredients: new GetIngredientsResponse(
                ingredients.Select(i => new GetIngredientResponse(
                    IngredientId: i.Id,
                    DishId: i.DishId,
                    Name: i.Name,
                    Weight: i.Weight))
                .ToList()));
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Channel is null)
            return;

        try
        {
            if (!string.IsNullOrWhiteSpace(_consumerTag))
            {
                await Channel.BasicCancelAsync(
                    _consumerTag,
                    noWait: false,
                    cancellationToken: cancellationToken);
            }

            await Channel.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(
                ex,
                "Error while stopping consumer {QueueName}",
                QueueName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Channel is not null)
        {
            await Channel.DisposeAsync();
            Channel = null;
        }
    }
}