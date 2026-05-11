using System;

namespace DF.Contracts.Gateway.Responses.Tracking;

public record LocationResponse(
    Guid Id,
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
);