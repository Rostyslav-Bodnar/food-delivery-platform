namespace DF.TrackingService.Contracts.Models.Responses;

public record LocationResponse(
    Guid Id,
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
    
    );