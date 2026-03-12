using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [Authorize]
    [Route("")]
    public class UserController : Controller
    {
        [HttpGet("hesabim")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("profilim")]
        public IActionResult Profile()
        {
            return View();
        }
    }
}