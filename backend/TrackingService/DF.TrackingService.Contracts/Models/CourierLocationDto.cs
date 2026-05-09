namespace DF.TrackingService.Contracts.Models;

public record CourierLocationDto(
    Guid OrderId,
    Guid CourierId,
    double Latitude,
    double Longitude,
    double? Speed,
    double? Heading,
    DateTime TimestampUtc
);
