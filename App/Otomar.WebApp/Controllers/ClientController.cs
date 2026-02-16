using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Order;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Interfaces;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("cari")]
    public class ClientController(IClientApi clientApi, IOrderApi orderApi, IIdentityService identityService) : Controller
    {
        [Authorize]
        [HttpGet("hesap-hareketleri")]
        public IActionResult Index()
        {
            ViewBag.ClientCode = identityService.GetClientCode();
            return View();
        }

        [Authorize]
        [HttpGet("siparis-olustur")]
        public IActionResult CreateClientOrder()
        {
            var isUserPaymentExempt = identityService.IsUserPaymentExempt();
            if (isUserPaymentExempt == false)
            {
                return RedirectToAction(nameof(PaymentController.Index), "Payment");
            }
            return View();
        }

        [Authorize]
        [HttpGet("kod/{clientCode}")]
        public async Task<IActionResult> GetClientByCode(string clientCode, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientByCodeAsync(clientCode, cancellationToken).ToActionResultAsync();
        }

        [AllowAnonymous]
        [HttpGet("vergi-tc-no/{taxNumber}")]
        public async Task<IActionResult> GetClientByTaxNumber(string taxNumber, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientByTaxTcNumberAsync(taxNumber, cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpGet("{clientCode}/hareketler")]
        public async Task<IActionResult> GetClientTransactionsByCode(string clientCode, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientTransactionsByCodeAsync(clientCode, cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpGet("{clientCode}/hareketler/paged")]
        public async Task<IActionResult> GetClientTransactionsByCodePaged(string clientCode, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            return await clientApi.GetClientTransactionsByCodePagedAsync(clientCode, pageNumber, pageSize, cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpPost("siparis-olustur")]
        public async Task<IActionResult> CreateClientOrder([FromBody] CreateClientOrderDto dto, CancellationToken cancellationToken = default)
        {
             var isUserPaymentExempt = identityService.IsUserPaymentExempt();
            if (isUserPaymentExempt == false)
            {
                return RedirectToAction(nameof(PaymentController.Index), "Payment");
            }

            return await orderApi.CreateClientOrderAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpGet("siparisler")]
        public async Task<IActionResult> GetClientOrders(CancellationToken cancellationToken = default)
        {
            return await orderApi.GetClientOrdersAsync(cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpGet("siparis/{id}")]
        public async Task<IActionResult> GetClientOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetClientOrderByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpGet("siparisler/kullanici/{userId}")]
        public async Task<IActionResult> GetClientOrdersByUser(string userId, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetClientOrdersByUserAsync(userId, cancellationToken).ToActionResultAsync();
        }

        [Authorize]
        [HttpGet("siparislerim")]
        public async Task<IActionResult> GetClientOrdersByUser(CancellationToken cancellationToken = default)
        {
            var userId = identityService.GetUserId();
            return await orderApi.GetClientOrdersByUserAsync(userId, cancellationToken).ToActionResultAsync();
        }
    }
}