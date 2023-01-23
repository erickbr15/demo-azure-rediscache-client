using Cache.AzureRedis.Model;

namespace Cache.AzureRedis.Business;

public interface IProductsService
{
    Task<IList<Product>> GetProductCatalogAsync(CancellationToken cancellationToken);
}
