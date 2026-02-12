using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using DF.MenuService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.MenuService.Application.Messaging.Consumers;

public class GetDishConsumer : IConsumer
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public GetDishConsumer(IConnection connection, IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "menu.getdish",
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
            var request = JsonSerializer.Deserialize<GetDishRequest>(message);

            // Тут твоя бізнес‑логіка: знайти accountId по UserId
            if (request.DishId != null)
            {
                var dish = await dishRepository.Get(request.DishId);
                
                var ingredients = await ingredientRepository.GetAllIngredientsByDishId(dish.Id);

                var response = new GetDishResponse(
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
                            ingredients
                                .Select(i => new GetIngredientResponse(
                                    IngredientId: i.Id,
                                    DishId: i.DishId,
                                    Name: i.Name,
                                    Weight: i.Weight
                                )).ToList()
                        )
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
            queue: "menu.getdish",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();
    }

}