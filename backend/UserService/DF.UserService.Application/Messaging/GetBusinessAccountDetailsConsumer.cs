using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using DF.Contracts.RPC.Requests;
using DF.Contracts.RPC.Responses;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace DF.UserService.Application.Messaging;

public class GetBusinessAccountDetailsConsumer : IConsumer
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public GetBusinessAccountDetailsConsumer(IConnection connection, IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "user.getbussinessaccount",
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
            
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Десеріалізація запиту
            var request = JsonSerializer.Deserialize<GetBusinessAccountDetailsRequest>(message);

            // Тут твоя бізнес‑логіка: знайти accountId по UserId
            if (request.BusinessAccountId != null)
            {
                var account = await accountRepository.Get(request.BusinessAccountId) as BusinessAccount;

                var response = new GetBusinessAccountDetailsResponse(
                    account.Id,
                    account.UserId,
                    account.Name,
                    account.Description
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
            queue: "user.getbussinessaccount",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();
    }

}
