using StackExchange.Redis;
using Ticketing.Core.Interfaces;

namespace Ticketing.Infrastructure.Services;

public class RedisLockService : ILockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> AcquireSeatLockAsync(Guid seatId, string userId, TimeSpan lockDuration)
    {
        var db = _redis.GetDatabase();
        string lockKey = $"lock:seat:{seatId}";

        return await db.StringSetAsync(lockKey, userId, lockDuration, When.NotExists);
    }

    public async Task<bool> ReleaseSeatLockAsync(Guid seatId, string userId)
    {
        var db = _redis.GetDatabase();
        string lockKey = $"lock:seat:{seatId}";

        string luaScript = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        var result = await db.ScriptEvaluateAsync(luaScript, new RedisKey[] { lockKey }, new RedisValue[] { userId });
        return (int)result == 1;
    }
}