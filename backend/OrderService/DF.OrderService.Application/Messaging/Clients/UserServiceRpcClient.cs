using DF.Contracts.RPC.Requests.UserService;
using DF.Contracts.RPC.Responses.UserService;
using RabbitMQ.Client;

namespace DF.OrderService.Application.Messaging.Clients;

public sealed class UserServiceRpcClient : IAsyncDisposable
{
    private readonly RpcChannel _rpc;

    private UserServiceRpcClient(RpcChannel rpc) => _rpc = rpc;

    public static async Task<UserServiceRpcClient> CreateAsync(IConnection connection, CancellationToken ct = default)
        => new(await RpcChannel.CreateAsync(connection, name: "UserService", cancellationToken: ct));

    public Task<GetAccountResponse> GetAccountAsync(GetAccountRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetAccountRequest, GetAccountResponse>("user.getaccount", request, ct);

    public Task<GetBusinessAccountResponse> GetBusinessAccountAsync(GetBusinessAccountRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetBusinessAccountRequest, GetBusinessAccountResponse>("user.getbussinessaccount", request, ct);

    
    public Task<List<GetBusinessAccountResponse>> GetBusinessAccountsBatchAsync(GetBusinessAccountsBatchRequest businessIds, CancellationToken ct = default)
        => _rpc.CallAsync<GetBusinessAccountsBatchRequest, List<GetBusinessAccountResponse>>("user.getbusinessaccountsbatch", businessIds, ct);
    
    public Task<List<GetCourierAccountResponse>> GetCourierAccountsBatchAsync(GetCourierAccountsBatchRequest courierIds, CancellationToken ct = default)
        => _rpc.CallAsync<GetCourierAccountsBatchRequest, List<GetCourierAccountResponse>>("user.getcourieraccountsbatch", courierIds, ct);
    
    public Task<List<GetCustomerAccountResponse>> GetCustomerAccountsBatchAsync(GetCustomerAccountsBatchRequest customerIds, CancellationToken ct = default)
        => _rpc.CallAsync<GetCustomerAccountsBatchRequest, List<GetCustomerAccountResponse>>("user.getcustomeraccountsbatch", customerIds, ct);

    public Task<GetCustomerAccountResponse> GetCustomerAccountAsync(GetCustomerAccountRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetCustomerAccountRequest, GetCustomerAccountResponse>("user.getcustomeraccount", request, ct);

    public Task<GetCourierAccountResponse> GetCourierAccountAsync(GetCourierAccountRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetCourierAccountRequest, GetCourierAccountResponse>("user.getcourieraccount", request, ct);

    public ValueTask DisposeAsync() => _rpc.DisposeAsync();
}
