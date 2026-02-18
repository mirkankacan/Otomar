using Otomar.Contracts.Common;
using Otomar.Contracts.Dtos.Category;

namespace Otomar.Application.Contracts.Services
{
    public interface ICategoryService
    {
        Task<ServiceResult<IEnumerable<CategoryDto>>> GetCategoriesAsync();

        Task<ServiceResult<IEnumerable<ManufacturerDto>>> GetManufacturersAsync();

        Task<ServiceResult<IEnumerable<FeaturedCategoryDto>>> GetFeaturedCategoriesAsync();

        Task<ServiceResult<IEnumerable<BrandModelYearDto>>> GetBrandsModelsYearsAsync();

        Task<ServiceResult<IEnumerable<ModelDto>>> GetModelsByBrandAsync(int brandId);

        Task<ServiceResult<IEnumerable<YearDto>>> GetYearsByModelAsync(int modelId);

        Task<ServiceResult<IEnumerable<BrandDto>>> GetBrandsAsync();
    }
}