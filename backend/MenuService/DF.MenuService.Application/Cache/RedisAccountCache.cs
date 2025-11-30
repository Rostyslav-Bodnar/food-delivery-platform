using StackExchange.Redis;

namespace DF.MenuService.Application.Cache;

public class RedisAccountCache(IConnectionMultiplexer redis) : IAccountCache
{
    private readonly IDatabase redisDatabase = redis.GetDatabase();
    private const string Prefix = "business-account:";

    public async Task<Guid?> GetBusinessIdAsync(Guid userId)
    {
        var value = await redisDatabase.StringGetAsync(Prefix + userId);
        
        if(!value.HasValue)
            return null;
        
        return Guid.Parse(value);
    }

    public async Task SetBusinessIdAsync(Guid userId, Guid businessId)
    {
        var key = Prefix + userId;
        var value = businessId.ToString();
        await redisDatabase.StringSetAsync(key, value, TimeSpan.FromHours(12));
    }
}