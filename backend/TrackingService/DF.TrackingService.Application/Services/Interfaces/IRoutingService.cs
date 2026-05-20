using DF.TrackingService.Contracts.Models.Responses;

namespace DF.TrackingService.Application.Services.Interfaces;

public interface IRoutingService
{
    Task<RouteResult> BuildRouteAsync(RoutePoint courier, RoutePoint business, RoutePoint customer);
}
