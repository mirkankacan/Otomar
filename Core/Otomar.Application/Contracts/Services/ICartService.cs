using Otomar.Application.Common;
using Otomar.Application.Dtos.Cart;
using System.Data;

namespace Otomar.Application.Contracts.Services
{
    public interface ICartService
    {
        /// <summary>
        /// Sepete ürün ekler (varsa miktarı artırır)
        /// </summary>
        Task<ServiceResult<CartDto>> AddToCartAsync(AddToCartDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepetteki ürün miktarını günceller
        /// </summary>
        Task<ServiceResult<CartDto>> UpdateCartItemAsync(UpdateCartItemDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepetten ürün siler
        /// </summary>
        Task<ServiceResult<CartDto>> RemoveFromCartAsync(int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepeti getirir
        /// </summary>
        Task<ServiceResult<CartDto>> GetCartAsync(CancellationToken cancellationToken = default, IDbTransaction? transaction = null);

        /// <summary>
        /// Sepeti tamamen temizler
        /// </summary>
        Task<ServiceResult> ClearCartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepet TTL'ini yeniler (kullanıcı aktifken)
        /// </summary>
        Task<ServiceResult> RefreshCartAsync(CancellationToken cancellationToken = default);
    }
}