using System;

namespace DF.Contracts.Gateway.Responses.Tracking;

public record BusinessLocationResponse(
    Guid Id,
    Guid LocationId,
    LocationResponse Location,
    Guid BusinessId
);