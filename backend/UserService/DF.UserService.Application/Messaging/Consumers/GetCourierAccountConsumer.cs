using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.UserService.Application.Messaging.Consumers;

public class GetCourierAccountConsumer : IConsumer
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public GetCourierAccountConsumer(IConnection connection, IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "user.getcourieraccount",
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
            var accountRepository = scope.ServiceProvider.GetRequiredService<IAccountRepository>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Десеріалізація запиту
            var request = JsonSerializer.Deserialize<GetCourierAccountRequest>(message);

            if (request.CourierId != null)
            {
                var account = await accountRepository.Get(request.CourierId) as CourierAccount;
                var user = await userRepository.Get(account.UserId);
                
                var response = new GetCourierAccountResponse(
                    account.Id,
                    account.UserId,
                    account.AccountType.ToString(),
                    account.ImageUrl,
                    account.Name,
                    account.Surname,
                    account.PhoneNumber,
                    user.Email,
                    account.Address
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
            queue: "user.getcourieraccount",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();
    }

}
