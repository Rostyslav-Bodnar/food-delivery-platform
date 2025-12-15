using NetTopologySuite.Geometries;

namespace DF.OrderService.Domain.Entities;

public class Location
{
    public Guid Id  {get; set;}
    public string FullAddress { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string House { get; set; } = string.Empty;

    public Point? GeoPoint { get; set; } = null;
}