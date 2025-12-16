using Otomar.WebApp.Enums;

namespace Otomar.WebApp.Models.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string OrderCode { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string CardNumber { get; set; }
        public string CardBrand { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

