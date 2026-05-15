using System;

namespace DF.Contracts.Gateway.Responses.Order;

public record TrackingAccessTokenResponse
(
    string Token,
    DateTime ExpiresAt
);