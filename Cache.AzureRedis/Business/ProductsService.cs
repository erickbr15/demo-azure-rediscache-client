using Cache.AzureRedis.Data.Services;
using Cache.AzureRedis.Model;

namespace Cache.AzureRedis.Business;

public class ProductsService : IProductsService
{
    private readonly IProductsDataService _dataService;

    public ProductsService(IProductsDataService productsDataService)
    {
        _dataService = productsDataService ?? throw new ArgumentNullException(nameof(productsDataService));
    }

    public Task<IList<Product>> GetProductCatalogAsync(CancellationToken cancellationToken)
    {
        return _dataService.GetAsync(cancellationToken);
    }
}
