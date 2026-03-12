using Otomar.Application.Contracts.Persistence;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Cart;
using Otomar.Shared.Dtos.Order;

namespace Otomar.Application.Contracts.Services
{
    public interface IOrderService
    {
        Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersByUserAsync(string userId);

        Task<ServiceResult<PagedResult<OrderDto>>> GetOrdersByUserAsync(string userId, int pageNumber, int pageSize);

        Task<ServiceResult<IEnumerable<OrderDto>>> GetOrdersAsync();

        Task<ServiceResult<OrderDto>> GetOrderByIdAsync(Guid id);

        Task<ServiceResult<OrderDto>> GetOrderByCodeAsync(string orderCode, IUnitOfWork? unitOfWork = null);

        Task<ServiceResult<Guid>> CreatePurchaseOrderAsync(CreatePurchaseOrderDto dto, CartDto cart, IUnitOfWork unitOfWork, CancellationToken cancellationToken);

        Task<ServiceResult<Guid>> CreateVirtualPosOrderAsync(CreateVirtualPosOrderDto dto, IUnitOfWork unitOfWork, CancellationToken cancellationToken);

        Task<ServiceResult<Guid>> CreateClientOrderAsync(CreateClientOrderDto createClientOrderDto, CancellationToken cancellationToken);

        Task<ServiceResult<IEnumerable<ClientOrderDto>>> GetClientOrdersAsync();

        Task<ServiceResult<ClientOrderDto>> GetClientOrderByIdAsync(Guid id);

        Task<ServiceResult<IEnumerable<ClientOrderDto>>> GetClientOrdersByUserAsync(string userId);
    }
}
