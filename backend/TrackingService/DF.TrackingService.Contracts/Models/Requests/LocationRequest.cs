namespace DF.TrackingService.Contracts.Models.Requests;

public record CreateLocationRequest(
    string FullAddress,
    string City,
    string Street,
    string House
    );
    
public record UpdateLocationRequest(
    string FullAddress,
    string City,
    string Street,
    string House
);