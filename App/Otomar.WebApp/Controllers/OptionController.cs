using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("secenek")]
    public class OptionController(IOptionApi optionApi) : Controller
    {
        [HttpGet("kargo")]
        public async Task<IActionResult> GetShippingOptions(CancellationToken cancellationToken = default)
        {
            return await optionApi.GetShippingOptionsAsync(cancellationToken).ToActionResultAsync();
        }
    }
}