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

            group.MapPost("/client-order", async ([FromBody] CreateClientOrderDto dto, [FromServices] IOrderService orderService, CancellationToken cancellationToken) =>
            {
                var result = await orderService.CreateClientOrderAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
         .WithName("CreateClientOrder");
            group.MapGet("/", async ([FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrdersAsync();
                return result.ToGenericResult();
            })
          .WithName("GetOrders");

            group.MapGet("/{id:guid}", async (Guid id, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrderByIdAsync(id);
                return result.ToGenericResult();
            })
              .WithName("GetOrderById");
            group.MapGet("/{orderCode}", async (string orderCode, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrderByCodeAsync(orderCode);
                return result.ToGenericResult();
            })
          .WithName("GetOrderByCode");
            group.MapGet("/user/{userId}", async (string userId, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrdersByUserAsync(userId);
                return result.ToGenericResult();
            })
            .WithName("GetOrdersByUser");

            group.MapGet("/client-orders", async ([FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetClientOrdersAsync();
                return result.ToGenericResult();
            })
             .WithName("GetClientOrders");

            group.MapGet("/client-orders/{id:guid}", async (Guid id, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetClientOrderByIdAsync(id);
                return result.ToGenericResult();
            })
          .WithName("GetClientOrderById");
            group.MapGet("/client-orders/user/{userId}", async (string userId, [FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetClientOrdersByUserAsync(userId);
                return result.ToGenericResult();
            })
             .WithName("GetClientOrdersByUser");
        }
    }
}