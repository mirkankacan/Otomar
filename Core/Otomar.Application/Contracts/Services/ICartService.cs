using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Cart;
using Otomar.Application.Contracts.Persistence;

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
        Task<ServiceResult<CartDto>> GetCartAsync(CancellationToken cancellationToken = default, IUnitOfWork? unitOfWork = null);

        /// <summary>
        /// Sepeti tamamen temizler
        /// </summary>
        Task<ServiceResult> ClearCartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sepet TTL'ini yeniler (kullanıcı aktifken)
        /// </summary>
        Task<ServiceResult> RefreshCartAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Belirli bir cart key ile sepeti temizler (3D callback gibi context olmayan durumlar için)
        /// </summary>
        Task<ServiceResult> ClearCartBySessionIdAsync(string cartSessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Liste sorgu cevaplarından toplu sepet oluşturur. Her cevaplanan parça, teklif fiyatıyla sepete eklenir.
        /// </summary>
        Task<ServiceResult<CartDto>> AddToCartFromListSearchAsync(string requestNo, CancellationToken cancellationToken = default);
    }
}