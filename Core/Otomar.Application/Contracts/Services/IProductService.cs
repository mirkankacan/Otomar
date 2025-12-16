using Otomar.Application.Common;
using Otomar.Application.Dtos.Product;

namespace Otomar.Application.Contracts.Services
{
    public interface IProductService
    {
        Task<ServiceResult<PagedResult<ProductDto>>> GetProductsAsync(int pageNumber, int pageSize, string? orderBy, string? mainCategory, string? subCategory, string? brand, string? model, string? year, string? manufacturer, decimal? minPrice, decimal? maxPrice, string? searchTerm);

        Task<ServiceResult<ProductDto?>> GetProductByIdAsync(int id);

        Task<ServiceResult<ProductDto?>> GetProductByCodeAsync(string code);

        Task<ServiceResult<IEnumerable<SimilarProductDto?>>> GetSimilarProductsByCodeAsync(string code);

        Task<ServiceResult<FeaturedProductDto>> GetFeaturedProductsAsync();
    }
}