namespace DF.TrackingService.Contracts.Models.Requests;

public record AddLocationRequest(
    Guid BusinessId,
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
    );