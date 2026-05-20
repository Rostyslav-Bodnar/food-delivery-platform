using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class GetLocationsBatchConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetLocationsBatchConsumer> logger)
    : RpcConsumerBase<GetLocationsBatchRequest, List<GetLocationsResponse>>(
        connection, scopeFactory, logger, "tracking.getlocationsbatch")
{
    protected override async Task<List<GetLocationsResponse>> HandleRequestAsync(
        GetLocationsBatchRequest request,
        IServiceProvider services)
    {
        var locationRepository =
            services.GetRequiredService<ILocationRepository>();

        if (request is null ||
            request.Locations is null ||
            request.Locations.Count == 0)
        {
            return CreateDefaultResponse();
        }

        // =========================
        // LOAD ALL LOCATIONS IN BATCH
        // =========================

        var locationIds = request.Locations
            .SelectMany(x => new[] { x.DeliverToId, x.DeliverFromId })
            .Distinct()
            .ToList();

        var locations = new Dictionary<Guid, Domain.Entities.Location>();

        foreach (var id in locationIds)
        {
            var location = await locationRepository.Get(id);

            if (location != null)
            {
                locations[id] = location;
            }
        }

        // =========================
        // BUILD RESPONSE PER REQUEST ITEM
        // =========================

        var result = new List<GetLocationsResponse>();

        foreach (var item in request.Locations)
        {
            locations.TryGetValue(item.DeliverToId, out var deliverTo);
            locations.TryGetValue(item.DeliverFromId, out var deliverFrom);

            result.Add(new GetLocationsResponse(
                DeliverTo: deliverTo is null ? EmptyDto() : ToDto(deliverTo),
                DeliverFrom: deliverFrom is null ? EmptyDto() : ToDto(deliverFrom)
            ));
        }

        return result;
    }

    protected override List<GetLocationsResponse> CreateDefaultResponse() =>
        new()
        {
            new GetLocationsResponse(EmptyDto(), EmptyDto())
        };

    // =========================
    // MAPPERS
    // =========================

    private static LocationDTO ToDto(Domain.Entities.Location l) => new(
        l.Id,
        l.FullAddress,
        l.City,
        l.Street,
        l.House,
        l.GeoPoint?.Y ?? 0,
        l.GeoPoint?.X ?? 0);

    private static LocationDTO EmptyDto() =>
        new(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, 0);
}