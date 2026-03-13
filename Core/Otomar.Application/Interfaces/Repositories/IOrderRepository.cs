using Otomar.Shared.Dtos.Cart;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Enums;

namespace Otomar.Application.Interfaces.Repositories
{
    /// <summary>
    /// Sipariş verilerine erişim sözleşmesi.
    /// Tüm transactional metodlar IUnitOfWork parametresi alır.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// IdtOrders tablosuna yeni sipariş kaydı ekler (Purchase/VirtualPOS).
        /// </summary>
        Task CreateOrderAsync(OrderInsertDto order, IUnitOfWork unitOfWork);

        /// <summary>
        /// IdtOrderItems tablosuna sipariş kalemlerini toplu ekler.
        /// </summary>
        Task CreateOrderItemsAsync(IEnumerable<OrderItemInsertDto> items, IUnitOfWork unitOfWork);

        /// <summary>
        /// IdtClientOrders tablosuna cari sipariş ekler.
        /// </summary>
        Task CreateClientOrderAsync(ClientOrderInsertDto clientOrder, IUnitOfWork unitOfWork);

        /// <summary>
        /// IdtClientOrderItems tablosuna cari sipariş kalemlerini toplu ekler.
        /// </summary>
        Task CreateClientOrderItemsAsync(IEnumerable<ClientOrderItemInsertDto> items, IUnitOfWork unitOfWork);

        /// <summary>
        /// Sipariş koduna göre sipariş getirir (IdtOrders + IdtPayments + IdtOrderItems).
        /// </summary>
        Task<OrderDto?> GetByCodeAsync(string orderCode, IUnitOfWork? unitOfWork = null);

        /// <summary>
        /// Sipariş ID'sine göre sipariş getirir (IdtOrders + IdtPayments + IdtOrderItems).
        /// </summary>
        Task<OrderDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// Tüm siparişleri getirir (IdtOrders + IdtPayments + IdtOrderItems).
        /// </summary>
        Task<IEnumerable<OrderDto>> GetAllAsync();

        /// <summary>
        /// Kullanıcıya ait tüm siparişleri getirir.
        /// </summary>
        Task<IEnumerable<OrderDto>> GetByUserAsync(string userId);

        /// <summary>
        /// Kullanıcıya ait siparişleri sayfalanmış olarak getirir.
        /// </summary>
        Task<(IEnumerable<OrderDto> Orders, int TotalCount)> GetByUserPagedAsync(string userId, int pageNumber, int pageSize);

        /// <summary>
        /// Cari sipariş ID'sine göre getirir (IdvClientOrders + IdtClientOrderItems).
        /// </summary>
        Task<ClientOrderDto?> GetClientOrderByIdAsync(Guid id);

        /// <summary>
        /// Tüm cari siparişleri getirir.
        /// </summary>
        Task<IEnumerable<ClientOrderDto>> GetAllClientOrdersAsync();

        /// <summary>
        /// Kullanıcıya ait cari siparişleri getirir.
        /// </summary>
        Task<IEnumerable<ClientOrderDto>> GetClientOrdersByUserAsync(string userId);
    }

    /// <summary>
    /// IdtOrderItems tablosuna INSERT için kullanılan DTO.
    /// </summary>
    public class OrderItemInsertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public string? ProductCode { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
    }

    /// <summary>
    /// IdtClientOrderItems tablosuna INSERT için kullanılan DTO.
    /// </summary>
    public class ClientOrderItemInsertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = default!;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
        public string? ProductCode { get; set; }
    }

    /// <summary>
    /// IdtOrders tablosuna INSERT için kullanılan DTO (Purchase/VirtualPOS).
    /// </summary>
    public class OrderInsertDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string? BuyerId { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public decimal? ShippingAmount { get; set; }
        public string? BillingName { get; set; }
        public string? BillingPhone { get; set; }
        public string? BillingCity { get; set; }
        public string? BillingDistrict { get; set; }
        public string? BillingStreet { get; set; }
        public string? ShippingName { get; set; }
        public string? ShippingPhone { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingDistrict { get; set; }
        public string? ShippingStreet { get; set; }
        public string? CorporateCompanyName { get; set; }
        public string? CorporateTaxNumber { get; set; }
        public string? CorporateTaxOffice { get; set; }
        public bool? IsEInvoiceUser { get; set; }
        public string? Email { get; set; }
        public string? IdentityNumber { get; set; }
        public OrderType OrderType { get; set; }
        public string? CartSessionId { get; set; }
    }

    /// <summary>
    /// IdtClientOrders tablosuna INSERT için kullanılan DTO.
    /// </summary>
    public class ClientOrderInsertDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string ClientAddress { get; set; } = default!;
        public string ClientPhone { get; set; } = default!;
        public string? InsuranceCompany { get; set; }
        public string? DocumentNo { get; set; }
        public string? LicensePlate { get; set; }
        public string? Note { get; set; }
        public string CreatedBy { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
    }
}
