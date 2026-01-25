using Otomar.WebApp.Dtos.Category;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface ICategoryApi
    {
        [Get("/api/categories")]
        Task<IEnumerable<CategoryDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

        [Get("/api/categories/manufacturers")]
        Task<IEnumerable<ManufacturerDto>> GetManufacturersAsync(CancellationToken cancellationToken = default);

        [Get("/api/categories/featured")]
        Task<IEnumerable<FeaturedCategoryDto>> GetFeaturedCategoriesAsync(CancellationToken cancellationToken = default);

        [Get("/api/categories/brands-models-years")]
        Task<IEnumerable<BrandModelYearDto>> GetBrandsModelsYearsAsync(CancellationToken cancellationToken = default);

        [Get("/api/categories/{brandId}/models")]
        Task<IEnumerable<ModelDto>> GetModelsByBrandAsync(int brandId, CancellationToken cancellationToken = default);

        [Get("/api/categories/{modelId}/years")]
        Task<IEnumerable<YearDto>> GetYearsByModelAsync(int modelId, CancellationToken cancellationToken = default);

        [Get("/api/categories/brands")]
        Task<IEnumerable<BrandDto>> GetBrandsAsync(CancellationToken cancellationToken = default);
    }
}
