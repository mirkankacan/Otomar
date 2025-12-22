using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Payment;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class PaymentEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/payments")
                .WithTags("Payments");

            group.MapPost("/", async ([FromBody] Dictionary<string, string> parameters, [FromServices] IPaymentService paymentService, CancellationToken cancellationToken) =>
            {
                var result = await paymentService.CreatePaymentAsync(parameters, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("CreatePayment");

            group.MapPost("/initialize", async ([FromBody] InitializePaymentDto initializePaymentDto, [FromServices] IPaymentService paymentService, [FromServices] IOrderService orderService, CancellationToken cancellationToken) =>
            {
                var result = await paymentService.InitializePaymentAsync(initializePaymentDto, cancellationToken);
                return result.ToGenericResult();
            })
             .WithName("InitializePayment");

            group.MapGet("/", async ([FromServices] IPaymentService paymentService) =>
            {
                var result = await paymentService.GetPaymentsAsync();
                return result.ToGenericResult();
            })
          .WithName("GetPayments");
            group.MapGet("/params/{orderCode}", async (string orderCode, [FromServices] IPaymentService paymentService, CancellationToken cancellationToken) =>
            {
                // Cache'den parametreleri almak için
                var result = await paymentService.GetPaymentParamsAsync(orderCode, cancellationToken);
                return result.ToGenericResult();
            })
          .WithName("GetParams");
            group.MapGet("/user/{userId}", async (string userId, [FromServices] IPaymentService paymentService) =>
            {
                var result = await paymentService.GetPaymentsByUserAsync(userId);
                return result.ToGenericResult();
            })
              .WithName("GetPaymentsByUser");

            group.MapGet("/{paymentId:guid}", async (Guid paymentId, [FromServices] IPaymentService paymentService) =>
            {
                var result = await paymentService.GetPaymentByIdAsync(paymentId);
                return result.ToGenericResult();
            })
            .WithName("GetPaymentById");
        }
    }
}