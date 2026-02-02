using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [Route("hakkimizda")]
    public class AboutController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }
    }
}