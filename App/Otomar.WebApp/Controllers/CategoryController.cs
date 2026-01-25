using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("kategori")]
    public class CategoryController(ICategoryApi categoryApi) : Controller
    {
        [HttpGet("listele")]
        public async Task<IActionResult> GetCategories(CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetCategoriesAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("ureticiler")]
        public async Task<IActionResult> GetManufacturers(CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetManufacturersAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("onerilenler")]
        public async Task<IActionResult> GetFeaturedCategories(CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetFeaturedCategoriesAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("marka-model-yil")]
        public async Task<IActionResult> GetBrandsModelsYears(CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetBrandsModelsYearsAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("marka/{brandId}/modeller")]
        public async Task<IActionResult> GetModelsByBrand(int brandId, CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetModelsByBrandAsync(brandId, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("model/{modelId}/yillar")]
        public async Task<IActionResult> GetYearsByModel(int modelId, CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetYearsByModelAsync(modelId, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("markalar")]
        public async Task<IActionResult> GetBrands(CancellationToken cancellationToken = default)
        {
            return await categoryApi.GetBrandsAsync(cancellationToken).ToActionResultAsync();
        }
    }
}