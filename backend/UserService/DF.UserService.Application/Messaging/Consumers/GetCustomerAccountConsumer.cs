using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public sealed class GetCustomerAccountConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetCustomerAccountConsumer> logger)
    : RpcConsumerBase<GetCustomerAccountRequest, GetCustomerAccountResponse>(connection, scopeFactory, logger, "user.getcustomeraccount")
{
    protected override async Task<GetCustomerAccountResponse> HandleRequestAsync(
        GetCustomerAccountRequest request,
        IServiceProvider services)
    {
        var accounts = services.GetRequiredService<IAccountRepository>();
        var users = services.GetRequiredService<IUserRepository>();

        if (await accounts.Get(request.CustomerId) is not CustomerAccount account)
        {
            return CreateDefaultResponse();
        }

        var user = await users.Get(account.UserId);

        return new GetCustomerAccountResponse(
            account.Id,
            account.UserId,
            account.AccountType.ToString(),
            account.ImageUrl ?? string.Empty,
            account.Name,
            account.Surname,
            account.PhoneNumber ?? string.Empty,
            user?.Email ?? string.Empty,
            account.Address ?? string.Empty);
    }

    protected override GetCustomerAccountResponse CreateDefaultResponse() =>
        new(
            Guid.Empty,
            Guid.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
}
