using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [Route("")]
    public class HomeController : Controller
    {
        [HttpGet("ana-sayfa")]
        public IActionResult Index()
        {
            return View();
        }
    }
}