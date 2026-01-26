using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Product;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class ProductEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/products")
                .WithTags("Products");

            // Endpoint
            group.MapGet("/", async ([AsParameters] ProductFilterRequestDto request,
                                     [FromServices] IProductService productService) =>
            {
                var result = await productService.GetProductsAsync(request);
                return result.ToGenericResult();
            })
            .WithName("GetProducts");

            group.MapGet("/featured", async ([FromServices] IProductService productService) =>
            {
                var result = await productService.GetFeaturedProductsAsync();
                return result.ToGenericResult();
            })
            .WithName("GetFeaturedProducts")
            .CacheOutput("FeaturedProductsCache");

            group.MapGet("/{id:int}", async (int id, [FromServices] IProductService productService) =>
            {
                var result = await productService.GetProductByIdAsync(id);
                return result.ToGenericResult();
            })
             .WithName("GetProductById")
              .CacheOutput("ProductByIdCache");

            group.MapGet(pattern: "/{code}", async (string code, [FromServices] IProductService productService) =>
            {
                var result = await productService.GetProductByCodeAsync(code);
                return result.ToGenericResult();
            })
              .WithName("GetProductByCode")
               .CacheOutput("ProductByCodeCache");
        }
    }
}