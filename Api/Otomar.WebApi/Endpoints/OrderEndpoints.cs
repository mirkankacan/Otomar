using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Order;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class OrderEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/orders")
                .WithTags("Orders");

            group.MapPost("/", async ([FromBody] CreateOrderDto dto, [FromServices] IOrderService orderService, CancellationToken cancellationToken) =>
            {
                var result = await orderService.CreateOrderAsync(dto);
                return result.ToGenericResult();
            })
            .WithName("CreateOrder");

            group.MapGet("/", async ([FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrdersAsync();
                return result.ToGenericResult();
            })
          .WithName("GetOrders");

            group.MapGet("/{id}", async (Guid id, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrderByIdAsync(id);
                return result.ToGenericResult();
            })
              .WithName("GetOrderById");

            group.MapGet("/user/{userId}", async (string userId, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrdersByUserAsync(userId);
                return result.ToGenericResult();
            })
            .WithName("GetOrdersByUser");
        }
    }
}