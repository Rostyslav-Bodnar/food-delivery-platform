namespace DF.OrderService.Contracts.Models.Responses;

public record TrackingAccessTokenResponse
(
    string Token,
    DateTime ExpiresAt
    );