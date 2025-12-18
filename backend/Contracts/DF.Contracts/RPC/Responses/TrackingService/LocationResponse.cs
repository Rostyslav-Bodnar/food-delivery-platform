namespace DF.Contracts.RPC.Responses.TrackingService;

public record GetLocationsResponse(
    LocationDTO DeliverTo,
    LocationDTO DeliverFrom
    );

public record LocationDTO(
    Guid LocationId,
    string FullAddress,
    double Latitude,
    double Longitude
    );
    
    