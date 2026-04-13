using Carter;
using Otomar.Application.Options;

namespace Otomar.WebApi.Endpoints
{
    public class OptionEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/options")
                .WithTags("Options");

            group.MapGet("/shipping", async (ShippingOptions shippingOptions) =>
            {
                return Results.Ok(shippingOptions);
            })
            .WithName("GetShippingOptions")
            .Produces<ShippingOptions>(200);
        }
    }
}