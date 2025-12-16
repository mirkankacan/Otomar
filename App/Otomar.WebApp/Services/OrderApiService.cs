using Otomar.WebApp.Models.Order;
using Otomar.WebApp.Responses;
using Otomar.WebApp.Services.Interfaces;

namespace Otomar.WebApp.Services
{
    public class OrderApiService(IApiService apiService) : IOrderApiService
    {
        private const string BaseEndpoint = "api/orders";

        public async Task<ApiResponse<Guid>> CreateOrderAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
        {
            return await apiService.PostAsync<Guid>($"{BaseEndpoint}/", dto, cancellationToken);
        }

        public async Task<ApiResponse<IEnumerable<OrderDto>>> GetOrdersAsync(CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<IEnumerable<OrderDto>>($"{BaseEndpoint}/", cancellationToken);
        }

        public async Task<ApiResponse<OrderDto>> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<OrderDto>($"{BaseEndpoint}/{id}", cancellationToken);
        }

        public async Task<ApiResponse<IEnumerable<OrderDto>>> GetOrdersByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<IEnumerable<OrderDto>>($"{BaseEndpoint}/user/{userId}", cancellationToken);
        }
    }
}

