using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Order;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("siparis")]
    public class OrderController(IOrderApi orderApi) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("musteri-siparisi-olustur")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateClientOrder([FromBody] CreateClientOrderDto dto, CancellationToken cancellationToken = default)
        {
            return await orderApi.CreateClientOrderAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("listele")]
        public async Task<IActionResult> GetOrders(CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrdersAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrderByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kod/{orderCode}")]
        public async Task<IActionResult> GetOrderByCode(string orderCode, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrderByCodeAsync(orderCode, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kullanici/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrdersByUserAsync(userId, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("musteri-siparisleri")]
        public async Task<IActionResult> GetClientOrders(CancellationToken cancellationToken = default)
        {
            return await orderApi.GetClientOrdersAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("musteri-siparisi/{id}")]
        public async Task<IActionResult> GetClientOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetClientOrderByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("musteri-siparisleri/kullanici")]
        public async Task<IActionResult> GetClientOrdersByUser(CancellationToken cancellationToken = default)
        {
            return await orderApi.GetClientOrdersByUserAsync(cancellationToken).ToActionResultAsync();
        }
    }
}