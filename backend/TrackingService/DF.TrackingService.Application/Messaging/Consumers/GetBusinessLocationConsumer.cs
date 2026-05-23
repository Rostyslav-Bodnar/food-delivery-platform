using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class GetBusinessLocationConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetBusinessLocationConsumer> logger)
    : RpcConsumerBase<GetBusinessLocationsRequest, GetBusinessLocationsResponse>(
        connection, scopeFactory, logger, "tracking.getbusinesslocations")
{
    protected override async Task<GetBusinessLocationsResponse> HandleRequestAsync(
        GetBusinessLocationsRequest request,
        IServiceProvider services)
    {
        var businessLocationRepository =
            services.GetRequiredService<IBusinessLocationRepository>();

        var businessLocations =
            (await businessLocationRepository.GetByBusinessIdAsync(request.BusinessId))
            .ToList();

        if (businessLocations.Count == 0)
        {
            // Default response carries BusinessId so the caller can still
            // correlate, but with an empty list. Treat empty as "no locations".
            return new GetBusinessLocationsResponse(
                request.BusinessId,
                Array.Empty<GetBusinessLocationResponse>());
        }

        return new GetBusinessLocationsResponse(
            request.BusinessId,
            businessLocations.Select(bl => new GetBusinessLocationResponse(
                bl.Id,
                bl.LocationId,
                bl.Location?.FullAddress ?? string.Empty,
                bl.Location?.City ?? string.Empty,
                bl.Location?.Street ?? string.Empty,
                bl.Location?.House ?? string.Empty,
                bl.Location?.GeoPoint?.Y ?? 0,
                bl.Location?.GeoPoint?.X ?? 0)).ToList());
    }

    protected override GetBusinessLocationsResponse CreateDefaultResponse() =>
        new(Guid.Empty, Array.Empty<GetBusinessLocationResponse>());
}
