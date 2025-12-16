namespace DF.Contracts.RPC.Requests.TrackingService;

public record UpdateBusinessLocationRequest(
    Guid  BusinessId,
    Guid BusinessLocationId,
    string FullAddress,
    string City,
    string Street,
    string House
);

public record CreateBusinessLocationRequest(
    Guid  BusinessId,
    string FullAddress,
    string City,
    string Street,
    string House
);
