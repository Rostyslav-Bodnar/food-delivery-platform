using DF.Contracts.Gateway.Requests.Tracking;
using DF.Contracts.Gateway.Responses.Tracking;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Domain.Entities;

namespace DF.TrackingService.Application.Services;

public class BusinessLocationService(
    IBusinessLocationRepository businessLocationRepository,
    ILocationRepository locationRepository,
    IUserContext userContext
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
        EnsureCallerOwnsBusiness(request.BusinessId);

        var location = await locationRepository.Get(request.LocationId);
        if (location is null)
            throw new KeyNotFoundException("Location not found");

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
        // Load first to verify the caller owns the business it belongs to.
        var existing = await businessLocationRepository.Get(id);
        if (existing is null)
        {
            return false;
        }

        EnsureCallerOwnsBusiness(existing.BusinessId);

        return await businessLocationRepository.Delete(id);
    }

    private void EnsureCallerOwnsBusiness(Guid businessId)
    {
        // account_id and account_type come from JWT claims set by UserService
        // TokenService and validated by TrackingService's JwtBearer scheme.
        if (!userContext.IsBusiness || userContext.AccountId != businessId)
        {
            throw new AccessViolationException(
                "Caller is not authorized to modify locations for this business.");
        }
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
