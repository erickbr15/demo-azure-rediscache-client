using Cache.AzureRedis.Business;
using Microsoft.AspNetCore.Mvc;

namespace Cache.AzureRedis.Web.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsService _productsService;

        public ProductsController(IProductsService productsService)
        {
            _productsService= productsService ?? throw new ArgumentNullException(nameof(productsService));
        }
        
        [HttpGet(Name = "GetProductsCatalog")]
        public async Task<ActionResult> Get(CancellationToken cancellationToken)
        {
            var products = await _productsService.GetProductCatalogAsync(cancellationToken);
            return new OkObjectResult(products);
        }
    }
}
