using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Product;

namespace Otomar.Application.Contracts.Persistence.Repositories
{
    /// <summary>
    /// Product data access operations.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Returns global filter settings for the specified user.
        /// </summary>
        Task<List<(string FilterType, string FilterValue)>> GetUserGlobalFiltersAsync(string userId);

        /// <summary>
        /// Returns featured products (latest, best seller, lowest/highest price).
        /// </summary>
        Task<FeaturedProductDto> GetFeaturedProductsAsync(List<(string FilterType, string FilterValue)> globalFilters);

        /// <summary>
        /// Returns a single product matching the given stock code.
        /// </summary>
        Task<ProductDto?> GetByCodeAsync(string code);

        /// <summary>
        /// Returns a single product matching the given ID.
        /// </summary>
        Task<ProductDto?> GetByIdAsync(int id, IUnitOfWork? unitOfWork = null);

        /// <summary>
        /// Returns a paged list of products matching the specified filter and global filters.
        /// </summary>
        Task<PagedResult<ProductDto>> GetFilteredAsync(
            ProductFilterRequestDto filter,
            List<(string FilterType, string FilterValue)> globalFilters);

        /// <summary>
        /// Returns similar products based on the given stock code.
        /// </summary>
        Task<IEnumerable<SimilarProductDto?>> GetSimilarByCodeAsync(string code);
    }
}
