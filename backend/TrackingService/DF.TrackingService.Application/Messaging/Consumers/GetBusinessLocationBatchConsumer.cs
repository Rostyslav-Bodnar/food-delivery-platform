using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class GetBusinessLocationBatchConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<GetBusinessLocationBatchConsumer> logger)
    : RpcConsumerBase<
        GetBusinessLocationsBatchRequest,
        List<GetBusinessLocationsResponse>>(
        connection,
        scopeFactory,
        logger,
        "tracking.getbusinesslocationsbatch")
{
    protected override async Task<List<GetBusinessLocationsResponse>> HandleRequestAsync(
        GetBusinessLocationsBatchRequest request,
        IServiceProvider services)
    {
        if (request.BusinessIds is null || request.BusinessIds.Count == 0)
        {
            return [];
        }

        var businessLocationRepository =
            services.GetRequiredService<IBusinessLocationRepository>();

        var uniqueBusinessIds = request.BusinessIds
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToList();

        if (uniqueBusinessIds.Count == 0)
        {
            return [];
        }

        var tasks = uniqueBusinessIds.ToDictionary(
            businessId => businessId,
            businessId => businessLocationRepository.GetByBusinessIdAsync(businessId));

        await Task.WhenAll(tasks.Values);

        var responses = new List<GetBusinessLocationsResponse>();

        foreach (var (businessId, task) in tasks)
        {
            var businessLocations = (await task).ToList();

            responses.Add(new GetBusinessLocationsResponse(
                BusinessId: businessId,
                BusinessLocations: businessLocations.Select(bl =>
                    new GetBusinessLocationResponse(
                        BusinessLocationId: bl.Id,
                        LocationId: bl.LocationId,
                        FullAddress: bl.Location?.FullAddress ?? string.Empty,
                        City: bl.Location?.City ?? string.Empty,
                        Street: bl.Location?.Street ?? string.Empty,
                        House: bl.Location?.House ?? string.Empty,
                        Latitude: bl.Location?.GeoPoint?.Y ?? 0,
                        Longitude: bl.Location?.GeoPoint?.X ?? 0))
                    .ToList()));
        }

        return responses;
    }

    protected override List<GetBusinessLocationsResponse> CreateDefaultResponse()
    {
        return [];
    }
}