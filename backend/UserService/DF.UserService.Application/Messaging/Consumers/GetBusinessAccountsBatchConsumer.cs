using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public sealed class GetBusinessAccountsBatchConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetBusinessAccountsBatchConsumer> logger)
    : RpcConsumerBase<GetBusinessAccountsBatchRequest, List<GetBusinessAccountResponse>>(
        connection, scopeFactory, logger, "user.getbusinessaccountsbatch")
{
    protected override async Task<List<GetBusinessAccountResponse>> HandleRequestAsync(
        GetBusinessAccountsBatchRequest request,
        IServiceProvider services)
    {
        if (request.BusinessIds is null || request.BusinessIds.Count == 0)
            return [];

        var accounts = services.GetRequiredService<IAccountRepository>();

        var result = await accounts.GetBusinessAccountsByIds(request.BusinessIds);

        return AccountBatchMapper.ToBusinessList(result);
    }

    protected override List<GetBusinessAccountResponse> CreateDefaultResponse()
        => [];
}