using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models.Requests;
using DF.TrackingService.Contracts.Models.Responses;
using NetTopologySuite.Geometries;
using Location = DF.TrackingService.Domain.Entities.Location;

namespace DF.TrackingService.Application.Services;

public class LocationService(
    ILocationRepository locationRepository,
    GeolocationService geolocationService
) : ILocationService
{
    public async Task<LocationResponse?> GetLocationAsync(Guid id)
    {
        var location = await locationRepository.Get(id);
        if (location is null) return null;

        return MapToResponse(location);
    }

    public async Task<List<LocationResponse>> GetLocationsAsync()
    {
        var locations = await locationRepository.GetAll();
        return locations
            .Where(l => l != null)
            .Select(l => MapToResponse(l!))
            .ToList();
    }

    public async Task<LocationResponse> CreateLocation(CreateLocationRequest request)
    {
        var geodata = await geolocationService.GetGeodataAsync(request.FullAddress);

        var location = new Location
        {
            Id = Guid.NewGuid(),
            FullAddress = request.FullAddress,
            City = request.City,
            Street = request.Street,
            House = request.House,
            GeoPoint = geodata != null ? new Point(geodata.Longitude, geodata.Latitude) { SRID = 4326 } : null
        };

        var created = await locationRepository.Create(location);
        return MapToResponse(created);
    }

    public async Task<LocationResponse> UpdateLocation(UpdateLocationRequest request)
    {
        var existing = (await locationRepository.GetAll())
            .FirstOrDefault(l => l?.FullAddress == request.FullAddress);

        if (existing is null) return null;

        existing.City = request.City;
        existing.Street = request.Street;
        existing.House = request.House;

        var geodata = await geolocationService.GetGeodataAsync(request.FullAddress);
        if (geodata != null)
        {
            existing.GeoPoint = new Point(geodata.Longitude, geodata.Latitude) { SRID = 4326 };
        }

        var updated = await locationRepository.Update(existing);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteLocation(Guid id)
    {
        var location = await locationRepository.Get(id);
        if (location is null) return false;

        var success = await locationRepository.Delete(id);
        
        return success;
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
