using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using DF.MenuService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishesConsumer : IConsumer
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public GetDishesConsumer(IConnection connection, IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "menu.getdishes",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();
    }

    public void Start()
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var dishRepository = scope.ServiceProvider.GetRequiredService<IDishRepository>();
            var ingredientRepository = scope.ServiceProvider.GetRequiredService<IIngredientRepository>();
            
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Десеріалізація запиту
            var request = JsonSerializer.Deserialize<GetDishesRequest>(message);

            // Тут твоя бізнес‑логіка: знайти accountId по UserId
            if (request.BusinessId != null)
            {
                var dishes = await dishRepository.GetByBusinessIdAsync(request.BusinessId);

                // Отримуємо інгредієнти для кожної страви
                var ingredientsByDish = new Dictionary<Guid, List<Ingredient>>();

                foreach (var dish in dishes)
                {
                    var ing = await ingredientRepository.GetAllIngredientsByDishId(dish.Id);
                    ingredientsByDish[dish.Id] = ing.ToList();
                }

                var response = new GetDishesResponse(
                    dishes.Select(dish =>
                        new GetDishResponse(
                            DishId: dish.Id,
                            Name: dish.Name,
                            Description: dish.Description,
                            Image: dish.Image,
                            Price: dish.Price,
                            CategoryId: (int)dish.Category,
                            CategoryName: dish.Category.ToString(),
                            CookingTime: dish.CookingTime,
                            BusinessId: dish.BusinessId,
                            Ingredients: new GetIngredientsResponse(
                                ingredientsByDish[dish.Id]
                                    .Select(i => new GetIngredientResponse(
                                        IngredientId: i.Id,
                                        DishId: i.DishId,
                                        Name: i.Name,
                                        Weight: i.Weight
                                    )).ToList()
                            )
                        )
                    ).ToList()
                );

                var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response));

                var props = new BasicProperties
                {
                    CorrelationId = ea.BasicProperties.CorrelationId
                };

                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: ea.BasicProperties.ReplyTo,
                    mandatory: false,
                    basicProperties: props,
                    body: responseBytes
                );
            }

        };

        _channel.BasicConsumeAsync(
            queue: "menu.getdishes",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();
    }

}