using NetTopologySuite.Geometries;

namespace DF.TrackingService.Domain.Entities;

public class Location
{
    public Guid Id  {get; set;}
    public string FullAddress { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
    public string House { get; set; } = string.Empty;

    public Point? GeoPoint { get; set; } = null;

    // Populated by OrderCreatedConsumer when the location is created for a
    // specific order's DeliverTo address. Used for at-least-once-delivery
    // idempotency (skip if a Location for this OrderId already exists).
    public Guid? OrderId { get; set; }
}