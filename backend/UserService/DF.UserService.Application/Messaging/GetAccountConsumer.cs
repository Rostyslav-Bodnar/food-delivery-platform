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

public class GetAccountConsumer
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public GetAccountConsumer(IConnection connection, IServiceScopeFactory scopeFactory)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "user.getaccount",
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
            var request = JsonSerializer.Deserialize<GetAccountRequest>(message);

            // Тут твоя бізнес‑логіка: знайти accountId по UserId
            if (request.UserId != null)
            {
                var account = await FindAccountByUserIdAsync(request.UserId.Value, accountRepository);

                var response = new GetAccountResponse(
                    account.AccountId,
                    account.UserId,
                    account.AccountType
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
            queue: "user.getaccount",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();
    }

    private async Task<GetAccountResponse> FindAccountByUserIdAsync(Guid userId, IAccountRepository accountRepository)
    {
        var account = await accountRepository.GetCurrentAccountByUserId(userId);

        if (account == null)
        {
            throw new InvalidOperationException($"Account for user {userId} not found.");
        }

        if (account.AccountType != AccountType.Business)
        {
            throw new InvalidOperationException($"Account {account.Id} is not of type Business.");
        }

        return new GetAccountResponse(
            account.Id,
            userId,
            "Business"
        );
    }

}
