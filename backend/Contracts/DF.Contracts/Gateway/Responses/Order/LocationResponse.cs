namespace DF.Contracts.Gateway.Responses.Order;

public record LocationResponse(
    string FullAddress,
    string City,
    string Street,
    string House,
    string Latitude,
    string Longitude
);