using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public sealed class GetAccountConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetAccountConsumer> logger)
    : RpcConsumerBase<GetAccountRequest, GetAccountResponse>(connection, scopeFactory, logger, "user.getaccount")
{
    protected override async Task<GetAccountResponse> HandleRequestAsync(
        GetAccountRequest request,
        IServiceProvider services)
    {
        if (request.UserId is null)
        {
            return CreateDefaultResponse();
        }

        var accounts = services.GetRequiredService<IAccountRepository>();

        var account = await accounts.GetCurrentAccountByUserId(request.UserId.Value);
        if (account is null)
        {
            return CreateDefaultResponse();
        }

        return new GetAccountResponse(
            account.Id,
            account.UserId,
            account.AccountType.ToString());
    }

    protected override GetAccountResponse CreateDefaultResponse() =>
        new(Guid.Empty, Guid.Empty, string.Empty);
}
