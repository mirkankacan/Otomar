using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Product;
using Otomar.Application.Interfaces;

namespace Otomar.Application.Interfaces.Services
{
    public interface IProductService
    {
        Task<ServiceResult<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequestDto productFilterRequestDto);

        Task<ServiceResult<ProductDto?>> GetProductByIdAsync(int id, IUnitOfWork? unitOfWork = null);

        Task<ServiceResult<ProductDto?>> GetProductByCodeAsync(string code);

        Task<ServiceResult<IEnumerable<SimilarProductDto?>>> GetSimilarProductsByCodeAsync(string code);

        Task<ServiceResult<FeaturedProductDto>> GetFeaturedProductsAsync();
    }
}
