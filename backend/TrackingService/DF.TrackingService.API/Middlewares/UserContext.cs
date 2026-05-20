using DF.TrackingService.Application.Services.Interfaces;

namespace DF.TrackingService.API.Middlewares;

public class UserContext(IHttpContextAccessor accessor) : IUserContext
{
    public Guid UserId =>
        accessor.HttpContext?.Items["UserId"] is Guid id
            ? id
            : Guid.Empty;

    // Prefer the X-Internal-AccountId header (REST path through the Gateway).
    // Fall back to the JWT claim for the SignalR hub, which receives a real
    // JWT minted by OrderService.
    public Guid AccountId
    {
        get
        {
            if (accessor.HttpContext?.Items["AccountId"] is Guid accId)
                return accId;

            return Guid.TryParse(
                accessor.HttpContext?.User?.FindFirst("account_id")?.Value,
                out var jwtAccountId)
                ? jwtAccountId
                : Guid.Empty;
        }
    }

    public string? AccountType =>
        accessor.HttpContext?.Items["AccountType"] as string
        ?? accessor.HttpContext?.User?.FindFirst("account_type")?.Value;

    public bool IsBusiness =>
        string.Equals(AccountType, "Business", StringComparison.OrdinalIgnoreCase);
}
