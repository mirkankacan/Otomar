using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
