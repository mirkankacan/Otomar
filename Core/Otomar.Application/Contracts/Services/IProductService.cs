using Otomar.Application.Common;
using Otomar.Application.Dtos.Product;

namespace Otomar.Application.Contracts.Services
{
    public interface IProductService
    {
        Task<ServiceResult<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequestDto productFilterRequestDto);

        Task<ServiceResult<ProductDto?>> GetProductByIdAsync(int id);

        Task<ServiceResult<ProductDto?>> GetProductByCodeAsync(string code);

        Task<ServiceResult<IEnumerable<SimilarProductDto?>>> GetSimilarProductsByCodeAsync(string code);

        Task<ServiceResult<FeaturedProductDto>> GetFeaturedProductsAsync();
    }
}