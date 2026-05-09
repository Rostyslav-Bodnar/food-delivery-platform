using System;

namespace DF.Contracts.RPC.Responses.TrackingService;

public record GetLocationsResponse(
    LocationDTO DeliverTo,
    LocationDTO DeliverFrom
    );

public record LocationDTO(
    Guid LocationId,
    string FullAddress,
    string City,
    string Street,
    string House,
    double Latitude,
    double Longitude
    );
    
    