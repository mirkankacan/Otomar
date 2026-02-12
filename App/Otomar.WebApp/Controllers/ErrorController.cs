using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [Route("hata")]
    public class ErrorController : Controller
    {
        [Route("{statusCode}")]
        public IActionResult Index(int statusCode)
        {
            Response.StatusCode = statusCode;

            return statusCode switch
            {
                404 => View("NotFound"),
                _ => View("General")
            };
        }
    }
}
