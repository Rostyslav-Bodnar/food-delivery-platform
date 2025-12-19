using System;

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