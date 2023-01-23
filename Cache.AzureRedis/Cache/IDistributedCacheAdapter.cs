namespace Cache.AzureRedis.Cache;

public interface IDistributedCacheAdapter
{
    T Get<T>(string key) where T : class;
    Task<T> GetAsync<T>(string key, CancellationToken token = default) where T : class;
    void Remove(string key);
    Task RemoveAsync(string key, CancellationToken token = default);
    void Set<T>(string key, T value);
    Task SetAsync<T>(string key, T value, CancellationToken token = default);
}
