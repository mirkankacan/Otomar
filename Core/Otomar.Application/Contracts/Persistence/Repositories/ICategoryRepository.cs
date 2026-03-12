using Otomar.Shared.Dtos.Category;

namespace Otomar.Application.Contracts.Persistence.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<BrandDto>> GetBrandsAsync();
        Task<(IEnumerable<BrandModelYearDto> Brands, IEnumerable<ModelDto> Models, IEnumerable<YearDto> Years)> GetBrandsModelsYearsRawAsync();
        Task<(IEnumerable<CategoryDto> Categories, IEnumerable<SubCategoryDto> SubCategories)> GetCategoriesRawAsync();
        Task<IEnumerable<FeaturedCategoryDto>> GetFeaturedCategoriesAsync();
        Task<IEnumerable<ManufacturerDto>> GetManufacturersAsync();
        Task<IEnumerable<ModelDto>> GetModelsByBrandAsync(int brandId);
        Task<IEnumerable<YearDto>> GetYearsByModelAsync(int modelId);
    }
}
