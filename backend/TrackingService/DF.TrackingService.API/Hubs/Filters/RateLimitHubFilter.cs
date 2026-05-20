using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace DF.TrackingService.API.Hubs.Filters;

public class RateLimitHubFilter(IMemoryCache cache) : IHubFilter
{
    // ✅ 1 update / 3 seconds
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(3);

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (invocationContext.HubMethodName != "SendLocation")
        {
            return await next(invocationContext);
        }

        var accountId = invocationContext.Context.User?.FindFirst("account_id")?.Value;
        var orderId = invocationContext.Context.User?.FindFirst("order_id")?.Value;

        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(orderId))
        {
            throw new HubException("Unauthorized");
        }

        var key = $"ratelimit:{accountId}:{orderId}";

        if (cache.TryGetValue(key, out _))
        {
            throw new HubException("Rate limit exceeded");
        }

        cache.Set(key, true, Window);

        return await next(invocationContext);
    }
}
