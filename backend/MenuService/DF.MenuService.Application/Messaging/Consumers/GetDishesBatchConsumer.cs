using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishesBatchConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetDishesBatchConsumer> logger)
    : RpcConsumerBase(connection, scopeFactory, logger)
{
    protected override string QueueName => "menu.getdishesbatch";

    protected override async Task ConsumeAsync(BasicDeliverEventArgs ea)
    {
        var request = JsonSerializer.Deserialize<GetDishesBatchRequest>(
            ea.Body.Span)
            ?? throw new InvalidOperationException(
                "Empty GetDishesBatchRequest payload");

        if (request.DishIds.Count == 0)
        {
            await PublishReplyAsync(
                ea.BasicProperties.ReplyTo,
                ea.BasicProperties.CorrelationId,
                new GetDishesResponse([]));

            return;
        }

        await using var scope = ScopeFactory.CreateAsyncScope();

        var dishRepository =
            scope.ServiceProvider.GetRequiredService<IDishRepository>();

        var ingredientRepository =
            scope.ServiceProvider.GetRequiredService<IIngredientRepository>();

        var uniqueDishIds = request.DishIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        var dishTasks = uniqueDishIds.ToDictionary(
            id => id,
            id => dishRepository.Get(id));

        await Task.WhenAll(dishTasks.Values);

        var dishes = dishTasks.Values
            .Select(x => x.Result)
            .Where(x => x is not null)
            .ToList();

        var responseItems = await Task.WhenAll(
            dishes.Select(d => MapDishAsync(d!, ingredientRepository)));

        var response = new GetDishesResponse(
            responseItems.ToList());

        await PublishReplyAsync(
            ea.BasicProperties.ReplyTo,
            ea.BasicProperties.CorrelationId,
            response);
    }
}