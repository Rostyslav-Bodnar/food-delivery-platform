using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using RabbitMQ.Client;

namespace DF.OrderService.Application.Messaging.Clients;

public sealed class TrackingServiceRpcClient : IAsyncDisposable
{
    private readonly RpcChannel _rpc;

    private TrackingServiceRpcClient(RpcChannel rpc) => _rpc = rpc;

    public static async Task<TrackingServiceRpcClient> CreateAsync(IConnection connection, CancellationToken ct = default)
        => new(await RpcChannel.CreateAsync(connection, name: "TrackingService", cancellationToken: ct));

    public Task<GetLocationsResponse> GetLocationsAsync(GetLocationRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetLocationRequest, GetLocationsResponse>("tracking.getlocations", request, ct);

    public Task<GetBusinessLocationsResponse> GetBusinessLocationsAsync(GetBusinessLocationsRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetBusinessLocationsRequest, GetBusinessLocationsResponse>("tracking.getbusinesslocations", request, ct);

    public ValueTask DisposeAsync() => _rpc.DisposeAsync();
}
