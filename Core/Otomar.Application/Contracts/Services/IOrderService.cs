using Otomar.Application.Common;
using Otomar.Application.Dtos.Order;
using System.Data;

namespace Otomar.Application.Contracts.Services
{
    public interface IOrderService
    {
        Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersByUserAsync(string userId);

        Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersAsync();

        Task<ServiceResult<OrderDto>> GetOrderByIdAsync(Guid id);

        Task<ServiceResult<Guid>> CreateOrderAsync(CreateOrderDto createOrderDto, IDbTransaction transaction);

        Task<ServiceResult<Guid>> CreateOrderAsync(CreateOrderDto createOrderDto);
    }
}