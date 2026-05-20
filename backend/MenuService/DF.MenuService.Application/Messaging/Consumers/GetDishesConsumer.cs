using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishesConsumer : IConsumer
{
    private const string QueueName = "menu.getdishes";

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GetDishesConsumer> _logger;
    private IChannel? _channel;
    private string? _consumerTag;

    public GetDishesConsumer(
        IConnection connection,
        IServiceScopeFactory scopeFactory,
        ILogger<GetDishesConsumer> logger)
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
            var request = JsonSerializer.Deserialize<GetDishesRequest>(ea.Body.Span)
                ?? throw new InvalidOperationException("Empty GetDishesRequest payload");

            await using var scope = _scopeFactory.CreateAsyncScope();
            var dishRepository = scope.ServiceProvider.GetRequiredService<IDishRepository>();
            var ingredientRepository = scope.ServiceProvider.GetRequiredService<IIngredientRepository>();

            // Empty BusinessId is treated as a valid "no dishes" reply, not a failure.
            var dishes = request.BusinessId == Guid.Empty
                ? []
                : (await dishRepository.GetByBusinessIdAsync(request.BusinessId)).ToList();

            var ingredientsByDish = new Dictionary<Guid, List<GetIngredientResponse>>();
            foreach (var dish in dishes)
            {
                var rows = await ingredientRepository.GetAllIngredientsByDishId(dish.Id);
                ingredientsByDish[dish.Id] = rows
                    .Select(i => new GetIngredientResponse(
                        IngredientId: i.Id,
                        DishId: i.DishId,
                        Name: i.Name,
                        Weight: i.Weight))
                    .ToList();
            }

            var response = new GetDishesResponse(
                dishes.Select(d => new GetDishResponse(
                    DishId: d.Id,
                    Name: d.Name,
                    Description: d.Description ?? string.Empty,
                    Image: d.Image ?? string.Empty,
                    Price: d.Price,
                    CategoryId: (int)d.Category,
                    CategoryName: d.Category.ToString(),
                    CookingTime: d.CookingTime,
                    BusinessId: d.BusinessId,
                    Ingredients: new GetIngredientsResponse(ingredientsByDish[d.Id])))
                    .ToList());

            await PublishReplyAsync(replyTo, correlationId, response);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to handle GetDishesRequest (CorrelationId={CorrelationId})", correlationId);

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

        await _channel.BasicPublishAsync(
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
            _logger.LogWarning(ex, "Error while stopping GetDishesConsumer");
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
