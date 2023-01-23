using Cache.AzureRedis.Cache;
using Cache.AzureRedis.Data.Repositories;
using Cache.AzureRedis.Model;

namespace Cache.AzureRedis.Data.Services;

public class ProductsDataService : IProductsDataService
{
    private readonly IProductsRepository _repository;
    private readonly IDistributedCacheAdapter _cache;
    private static string ProductCatalogKey = "ProductCatalog";

    public ProductsDataService(IProductsRepository repository, IDistributedCacheAdapter distributedCacheAdapter)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = distributedCacheAdapter ?? throw new ArgumentNullException(nameof(distributedCacheAdapter));
    }

    public async Task<IList<Product>> GetAsync(CancellationToken cancellationToken = default)
    {
        var products = await _cache.GetAsync<List<Product>>(ProductCatalogKey, cancellationToken);
        if (products != null)
        {
            return products;
        }

        var productsFromDb = (await _repository.GetAsync(cancellationToken)).ToList();
        await _cache.SetAsync<List<Product>>(ProductCatalogKey, productsFromDb, cancellationToken);

        return productsFromDb;
    }
}