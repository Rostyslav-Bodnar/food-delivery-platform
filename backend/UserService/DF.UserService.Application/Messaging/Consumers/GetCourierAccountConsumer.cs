using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using DF.UserService.Application.Mappers;
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

        return AccountResponseMapper.ToCourierResponse(account, user?.Email);
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
