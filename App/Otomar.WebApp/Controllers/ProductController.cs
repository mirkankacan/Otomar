using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Product;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("urun")]
    public class ProductController(IProductApi productApi) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("listele")]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilterRequestDto request, CancellationToken cancellationToken = default)
        {
            return await productApi.GetProductsAsync(request, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("one-cikarilanlar")]
        public async Task<IActionResult> GetFeaturedProducts(CancellationToken cancellationToken = default)
        {
            return await productApi.GetFeaturedProductsAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken = default)
        {
            return await productApi.GetProductByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kod/{code}")]
        public async Task<IActionResult> GetProductByCode(string code, CancellationToken cancellationToken = default)
        {
            return await productApi.GetProductByCodeAsync(code, cancellationToken).ToActionResultAsync();
        }
    }
}