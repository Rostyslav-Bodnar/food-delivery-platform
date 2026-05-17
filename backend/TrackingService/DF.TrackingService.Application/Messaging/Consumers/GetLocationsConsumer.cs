using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class GetLocationsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetLocationsConsumer> logger)
    : RpcConsumerBase<GetLocationRequest, GetLocationsResponse>(
        connection, scopeFactory, logger, "tracking.getlocations")
{
    protected override async Task<GetLocationsResponse> HandleRequestAsync(
        GetLocationRequest request,
        IServiceProvider services)
    {
        var locationRepository = services.GetRequiredService<ILocationRepository>();

        var deliverTo = await locationRepository.Get(request.DeliverToId);
        var deliverFrom = await locationRepository.Get(request.DeliverFromId);

        if (deliverTo is null || deliverFrom is null)
        {
            return CreateDefaultResponse();
        }

        return new GetLocationsResponse(
            DeliverTo: ToDto(deliverTo),
            DeliverFrom: ToDto(deliverFrom));
    }

    protected override GetLocationsResponse CreateDefaultResponse() =>
        new(EmptyDto(), EmptyDto());

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
