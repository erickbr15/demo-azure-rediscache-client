using Polly;
using Polly.Retry;
using StackExchange.Redis;
using System.Net.Sockets;

namespace Cache.AzureRedis.Cache;

/// <summary>
///     Redis connection
/// </summary>
/// <remarks>
///     Implementation taken from 
///     https://github.com/Azure-Samples/azure-cache-redis-samples/tree/main/quickstart/aspnet-core
/// </remarks>
public class RedisConnection : IRedisConnection, IDisposable
{
    private const int RetryMaxAttempts = 2;

    // StackExchange.Redis will also be trying to reconnect internally,
    // so limit how often we recreate the ConnectionMultiplexer instance
    // in an attempt to reconnect
    private readonly TimeSpan ReconnectMinInterval = TimeSpan.FromSeconds(60);

    // If errors occur for longer than this threshold, StackExchange.Redis
    // may be failing to reconnect internally, so we'll recreate the
    // ConnectionMultiplexer instance
    private readonly TimeSpan ReconnectErrorThreshold = TimeSpan.FromSeconds(30);
    private readonly TimeSpan RestartConnectionTimeout = TimeSpan.FromSeconds(15);

    private readonly string _connectionString;

    private ConnectionMultiplexer _connection;
    private IDatabase _database;
    private long _lastReconnectTicks;
    private DateTimeOffset _firstErrorTime;
    private DateTimeOffset _previousErrorTime;
    private SemaphoreSlim _reconnectSemaphore;

    private RedisConnection(string connectionString)
    {
        _lastReconnectTicks = DateTimeOffset.MinValue.UtcTicks;
        _firstErrorTime = DateTimeOffset.MinValue;
        _previousErrorTime = DateTimeOffset.MinValue;
        _reconnectSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        _connection = default!;
        _database = default!;

        _connectionString = connectionString;
    }

    public static async Task<RedisConnection> InitializeAsync(string connectionString)
    {
        var redisConnection = new RedisConnection(connectionString);
        await redisConnection.ForceReconnectAsync(initializing: true);

        return redisConnection;
    }

    /// <summary>
    ///     Wait and retry
    /// </summary>
    public T WaitAndRetry<T>(Func<IDatabase, T> func)
    {
        RetryPolicy retryPolicy = Policy.Handle<RedisConnectionException>()
            .Or<SocketException>()
            .Or<ObjectDisposedException>()
            .WaitAndRetry(RetryMaxAttempts, retry => TimeSpan.FromSeconds(2));

        return retryPolicy.Execute(() =>
        {
            ForceReconnectAsync().Wait();
            return func(_database);
        });
    }

    /// <summary>
    ///     Wait and retry async
    /// </summary>
    /// <remarks>
    ///     For Polly info, please see: https://github.com/App-vNext/Polly
    /// </remarks>
    public Task<T> WaitAndRetryAsync<T>(Func<IDatabase, Task<T>> func, CancellationToken cancellationToken)
    {
        AsyncRetryPolicy retryPolicy = Policy.Handle<RedisConnectionException>()
            .Or<SocketException>()
            .Or<ObjectDisposedException>()
            .WaitAndRetryAsync(RetryMaxAttempts, retry => TimeSpan.FromSeconds(2));

        return retryPolicy.ExecuteAsync((CancellationToken) =>
        {
            ForceReconnectAsync().Wait();
            return func(_database);
        }, cancellationToken);
    }

    /// <summary>
    /// Force a new ConnectionMultiplexer to be created.        
    /// </summary>
    /// <param name="initializing">Should only be true when ForceReconnect is running at startup.</param>
    /// <remarks>
    ///     1. Users of the ConnectionMultiplexer MUST handle ObjectDisposedExceptions, which can now happen as a result of calling ForceReconnectAsync().
    ///     2. Call ForceReconnectAsync() for RedisConnectionExceptions and RedisSocketExceptions. You can also call it for RedisTimeoutExceptions,
    ///         but only if you're using generous ReconnectMinInterval and ReconnectErrorThreshold. Otherwise, establishing new connections can cause
    ///         a cascade failure on a server that's timing out because it's already overloaded.
    ///     3. The code will:
    ///         a. wait to reconnect for at least the "ReconnectErrorThreshold" time of repeated errors before actually reconnecting
    ///         b. not reconnect more frequently than configured in "ReconnectMinInterval"
    /// </remarks>
    private async Task ForceReconnectAsync(bool initializing = false)
    {
        long previousTicks = Interlocked.Read(ref _lastReconnectTicks);
        var previousReconnectTime = new DateTimeOffset(previousTicks, TimeSpan.Zero);
        TimeSpan elapsedSinceLastReconnect = DateTimeOffset.UtcNow - previousReconnectTime;

        // We want to limit how often we perform this top-level reconnect, so we check how long it's been since our last attempt.
        if (elapsedSinceLastReconnect < ReconnectMinInterval)
        {
            return;
        }

        try
        {
            await _reconnectSemaphore.WaitAsync(RestartConnectionTimeout);
        }
        catch
        {
            // If we fail to enter the semaphore, then it is possible that another thread has already done so.
            // ForceReconnectAsync() can be retried while connectivity problems persist.
            return;
        }

        try
        {
            var utcNow = DateTimeOffset.UtcNow;
            elapsedSinceLastReconnect = utcNow - previousReconnectTime;

            if (_firstErrorTime == DateTimeOffset.MinValue && !initializing)
            {
                // We haven't seen an error since last reconnect, so set initial values.
                _firstErrorTime = utcNow;
                _previousErrorTime = utcNow;
                return;
            }

            if (elapsedSinceLastReconnect < ReconnectMinInterval)
            {
                return; // Some other thread made it through the check and the lock, so nothing to do.
            }

            TimeSpan elapsedSinceFirstError = utcNow - _firstErrorTime;
            TimeSpan elapsedSinceMostRecentError = utcNow - _previousErrorTime;

            bool shouldReconnect =
                elapsedSinceFirstError >= ReconnectErrorThreshold // Make sure we gave the multiplexer enough time to reconnect on its own if it could.
                && elapsedSinceMostRecentError <= ReconnectErrorThreshold; // Make sure we aren't working on stale data (e.g. if there was a gap in errors, don't reconnect yet).

            // Update the previousErrorTime timestamp to be now (e.g. this reconnect request).
            _previousErrorTime = utcNow;

            if (!shouldReconnect && !initializing)
            {
                return;
            }

            _firstErrorTime = DateTimeOffset.MinValue;
            _previousErrorTime = DateTimeOffset.MinValue;

            if (_connection != null)
            {
                try
                {
                    await _connection.CloseAsync();
                }
                catch
                {
                    // Ignore any errors from the old connection
                }
            }

            Interlocked.Exchange(ref _connection, null);
            ConnectionMultiplexer newConnection = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            Interlocked.Exchange(ref _connection, newConnection);

            Interlocked.Exchange(ref _lastReconnectTicks, utcNow.UtcTicks);
            IDatabase newDatabase = _connection.GetDatabase();
            Interlocked.Exchange(ref _database, newDatabase);
        }
        finally
        {
            _reconnectSemaphore.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            _connection?.Dispose();
        }
        catch { }
    }
}
