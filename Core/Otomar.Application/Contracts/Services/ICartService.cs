using Otomar.Application.Common;
using Otomar.Application.Dtos.Cart;

namespace Otomar.Application.Contracts.Services
{
    public interface ICartService
    {
        /// <summary>
        /// Sepete ürün ekler (varsa miktarı artırır)
        /// </summary>
        Task<ServiceResult<CartDto>> AddToCartAsync(string cartKey, AddToCartDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepetteki ürün miktarını günceller
        /// </summary>
        Task<ServiceResult<CartDto>> UpdateCartItemAsync(string cartKey, UpdateCartItemDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepetten ürün siler
        /// </summary>
        Task<ServiceResult<CartDto>> RemoveFromCartAsync(string cartKey, int productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepeti getirir
        /// </summary>
        Task<ServiceResult<CartDto>> GetCartAsync(string cartKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepeti tamamen temizler
        /// </summary>
        Task<ServiceResult> ClearCartAsync(string cartKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepet TTL'ini yeniler (kullanıcı aktifken)
        /// </summary>
        Task<ServiceResult> RefreshCartAsync(string cartKey, CancellationToken cancellationToken = default);
    }
}