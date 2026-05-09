namespace DF.OrderService.Contracts.Models.Responses;

public record LocationResponse(
    string FullAddress,
    string City,
    string Street,
    string House,
    string Latitude,
    string Longitude
    );