using System;
using System.Collections.Generic;

namespace DF.Contracts.RPC.Responses.TrackingService;

public record UpdateBusinessLocationResponse(
    Guid  BusinessId,
    Guid BusinessLocationId,
    string FullAddress,
    string City,
    string Street,
    string House
    );
    
public record CreateBusinessLocationResponse(
    Guid  BusinessId,
    Guid BusinessLocationId,
    string FullAddress,
    string City,
    string Street,
    string House
);

public record GetBusinessLocationsResponse (
    IEnumerable<GetBusinessLocationResponse> BusinessLocations
        );

public record GetBusinessLocationResponse(
    Guid BusinessId,
    Guid BusinessLocationId,
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
    
);