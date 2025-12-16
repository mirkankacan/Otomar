using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class CategoryEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/categories")
                .WithTags("Categories");

            group.MapGet("/", async ([FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetCategoriesAsync();
                return result.ToGenericResult();
            })
            .WithName("GetCategories");

            group.MapGet("/manufacturers", async ([FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetManufacturersAsync();
                return result.ToGenericResult();
            })
            .WithName("GetManufacturers");

            group.MapGet("/featured", async ([FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetFeaturedCategoriesAsync();
                return result.ToGenericResult();
            })
          .WithName("GetFeaturedCategories");

            group.MapGet("/brands-models-years", async ([FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetBrandsModelsYearsAsync();
                return result.ToGenericResult();
            })
          .WithName("GetBrandsModelsYears");

            group.MapGet("/{brandId:int}/models", async (int brandId, [FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetModelsByBrandAsync(brandId);
                return result.ToGenericResult();
            })
             .WithName("GetModelsByBrand");

            group.MapGet("/{modelId:int}/years", async (int modelId, [FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetYearsByModelAsync(modelId);
                return result.ToGenericResult();
            })
            .WithName("GetYearsByModel");

            group.MapGet("/brands", async ([FromServices] ICategoryService categoryService) =>
            {
                var result = await categoryService.GetBrandsAsync();
                return result.ToGenericResult();
            })
          .WithName("GetBrands");
        }
    }
}