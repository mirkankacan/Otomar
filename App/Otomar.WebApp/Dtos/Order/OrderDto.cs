using Otomar.WebApp.Dtos.Payment;
using Otomar.WebApp.Enums;

namespace Otomar.WebApp.Dtos.Order
{
    public class OrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string? BuyerId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public OrderType OrderType { get; set; }

        public string? Email { get; set; }
        public string? IdentityNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? ShippingAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public AddressDto? BillingAddress { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
        public PaymentDto? Payment { get; set; }
    }
}
