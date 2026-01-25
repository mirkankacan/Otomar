using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Product;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;
using Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("")]
    public class HomeController(IProductApi productApi) : Controller
    {
        [HttpGet("ana-sayfa")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            try
            {
                var featuredProducts = await productApi.GetFeaturedProductsAsync(cancellationToken);
                if (featuredProducts != null)
                {
                    ViewBag.FeaturedProducts = featuredProducts;
                }
            }
            catch (ApiException)
            {
                // Hata durumunda ViewBag boş kalır
            }
            return View();
        }
    }
}