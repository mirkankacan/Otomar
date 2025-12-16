using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Models.Order;
using Otomar.WebApp.Services.Interfaces;

namespace Otomar.WebApp.Controllers
{
    public class OrderController(IOrderApiService orderApiService) : Controller
    {
        public async Task<IActionResult> CreateOrder(CreateOrderDto createOrderDto, CancellationToken cancellationToken)
        {
            var createOrder = await orderApiService.CreateOrderAsync(createOrderDto, cancellationToken);
            return createOrder.IsSuccess ? Json(createOrder.Data) : Json(createOrder.ErrorMessage);
        }
    }
}