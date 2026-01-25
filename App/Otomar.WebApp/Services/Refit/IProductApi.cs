using Otomar.WebApp.Common;
using Otomar.WebApp.Dtos.Product;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IProductApi
    {
        [Get("/api/products")]
        Task<PagedResult<ProductDto>> GetProductsAsync([Query] ProductFilterRequestDto request, CancellationToken cancellationToken = default);

        [Get("/api/products/featured")]
        Task<FeaturedProductDto> GetFeaturedProductsAsync(CancellationToken cancellationToken = default);

        [Get("/api/products/{id}")]
        Task<ProductDto> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);

        [Get("/api/products/{code}")]
        Task<ProductDto> GetProductByCodeAsync(string code, CancellationToken cancellationToken = default);
    }
}
