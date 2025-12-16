using DF.TrackingService.Contracts.Models.Requests;
using DF.TrackingService.Contracts.Models.Responses;

namespace DF.TrackingService.Application.Services.Interfaces;

public interface ILocationService
{
    Task<LocationResponse?> GetLocationAsync(Guid id);
    Task<List<LocationResponse>> GetLocationsAsync();
    Task<LocationResponse> CreateLocation(CreateLocationRequest request);
    Task<LocationResponse> UpdateLocation(UpdateLocationRequest request);
    Task<bool> DeleteLocation(Guid id);
}