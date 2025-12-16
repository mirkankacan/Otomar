using Otomar.Application.Dtos.Payment;
using Otomar.Application.Enums;

namespace Otomar.Application.Dtos.Order
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string? BuyerId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Email { get; set; }
        public string? IdentityNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public IEnumerable<OrderItemDto> Items { get; set; } = Enumerable.Empty<OrderItemDto>();
        public AddressDto? BillingAddress { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
        public PaymentDto Payment { get; set; }
    }
}