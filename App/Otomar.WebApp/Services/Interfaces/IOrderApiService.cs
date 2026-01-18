using Otomar.WebApp.Models.Order;
using Otomar.WebApp.Responses;

namespace Otomar.WebApp.Services.Interfaces
{
    public interface IOrderApiService
    {
        Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

        Task<ApiResponse<IEnumerable<OrderDto>>> GetOrdersAsync(CancellationToken cancellationToken = default);

        Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<ApiResponse<OrderDto>> GetOrderByCodeAsync(string orderCode, CancellationToken cancellationToken = default);

        Task<ApiResponse<IEnumerable<OrderDto>>> GetOrdersByUserAsync(string userId, CancellationToken cancellationToken = default);
    }
}