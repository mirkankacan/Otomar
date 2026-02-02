using Otomar.Application.Common;
using Otomar.Application.Dtos.Order;
using System.Data;

namespace Otomar.Application.Contracts.Services
{
    public interface IOrderService
    {
        Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersByUserAsync(string userId);

        Task<ServiceResult<PagedResult<OrderDto>>> GetOrdersByUserAsync(string userId, int pageNumber, int pageSize);

        Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersAsync();

        Task<ServiceResult<OrderDto>> GetOrderByIdAsync(Guid id);

        Task<ServiceResult<OrderDto>> GetOrderByCodeAsync(string orderCode, IDbTransaction transaction = null);

        Task<ServiceResult<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, IDbTransaction transaction, CancellationToken cancellationToken);

        Task<ServiceResult<Guid>> CreateVirtualPosOrderAsync(CreateVirtualPosOrderDto dto, IDbTransaction transaction, CancellationToken cancellationToken);

        Task<ServiceResult<Guid>> CreateClientOrderAsync(CreateClientOrderDto createClientOrderDto, CancellationToken cancellationToken);

        Task<ServiceResult<IEnumerable<ClientOrderDto>>> GetClientOrdersAsync();

        Task<ServiceResult<ClientOrderDto>> GetClientOrderByIdAsync(Guid id);

        Task<ServiceResult<IEnumerable<ClientOrderDto>>> GetClientOrdersByUserAsync(string userId);
    }
}