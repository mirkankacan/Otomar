using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Client;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("cari")]
    public class ClientController(IClientApi clientApi) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("kod/{clientCode}")]
        public async Task<IActionResult> GetClientByCode(string clientCode, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientByCodeAsync(clientCode, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("vergi-no/{taxNumber}")]
        public async Task<IActionResult> GetClientByTaxNumber(string taxNumber, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientByTaxNumberAsync(taxNumber, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("{clientCode}/hareketler")]
        public async Task<IActionResult> GetClientTransactionsByCode(string clientCode, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientTransactionsByCodeAsync(clientCode, cancellationToken).ToActionResultAsync();
        }
    }
}
