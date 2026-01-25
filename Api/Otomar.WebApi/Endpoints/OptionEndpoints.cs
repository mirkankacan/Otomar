using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Options;

namespace Otomar.WebApi.Endpoints
{
    public class OptionEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/options")
                .WithTags("Options");

            group.MapGet("/shipping", async (ShippingOptions shippingOptions, [FromServices] IClientService clientService) =>
            {
                return Results.Ok(shippingOptions);
            })
            .WithName("GetShippingOptions")
            .Produces<ShippingOptions>(200);
        }
    }
}