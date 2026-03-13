using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Otomar.Shared.Common;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Interfaces;
using Otomar.Shared.Dtos.Product;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using System.Net;

namespace Otomar.Application.Services
{
    public class ProductService(
        IProductRepository productRepository,
        ILogger<ProductService> logger,
        IIdentityService identityService,
        IHttpContextAccessor httpContextAccessor) : IProductService
    {
        public async Task<ServiceResult<FeaturedProductDto>> GetFeaturedProductsAsync()
        {
            var globalFilters = await GetUserGlobalFiltersAsync();
            var homePageProducts = await productRepository.GetFeaturedProductsAsync(globalFilters);
            return ServiceResult<FeaturedProductDto>.SuccessAsOk(homePageProducts);
        }

        public async Task<ServiceResult<ProductDto?>> GetProductByCodeAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                return ServiceResult<ProductDto?>.Error("Geçersiz Stok Kodu", "Stok kodu boş geçilemez", HttpStatusCode.BadRequest);
            }

            var result = await productRepository.GetByCodeAsync(code);
            if (result == null)
            {
                logger.LogWarning($"'{code}' stok kodlu ürün bulunamadı");
                return ServiceResult<ProductDto?>.Error("Ürün Bulunamadı", $"'{code}' stok kodlu ürün bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<ProductDto?>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<ProductDto?>> GetProductByIdAsync(int id, IUnitOfWork? unitOfWork = null)
        {
            var result = await productRepository.GetByIdAsync(id, unitOfWork);
            if (result == null)
            {
                logger.LogWarning($"'{id}' ID'li ürün bulunamadı");
                return ServiceResult<ProductDto?>.Error("Ürün Bulunamadı", $"'{id}' ID'li ürün bulunamadı", HttpStatusCode.NotFound);
            }

            return ServiceResult<ProductDto?>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<PagedResult<ProductDto>>> GetProductsAsync(ProductFilterRequestDto productFilterRequestDto)
        {
            var globalFilters = await GetUserGlobalFiltersAsync();
            var pagedResult = await productRepository.GetFilteredAsync(productFilterRequestDto, globalFilters);
            return ServiceResult<PagedResult<ProductDto>>.SuccessAsOk(pagedResult);
        }

        public async Task<ServiceResult<IEnumerable<SimilarProductDto?>>> GetSimilarProductsByCodeAsync(string code)
        {
            var result = await productRepository.GetSimilarByCodeAsync(code);
            return ServiceResult<IEnumerable<SimilarProductDto?>>.SuccessAsOk(result);
        }

        private async Task<List<(string FilterType, string FilterValue)>> GetUserGlobalFiltersAsync()
        {
            if (httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated != true)
                return [];

            var userId = identityService.GetUserId();
            if (string.IsNullOrEmpty(userId))
                return [];

            return await productRepository.GetUserGlobalFiltersAsync(userId);
        }
    }
}
