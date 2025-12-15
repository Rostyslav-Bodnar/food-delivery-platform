using DF.OrderService.Contracts.Models.Requests;
using NetTopologySuite.Geometries;

namespace DF.OrderService.Application.Mappers;

public class LocationMapper
{
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    // EPSG:4326 — стандарт Google Maps / GPS

    public Domain.Entities.Location Map(CreateLocationRequest request)
    {
        return new Domain.Entities.Location
        {
            FullAddress = request.FullAddress,
            City = request.City,
            Street = request.Street,
            House = request.House,
            GeoPoint = new Point(request.Longitude, request.Latitude) 
            {
                SRID = 4326
            }
        };
    }
}