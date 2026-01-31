using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Product;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Helpers;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("urun")]
    public class ProductController(IProductApi productApi) : Controller
    {
        [HttpGet("{stockName}/{stockCode}")]
        public async Task<IActionResult> Index(string stockName, string stockCode, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(stockName) || string.IsNullOrEmpty(stockCode))
                return Redirect("/ana-sayfa");

            var response = await productApi.GetProductByCodeAsync(stockCode, cancellationToken);
            if (response == null)
                return Redirect("/ana-sayfa");

            var requestSlug = SlugHelper.Generate(stockName);
            var responseSlug = SlugHelper.Generate(response.STOK_ADI);

            if (!requestSlug.Equals(responseSlug, StringComparison.OrdinalIgnoreCase))
            {
                // Doðru URL'ye 301 (Permanent Redirect) ile yönlendir
                return RedirectPermanent($"/urun/{responseSlug}/{stockCode}");
            }

            return View(response);
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

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken = default)
        {
            return await productApi.GetProductByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kod/{code}")]
        public async Task<IActionResult> GetProductByCode(string code, CancellationToken cancellationToken = default)
        {
            return await productApi.GetProductByCodeAsync(code, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("benzer/{stokKodu}")]
        public async Task<IActionResult> GetSimilarProducts(string stokKodu, CancellationToken cancellationToken = default)
        {
            return await productApi.GetSimilarProductsByCodeAsync(stokKodu, cancellationToken).ToActionResultAsync();
        }
    }
}