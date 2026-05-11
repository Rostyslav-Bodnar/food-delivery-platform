using System;

namespace DF.Contracts.Gateway.Requests.Tracking;

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
public record AddLocationRequest(
    Guid BusinessId,
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
);