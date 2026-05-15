using DF.Contracts.Gateway.Requests.Tracking;
using DF.Contracts.Gateway.Responses.Tracking;

namespace DF.TrackingService.Application.Services.Interfaces;

public interface ILocationService
{
    Task<LocationResponse?> GetLocationAsync(Guid id);
    Task<List<LocationResponse>> GetLocationsAsync();
    Task<LocationResponse> CreateLocation(CreateLocationRequest request);
    Task<LocationResponse> UpdateLocation(UpdateLocationRequest request);
    Task<bool> DeleteLocation(Guid id);
    Task<LocationResponse> AddLocationAsync(AddLocationRequest request);

}