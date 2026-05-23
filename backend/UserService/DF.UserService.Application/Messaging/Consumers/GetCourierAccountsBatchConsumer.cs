using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public class GetCourierAccountsBatchConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetCourierAccountsBatchConsumer> logger)
    : RpcConsumerBase<GetCourierAccountsBatchRequest, List<GetCourierAccountResponse>>(
        connection, scopeFactory, logger, "user.getcourieraccountsbatch")
{
    protected override async Task<List<GetCourierAccountResponse>> HandleRequestAsync(
        GetCourierAccountsBatchRequest request,
        IServiceProvider services)
    {
        if (request.CourierIds is null || request.CourierIds.Count == 0)
            return [];

        var accountsRepo = services.GetRequiredService<IAccountRepository>();
        var usersRepo = services.GetRequiredService<IUserRepository>();

        var accounts = await accountsRepo.GetCourierAccountsByIds(request.CourierIds);

        var userIds = accounts.Select(x => x.UserId).Distinct().ToList();
        var users = await usersRepo.GetByIds(userIds);

        var emailMap = users.ToDictionary(x => x.Id, x => x.Email);

        return AccountBatchMapper.ToCourierList(accounts, emailMap);
    }

    protected override List<GetCourierAccountResponse> CreateDefaultResponse()
        => [];
}