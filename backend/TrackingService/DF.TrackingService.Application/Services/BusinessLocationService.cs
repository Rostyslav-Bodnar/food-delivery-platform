using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models.Requests;
using DF.TrackingService.Contracts.Models.Responses;
using DF.TrackingService.Domain.Entities;

namespace DF.TrackingService.Application.Services;

public class BusinessLocationService(
    IBusinessLocationRepository businessLocationRepository,
    ILocationRepository locationRepository
) : IBusinessLocationService
{
    public async Task<BusinessLocationResponse?> GetBusinessLocationAsync(Guid id)
    {
        var entity = await businessLocationRepository.Get(id);
        if (entity is null) return null;

        return MapToResponse(entity);
    }

    public async Task<List<BusinessLocationResponse>> GetBusinessLocationsByBusinessIdAsync(Guid businessId)
    {
        var entities = await businessLocationRepository.GetByBusinessIdAsync(businessId);
        return entities
            .Where(e => e != null)
            .Select(e => MapToResponse(e!))
            .ToList();
    }

    public async Task<BusinessLocationResponse> CreateBusinessLocationAsync(CreateBusinessLocationRequest request)
    {
        var location = await locationRepository.Get(request.LocationId);
        if (location is null)
            throw new NullReferenceException("Location not found");

        var entity = new BusinessLocation
        {
            Id = Guid.NewGuid(),
            BusinessId = request.BusinessId,
            LocationId = request.LocationId,
            Location = location
        };

        var created = await businessLocationRepository.Create(entity);
        return MapToResponse(created);
    }

    public async Task<bool> DeleteBusinessLocationAsync(Guid id)
    {
        return await businessLocationRepository.Delete(id);
    }

    private static BusinessLocationResponse MapToResponse(BusinessLocation entity)
    {
        return new BusinessLocationResponse(
            entity.Id,
            entity.LocationId,
            new LocationResponse(
                entity.Location.Id,
                entity.Location.FullAddress,
                entity.Location.City,
                entity.Location.Street,
                entity.Location.House,
                entity.Location.GeoPoint?.Y ?? 0,
                entity.Location.GeoPoint?.X ?? 0
            ),
            entity.BusinessId
        );
    }
}
