using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [Route("")]
    public class OptionController(IOptionApi optionApi) : Controller
    {
        [HttpGet("kargo-sinir")]
        public async Task<IActionResult> GetShippingOptions(CancellationToken cancellationToken = default)
        {
            return await optionApi.GetShippingOptionsAsync(cancellationToken).ToActionResultAsync();
        }
    }
}