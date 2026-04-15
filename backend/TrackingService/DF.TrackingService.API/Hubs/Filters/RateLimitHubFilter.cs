using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;

namespace DF.TrackingService.API.Hubs.Filters;

public class RateLimitHubFilter : IHubFilter
{
    private readonly IMemoryCache _cache;

    // ✅ 1 update / 3 seconds
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(3);

    public RateLimitHubFilter(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (invocationContext.HubMethodName != "SendLocation")
        {
            return await next(invocationContext);
        }

        var userId = invocationContext.Context.User?.FindFirst("sub")?.Value;
        var orderId = invocationContext.Context.User?.FindFirst("order_id")?.Value;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(orderId))
        {
            throw new HubException("Unauthorized");
        }

        var key = $"ratelimit:{userId}:{orderId}";

        if (_cache.TryGetValue(key, out _))
        {
            throw new HubException("Rate limit exceeded");
        }

        _cache.Set(key, true, Window);

        return await next(invocationContext);
    }
}
