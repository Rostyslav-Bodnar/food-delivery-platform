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

    public Task<GetLocationsResponse> GetLocationAsync(GetLocationRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetLocationRequest, GetLocationsResponse>("tracking.getlocations", request, ct);

    public Task<List<GetLocationsResponse>> GetLocationsBatchAsync(GetLocationsBatchRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetLocationsBatchRequest, List<GetLocationsResponse>>("tracking.getlocationsbatch", request, ct);
    
    public Task<GetBusinessLocationsResponse> GetBusinessLocationsAsync(GetBusinessLocationsRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetBusinessLocationsRequest, GetBusinessLocationsResponse>("tracking.getbusinesslocations", request, ct);

    public Task<List<GetBusinessLocationsResponse>> GetBusinessLocationsBatchAsync(GetBusinessLocationsBatchRequest request, CancellationToken ct = default)
        => _rpc.CallAsync<GetBusinessLocationsBatchRequest, List<GetBusinessLocationsResponse>>("tracking.getbusinesslocationsbatch", request, ct);
    
    public ValueTask DisposeAsync() => _rpc.DisposeAsync();
}
