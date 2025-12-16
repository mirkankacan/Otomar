using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
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

            group.MapGet("/", async ([FromServices] IPaymentService paymentService) =>
            {
                var result = await paymentService.GetPaymentsAsync();
                return result.ToGenericResult();
            })
          .WithName("GetPayments");

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