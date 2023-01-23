using Cache.AzureRedis.Model;

namespace Cache.AzureRedis.Data.Repositories;

public interface IProductsRepository
{
    Task<IList<Product>> GetAsync(CancellationToken cancellationToken = default);
}
