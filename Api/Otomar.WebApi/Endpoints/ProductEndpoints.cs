using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class ProductEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/products")
                .WithTags("Products");

            group.MapGet("/", async ([FromQuery] int pageNumber, [FromQuery] int pageSize, [FromQuery] string? orderBy, [FromQuery] string? mainCategory, [FromQuery] string? subCategory, [FromQuery] string? brand, [FromQuery] string? model, [FromQuery] string? year, [FromQuery] string? manufacturer, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] string? searchTerm, [FromServices] IProductService productService) =>
            {
                switch (pageNumber)
                {
                    case <= 0:
                        pageNumber = 1;
                        break;

                    default:
                        break;
                }
                switch (pageSize)
                {
                    case <= 10:
                        pageSize = 10;
                        break;

                    case >= 100:
                        pageSize = 100;
                        break;

                    default:
                        break;
                }
                var result = await productService.GetProductsAsync(pageNumber, pageSize, orderBy, mainCategory, subCategory, brand, model, year, manufacturer, minPrice, maxPrice, searchTerm);
                return result.ToGenericResult();
            })
            .WithName("GetProducts");

            group.MapGet("/featured", async ([FromServices] IProductService productService) =>
            {
                var result = await productService.GetFeaturedProductsAsync();
                return result.ToGenericResult();
            })
            .WithName("GetFeaturedProducts");

            group.MapGet("/{id:int}", async (int id, [FromServices] IProductService productService) =>
            {
                var result = await productService.GetProductByIdAsync(id);
                return result.ToGenericResult();
            })
             .WithName("GetProductById");

            group.MapGet(pattern: "/{code}", async (string code, [FromServices] IProductService productService) =>
            {
                var result = await productService.GetProductByCodeAsync(code);
                return result.ToGenericResult();
            })
              .WithName("GetProductByCode");
        }
    }
}