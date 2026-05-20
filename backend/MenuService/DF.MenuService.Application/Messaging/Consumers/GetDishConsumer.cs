using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishConsumer : IConsumer
{
    private const string QueueName = "menu.getdish";

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GetDishConsumer> _logger;
    private IChannel? _channel;
    private string? _consumerTag;

    public GetDishConsumer(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<GetDishConsumer> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(_channel, cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(QueueName),
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        var replyTo = ea.BasicProperties.ReplyTo;
        var correlationId = ea.BasicProperties.CorrelationId;

        try
        {
            var request = JsonSerializer.Deserialize<GetDishRequest>(ea.Body.Span)
                ?? throw new InvalidOperationException("Empty GetDishRequest payload");

            if (request.DishId == Guid.Empty)
                throw new ArgumentException("DishId must not be empty");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dishRepository = scope.ServiceProvider.GetRequiredService<IDishRepository>();
            var ingredientRepository = scope.ServiceProvider.GetRequiredService<IIngredientRepository>();

            var dish = await dishRepository.Get(request.DishId)
                       ?? throw new KeyNotFoundException($"Dish {request.DishId} not found");

            var ingredients = await ingredientRepository.GetAllIngredientsByDishId(dish.Id);

            var response = new GetDishResponse(
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
                    ingredients
                        .Select(i => new GetIngredientResponse(
                            IngredientId: i.Id,
                            DishId: i.DishId,
                            Name: i.Name,
                            Weight: i.Weight))
                        .ToList()));

            await PublishReplyAsync(replyTo, correlationId, response);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to handle GetDishRequest (CorrelationId={CorrelationId})", correlationId);

            // Poison message: don't requeue. The caller's RPC timeout will release it.
            try
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
            catch (Exception nackEx)
            {
                _logger.LogWarning(nackEx, "Failed to nack message {DeliveryTag}", ea.DeliveryTag);
            }
        }
    }

    private async Task PublishReplyAsync<T>(string? replyTo, string? correlationId, T payload)
    {
        if (_channel is null || string.IsNullOrEmpty(replyTo)) return;

        var props = new BasicProperties { CorrelationId = correlationId };
        var body = JsonSerializer.SerializeToUtf8Bytes(payload);

        await using var publishChannel =
            await _connection.CreateChannelAsync();
        
        await publishChannel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: replyTo,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
            {
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            }
            await _channel.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping GetDishConsumer");
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
}
