using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;

namespace Cache.AzureRedis.Cache;

/// <summary>
///     Azure Redis Distributed Cache Facade
/// </summary>
public class DistributedCacheFacade : IDistributedCache
{
    private readonly RedisConnection _redisConnection;

    public DistributedCacheFacade(RedisConnection redisConnection)
    {
        _redisConnection = redisConnection ?? throw new ArgumentNullException(nameof(redisConnection));
    }

    /// <inheritdoc />
    public byte[]? Get(string key)
    {
        var value = _redisConnection.WaitAndRetry(db => db.StringGet(key));

        if (value.IsNull || !value.HasValue)
        {
            return null;
        }
        return (byte[]?)value;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var redisKey = new RedisKey(key);
        var value = await _redisConnection.WaitAndRetryAsync(db => db.StringGetAsync(redisKey), token);

        if (value.IsNull || !value.HasValue)
        {
            return null;
        }
        return (byte[]?)value;

    }

    /// <inheritdoc />
    public void Refresh(string key)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public Task RefreshAsync(string key, CancellationToken token = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _redisConnection.WaitAndRetry(db => db.StringGetDelete(key));
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        var redisKey = new RedisKey(key);
        return _redisConnection.WaitAndRetryAsync(db => db.StringGetDeleteAsync(redisKey), token);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        var redisKey = new RedisKey(key);
        var redisValue = RedisValue.CreateFrom(new MemoryStream(value));

        _redisConnection.WaitAndRetry(db => db.StringSet(redisKey, redisValue, options.SlidingExpiration));
    }

    /// <inheritdoc />
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        var redisKey = new RedisKey(key);
        var redisValue = RedisValue.CreateFrom(new MemoryStream(value));

        return _redisConnection.WaitAndRetryAsync(db => db.StringSetAsync(redisKey, redisValue, options.SlidingExpiration), token);
    }
}
