using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Mappers;
using DF.UserService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public class GetCustomerAccountsBatchConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetCourierAccountsBatchConsumer> logger)
    : RpcConsumerBase<GetCustomerAccountsBatchRequest, List<GetCustomerAccountResponse>>(
        connection, scopeFactory, logger, "user.getcustomeraccountsbatch")
{
    protected override async Task<List<GetCustomerAccountResponse>> HandleRequestAsync(
        GetCustomerAccountsBatchRequest request,
        IServiceProvider services)
    {
        if (request.CustomerIds is null || request.CustomerIds.Count == 0)
            return [];

        var accountsRepo = services.GetRequiredService<IAccountRepository>();
        var usersRepo = services.GetRequiredService<IUserRepository>();

        var accounts = await accountsRepo.GetCustomerAccountsByIds(request.CustomerIds);

        var userIds = accounts.Select(x => x.UserId).Distinct().ToList();
        var users = await usersRepo.GetByIds(userIds);

        var emailMap = users.ToDictionary(x => x.Id, x => x.Email);

        return AccountBatchMapper.ToCustomerList(accounts, emailMap);
    }

    protected override List<GetCustomerAccountResponse> CreateDefaultResponse()
        => [];
}