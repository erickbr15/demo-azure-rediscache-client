using Cache.AzureRedis.Business;
using Cache.AzureRedis.Cache;
using Cache.AzureRedis.Data.Repositories;
using Cache.AzureRedis.Data.Services;
using Microsoft.Extensions.Caching.Distributed;

namespace Cache.AzureRedis.Web.Api;

public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Register Cache services
    /// </summary>
    public static IServiceCollection AddDistributedCacheServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RedisConnectionString");
        var connectionFactoryInitializer = async () =>
        {
            return await RedisConnection.InitializeAsync(connectionString);
        };

        var connection = connectionFactoryInitializer.Invoke().Result;

        var distributedCacheEntryOptions = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(300)
        };

        services.AddSingleton(distributedCacheEntryOptions);
        services.AddSingleton(connection);
        services.AddSingleton<IDistributedCache, DistributedCacheFacade>();
        services.AddSingleton<IDistributedCacheAdapter, DistributedCacheAdapter>();

        return services;
    }

    /// <summary>
    ///     Register products services
    /// </summary>
    public static IServiceCollection AddProductsServices(this IServiceCollection services)
    {
        services.AddTransient<IProductsRepository, ProductsRepository>();
        services.AddTransient<IProductsDataService, ProductsDataService>();
        services.AddTransient<IProductsService, ProductsService>();

        return services;
    }
}

