using Otomar.WebApp.Dtos.Cart;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface ICartApi
    {
        [Get("/api/cart")]
        Task<CartDto> GetCartAsync(CancellationToken cancellationToken = default);

        [Post("/api/cart/items")]
        Task<CartDto> AddToCartAsync([Body] AddToCartDto dto, CancellationToken cancellationToken = default);

        [Put("/api/cart/items")]
        Task<CartDto> UpdateCartItemAsync([Body] UpdateCartItemDto dto, CancellationToken cancellationToken = default);

        [Delete("/api/cart/items/{productId}")]
        Task<CartDto> RemoveFromCartAsync(int productId, CancellationToken cancellationToken = default);

        [Delete("/api/cart")]
        Task ClearCartAsync(CancellationToken cancellationToken = default);

        [Post("/api/cart/refresh")]
        Task RefreshCartAsync(CancellationToken cancellationToken = default);
    }
}
