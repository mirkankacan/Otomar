using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("")]
    public class HomeController(IProductApi productApi) : Controller
    {
        [HttpGet("ana-sayfa")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {

            return View();
        }
    }
}