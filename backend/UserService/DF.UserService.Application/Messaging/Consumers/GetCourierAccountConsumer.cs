using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Repositories.Interfaces;
using DF.UserService.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Consumers;

public sealed class GetCourierAccountConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetCourierAccountConsumer> logger)
    : RpcConsumerBase<GetCourierAccountRequest, GetCourierAccountResponse>(connection, scopeFactory, logger, "user.getcourieraccount")
{
    protected override async Task<GetCourierAccountResponse> HandleRequestAsync(
        GetCourierAccountRequest request,
        IServiceProvider services)
    {
        var accounts = services.GetRequiredService<IAccountRepository>();
        var users = services.GetRequiredService<IUserRepository>();

        if (await accounts.Get(request.CourierId) is not CourierAccount account)
        {
            return CreateDefaultResponse();
        }

        var user = await users.Get(account.UserId);

        return new GetCourierAccountResponse(
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

    protected override GetCourierAccountResponse CreateDefaultResponse() =>
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
