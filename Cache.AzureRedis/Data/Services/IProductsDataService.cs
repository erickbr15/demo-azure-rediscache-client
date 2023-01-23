using Cache.AzureRedis.Model;

namespace Cache.AzureRedis.Data.Services;

public interface IProductsDataService
{
    Task<IList<Product>> GetAsync(CancellationToken cancellationToken = default);
}
