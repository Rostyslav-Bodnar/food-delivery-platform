using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public sealed class GetBusinessAccountConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetBusinessAccountConsumer> logger)
    : RpcConsumerBase<GetBusinessAccountRequest, GetBusinessAccountResponse>(connection, scopeFactory, logger, "user.getbussinessaccount")
{
    protected override async Task<GetBusinessAccountResponse> HandleRequestAsync(
        GetBusinessAccountRequest request,
        IServiceProvider services)
    {
        var accounts = services.GetRequiredService<IAccountRepository>();

        if (await accounts.Get(request.BusinessAccountId) is not BusinessAccount account)
        {
            return CreateDefaultResponse();
        }

        return AccountResponseMapper.ToBusinessResponse(account);
    }

    protected override GetBusinessAccountResponse CreateDefaultResponse() =>
        new(
            Guid.Empty,
            Guid.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            new List<string>(),
            false,
            false,
            string.Empty,
            string.Empty);
}
