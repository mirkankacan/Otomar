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
                400 => View("BadRequest"),
                403 => View("Forbidden"),
                404 => View("NotFound"),
                500 => View("ServerError"),
                503 => View("ServiceUnavailable"),
                _ => View("General")
            };
        }
    }
}
