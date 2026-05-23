using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishesConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetDishesConsumer> logger)
    : RpcConsumerBase(connection, scopeFactory, logger)
{
    protected override string QueueName => "menu.getdishes";

    protected override async Task ConsumeAsync(BasicDeliverEventArgs ea)
    {
        var request = JsonSerializer.Deserialize<GetDishesRequest>(ea.Body.Span)
                      ?? throw new InvalidOperationException(
                          "Empty GetDishesRequest payload");

        await using var scope = ScopeFactory.CreateAsyncScope();

        var dishRepository =
            scope.ServiceProvider.GetRequiredService<IDishRepository>();

        var ingredientRepository =
            scope.ServiceProvider.GetRequiredService<IIngredientRepository>();

        var dishes = request.BusinessId == Guid.Empty
            ? []
            : (await dishRepository
                .GetByBusinessIdAsync(request.BusinessId))
            .ToList();

        var responseItems = await Task.WhenAll(
            dishes.Select(d => MapDishAsync(d, ingredientRepository)));

        var response = new GetDishesResponse(
            responseItems.ToList());

        await PublishReplyAsync(
            ea.BasicProperties.ReplyTo,
            ea.BasicProperties.CorrelationId,
            response);
    }
}