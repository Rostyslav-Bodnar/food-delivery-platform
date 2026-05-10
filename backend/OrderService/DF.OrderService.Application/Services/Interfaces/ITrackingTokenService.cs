using DF.Contracts.Gateway.Responses.Order;

namespace DF.OrderService.Application.Services.Interfaces;

public interface ITrackingTokenService
{
    TrackingAccessTokenResponse CreateTrackingToken(
        Guid subjectId,
        string role,
        Guid orderId,
        IReadOnlyCollection<string> scopes,
        TimeSpan lifetime);
}
