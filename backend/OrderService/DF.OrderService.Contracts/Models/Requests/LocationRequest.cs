namespace DF.OrderService.Contracts.Models.Requests;

public record CreateLocationRequest(
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
);