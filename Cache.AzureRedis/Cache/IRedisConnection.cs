using StackExchange.Redis;

namespace Cache.AzureRedis.Cache;

public interface IRedisConnection
{
    T WaitAndRetry<T>(Func<IDatabase, T> func);
    Task<T> WaitAndRetryAsync<T>(Func<IDatabase, Task<T>> func, CancellationToken cancellationToken);
}
