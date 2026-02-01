using Otomar.WebApp.Dtos.Order;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IOrderApi
    {
        [Post("/api/orders/client-order")]
        Task<ClientOrderDto> CreateClientOrderAsync([Body] CreateClientOrderDto dto, CancellationToken cancellationToken = default);

        [Get("/api/orders")]
        Task<IEnumerable<OrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default);

        [Get("/api/orders/{id}")]
        Task<OrderDto> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

        [Get("/api/orders/{orderCode}")]
        Task<OrderDto> GetOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default);

        [Get("/api/orders/user/{userId}")]
        Task<IEnumerable<OrderDto>> GetOrdersByUserAsync(string userId, CancellationToken cancellationToken = default);

        [Get("/api/orders/client-orders")]
        Task<IEnumerable<ClientOrderDto>> GetClientOrdersAsync(CancellationToken cancellationToken = default);

        [Get("/api/orders/client-orders/{id}")]
        Task<ClientOrderDto> GetClientOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

        [Get("/api/orders/client-orders/user/{userId}")]
        Task<IEnumerable<ClientOrderDto>> GetClientOrdersByUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}