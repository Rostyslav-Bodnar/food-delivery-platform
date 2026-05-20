using DF.Contracts.RPC.Requests.MenuService;
using DF.Contracts.RPC.Responses.MenuService;
using RabbitMQ.Client;

namespace DF.OrderService.Application.Messaging.Clients;

public sealed class MenuServiceRpcClient : IAsyncDisposable
{
    private readonly RpcChannel _rpc;

    private MenuServiceRpcClient(RpcChannel rpc) => _rpc = rpc;

    public static async Task<MenuServiceRpcClient> CreateAsync(IConnection connection, CancellationToken ct = default)
        => new(await RpcChannel.CreateAsync(connection, name: "MenuService", cancellationToken: ct));

    public Task<GetDishesResponse> GetDishesAsync(GetDishesRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetDishesRequest, GetDishesResponse>("menu.getdishes", request, ct);

    public Task<GetDishResponse> GetDishAsync(GetDishRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetDishRequest, GetDishResponse>("menu.getdish", request, ct);

    public ValueTask DisposeAsync() => _rpc.DisposeAsync();
}
