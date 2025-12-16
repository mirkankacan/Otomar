using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    public class ClientController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
