using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace Cache.AzureRedis.Cache;

public class DistributedCacheAdapter : IDistributedCacheAdapter
{
    private readonly IDistributedCache _distributedCache;
    private readonly DistributedCacheEntryOptions _distributedCacheEntryOptions;

    public DistributedCacheAdapter(IDistributedCache distributedCache, DistributedCacheEntryOptions distributedCacheEntryOptions)
	{
		_distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        _distributedCacheEntryOptions = distributedCacheEntryOptions;
	}

    /// <inheritdoc />
    public T Get<T>(string key) where T : class
    {
        var jsonValue = _distributedCache.Get(key);        
        if(jsonValue == null)
        {
            return null!;
        }

        var result = JsonSerializer.Deserialize<T>(new MemoryStream(jsonValue));

        return result!;
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key, CancellationToken token = default) where T : class
    {
        var jsonValue = await _distributedCache.GetAsync(key, token);
        if(jsonValue == null)
        {
            return null!;
        }

        var result = JsonSerializer.Deserialize<T>(new MemoryStream(jsonValue));

        return result!;
    }    

    /// <inheritdoc />
    public void Remove(string key)
    {
        _distributedCache.Remove(key);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        return _distributedCache.RemoveAsync(key, token);
    }

    /// <inheritdoc />
    public void Set<T>(string key, T value)
    {
        var jsonString = JsonSerializer.Serialize(value, typeof(T));
        var bytesValue = UTF8Encoding.UTF8.GetBytes(jsonString);

        _distributedCache.Set(key, bytesValue, _distributedCacheEntryOptions);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, CancellationToken token = default)
    {
        var jsonString = JsonSerializer.Serialize(value, typeof(T));
        var bytesValue = UTF8Encoding.UTF8.GetBytes(jsonString);

        return _distributedCache.SetAsync(key, bytesValue, _distributedCacheEntryOptions, token);
    }
}
