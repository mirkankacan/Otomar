using Otomar.WebApp.Enums;

namespace Otomar.WebApp.Dtos.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public decimal? ShippingAmount { get; set; }
        public string BankCardBrand { get; set; }
        public string BankCardIssuer { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BankProcReturnCode { get; set; }
        public string MaskedCreditCard { get; set; }
        public bool IsSuccess => BankProcReturnCode == "00" ? true : false;
    }
}
