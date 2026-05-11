using System;

namespace DF.Contracts.Gateway.Requests.Tracking;

public record CreateBusinessLocationRequest(
    Guid BusinessId,
    Guid LocationId
);