using Cache.AzureRedis.Model;

namespace Cache.AzureRedis.Data.Repositories;

public class ProductsRepository : IProductsRepository
{
    public Task<IList<Product>> GetAsync(CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        for (int i = 0; i < 20; i++)
        {
            products.Add(new Product
            {
                Id = Guid.NewGuid(),
                Name = $"product-{i + 1}",
                Description = $"description-{i + 1}",
                Price = 45.0M,
                Sku = $"product-{i + 1}-sku"
            });
        }

        return Task.FromResult<IList<Product>>(products);
    }
}
