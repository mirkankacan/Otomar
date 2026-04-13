using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Enums;
using Otomar.WebApi.Extensions;
using static Otomar.WebApi.Extensions.RateLimitingRegistration;

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
         .WithName("CreateClientOrder")
         .RequireRateLimiting(Policies.OrderCreate);
            group.MapGet("/", async ([FromServices] IOrderService orderService) =>
            {
                var result = await orderService.GetOrdersAsync();
                return result.ToGenericResult();
            })
          .WithName("GetOrders");

            group.MapGet("/paged", async ([FromServices] IOrderService orderService, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10) =>
            {
                var result = await orderService.GetOrdersAsync(pageNumber, pageSize);
                return result.ToGenericResult();
            })
            .WithName("GetOrdersPaged")
            .RequireAuthorization("AdminOnly");

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
            group.MapGet("/user/{userId}/paged", async (string userId, [FromServices] IOrderService orderService, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10) =>
            {
                var result = await orderService.GetOrdersByUserAsync(userId, pageNumber, pageSize);
                return result.ToGenericResult();
            })
            .WithName("GetOrdersByUserPaged");

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

            group.MapPost("/resend-mail", async (
                [FromBody] ResendOrderMailDto dto,
                [FromServices] IOrderService orderService,
                [FromServices] IEmailService emailService,
                CancellationToken cancellationToken) =>
            {
                if (dto.Id is null && string.IsNullOrWhiteSpace(dto.OrderCode))
                    return Results.BadRequest("Id veya OrderCode alanlarından biri zorunludur.");

                var orderResult = dto.Id.HasValue
                    ? await orderService.GetOrderByIdAsync(dto.Id.Value)
                    : await orderService.GetOrderByCodeAsync(dto.OrderCode!);

                if (orderResult.IsFail)
                    return orderResult.ToGenericResult();

                var order = orderResult.Data!;
                if (order.Payment is null)
                    return Results.BadRequest("Bu sipariş/ödemeye ait ödeme bilgisi bulunamadı.");

                switch (order.OrderType)
                {
                    case OrderType.VirtualPOS:

                        await emailService.SendVirtualPosPaymentSuccessMailAsync(order, order.Payment, cancellationToken);
                        break;

                    case OrderType.Purchase:
                        await emailService.SendPaymentSuccessMailAsync(order, cancellationToken);
                        break;

                    default:
                        break;
                }

                return Results.NoContent();
            })
            .WithName("ResendOrderMail")
            .RequireAuthorization("AdminOnly");
        }
    }
}