using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging.Clients;

public sealed class TrackingServiceRpcClient : IAsyncDisposable
{
    private readonly RpcChannel _rpc;

    private TrackingServiceRpcClient(RpcChannel rpc) => _rpc = rpc;

    public static async Task<TrackingServiceRpcClient> CreateAsync(IConnection connection, CancellationToken ct = default)
        => new(await RpcChannel.CreateAsync(connection, name: "TrackingService", cancellationToken: ct));

    public Task<UpdateBusinessLocationResponse> UpdateBusinessLocationAsync(
        UpdateBusinessLocationRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<UpdateBusinessLocationRequest, UpdateBusinessLocationResponse>(
            "tracking.updatebusinesslocation", request, ct);

    public ValueTask DisposeAsync() => _rpc.DisposeAsync();
}
