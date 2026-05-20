using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetDishConsumer> logger)
    : RpcConsumerBase(connection, scopeFactory, logger)
{
    protected override string QueueName => "menu.getdish";

    protected override async Task ConsumeAsync(BasicDeliverEventArgs ea)
    {
        var request = JsonSerializer.Deserialize<GetDishRequest>(ea.Body.Span)
                      ?? throw new InvalidOperationException(
                          "Empty GetDishRequest payload");

        if (request.DishId == Guid.Empty)
            throw new ArgumentException("DishId must not be empty");

        await using var scope = ScopeFactory.CreateAsyncScope();

        var dishRepository =
            scope.ServiceProvider.GetRequiredService<IDishRepository>();

        var ingredientRepository =
            scope.ServiceProvider.GetRequiredService<IIngredientRepository>();

        var dish = await dishRepository.Get(request.DishId)
                   ?? throw new KeyNotFoundException(
                       $"Dish {request.DishId} not found");

        var response = await MapDishAsync(
            dish,
            ingredientRepository);

        await PublishReplyAsync(
            ea.BasicProperties.ReplyTo,
            ea.BasicProperties.CorrelationId,
            response);
    }
}