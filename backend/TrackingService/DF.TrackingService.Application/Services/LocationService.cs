using DF.Contracts.Gateway.Requests.Tracking;
using DF.Contracts.Gateway.Responses.Tracking;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Domain.Entities;
using DF.TrackingService.Infrastructure.Data;
using NetTopologySuite.Geometries;
using Location = DF.TrackingService.Domain.Entities.Location;

namespace DF.TrackingService.Application.Services;

public class LocationService(
    SqlDbContext dbContext,
    ILocationRepository locationRepository,
    IBusinessLocationRepository businessLocationRepository,
    GeolocationService geolocationService,
    IUserContext userContext
) : ILocationService
{
    public async Task<LocationResponse?> GetLocationAsync(Guid id)
    {
        var location = await locationRepository.Get(id);
        if (location is null) return null;

        return MapToResponse(location);
    }

    public async Task<List<LocationResponse>> GetLocationsAsync(int skip = 0, int take = 100)
    {
        var locations = await locationRepository.ListAsync(skip, take);
        return locations.Select(MapToResponse).ToList();
    }

    public async Task<LocationResponse> CreateLocation(CreateLocationRequest request)
    {
        var geodata = await geolocationService.GetGeodataAsync(request.FullAddress);
        if (geodata is null)
        {
            // Refuse to persist a Location with no coordinates — downstream
            // code (consumers, response mappers) dereferences GeoPoint and
            // would NRE. ArgumentException → 400 via ExceptionMiddleware.
            throw new ArgumentException(
                $"Could not geocode address: {request.FullAddress}");
        }

        var location = new Location
        {
            Id = Guid.NewGuid(),
            FullAddress = request.FullAddress,
            City = request.City,
            Street = request.Street,
            House = request.House,
            GeoPoint = new Point(geodata.Longitude, geodata.Latitude) { SRID = 4326 }
        };

        var created = await locationRepository.Create(location);
        return MapToResponse(created);
    }

    public async Task<LocationResponse?> UpdateLocation(Guid id, UpdateLocationRequest request)
    {
        var existing = await locationRepository.Get(id);
        if (existing is null) return null;

        var geodata = await geolocationService.GetGeodataAsync(request.FullAddress);
        if (geodata is null)
        {
            throw new ArgumentException(
                $"Could not geocode address: {request.FullAddress}");
        }

        existing.FullAddress = request.FullAddress;
        existing.City = request.City;
        existing.Street = request.Street;
        existing.House = request.House;
        existing.GeoPoint = new Point(geodata.Longitude, geodata.Latitude) { SRID = 4326 };

        var updated = await locationRepository.Update(existing);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteLocation(Guid id)
    {
        var location = await locationRepository.Get(id);
        if (location is null) return false;

        return await locationRepository.Delete(id);
    }

    public async Task<LocationResponse> AddLocationAsync(AddLocationRequest request)
    {
        EnsureCallerOwnsBusiness(request.BusinessId);

        // Wrap Location + BusinessLocation creation in a single transaction so
        // a failure between the two writes can't leave an orphan Location.
        await using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var location = new Location
            {
                FullAddress = request.FullAddress,
                City = request.City,
                Street = request.Street,
                House = request.House,
                GeoPoint = new Point(request.Longitude, request.Latitude) { SRID = 4326 }
            };
            location = await locationRepository.Create(location);

            var businessLocation = new BusinessLocation
            {
                BusinessId = request.BusinessId,
                Location = location,
                LocationId = location.Id
            };
            await businessLocationRepository.Create(businessLocation);

            await transaction.CommitAsync();

            return MapToResponse(location);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private void EnsureCallerOwnsBusiness(Guid businessId)
    {
        // The caller's active account must BE the business they're modifying.
        // account_id and account_type come from JWT claims set by UserService
        // TokenService and validated by TrackingService's JwtBearer scheme.
        if (!userContext.IsBusiness || userContext.AccountId != businessId)
        {
            throw new AccessViolationException(
                "Caller is not authorized to modify locations for this business.");
        }
    }

    private static LocationResponse MapToResponse(Location location)
    {
        return new LocationResponse(
            location.Id,
            location.FullAddress,
            location.City,
            location.Street,
            location.House,
            location.GeoPoint?.Y ?? 0, // Latitude
            location.GeoPoint?.X ?? 0  // Longitude
        );
    }
}
