using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.Shared.Dtos.Order;
using Otomar.WebApp.Extensions;
using Otomar.Shared.Interfaces;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Authorize]
    [Route("siparis")]
    public class OrderController(IOrderApi orderApi, IIdentityService identityService) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("listele")]
        public async Task<IActionResult> GetOrders(CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrdersAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrderByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kod/{orderCode}")]
        public async Task<IActionResult> GetOrderByCode(string orderCode, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrderByCodeAsync(orderCode, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("siparislerim")]
        public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken = default)
        {
            var userId = identityService.GetUserId();
            return await orderApi.GetOrdersByUserAsync(userId, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("siparislerim/paged")]
        public async Task<IActionResult> GetMyOrdersPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            var userId = identityService.GetUserId();
            return await orderApi.GetOrdersByUserPagedAsync(userId, pageNumber, pageSize, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kullanici/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrdersByUserAsync(userId, cancellationToken).ToActionResultAsync();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/tum-siparisler")]
        public IActionResult AllOrders()
        {
            return View("~/Views/User/AllOrders.cshtml");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin/tum-siparisler/paged")]
        public async Task<IActionResult> GetAllOrdersPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
        {
            return await orderApi.GetOrdersPagedAsync(pageNumber, pageSize, cancellationToken).ToActionResultAsync();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("admin/resend-mail")]
        public async Task<IActionResult> ResendOrderMail([FromBody] ResendOrderMailDto dto, CancellationToken cancellationToken = default)
        {
            return await orderApi.ResendOrderMailAsync(dto, cancellationToken).ToActionResultAsync();
        }
    }
}