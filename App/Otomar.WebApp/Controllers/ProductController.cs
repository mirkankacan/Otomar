using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [Route("urun")]
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}