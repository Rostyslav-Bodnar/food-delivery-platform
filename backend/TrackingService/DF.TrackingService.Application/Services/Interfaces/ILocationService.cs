using DF.Contracts.Gateway.Requests.Tracking;
using DF.Contracts.Gateway.Responses.Tracking;

namespace DF.TrackingService.Application.Services.Interfaces;

public interface ILocationService
{
    Task<LocationResponse?> GetLocationAsync(Guid id);
    Task<List<LocationResponse>> GetLocationsAsync(int skip = 0, int take = 100);
    Task<LocationResponse> CreateLocation(CreateLocationRequest request);
    Task<LocationResponse?> UpdateLocation(Guid id, UpdateLocationRequest request);
    Task<bool> DeleteLocation(Guid id);
    Task<LocationResponse> AddLocationAsync(AddLocationRequest request);
}
