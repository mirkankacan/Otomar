using Otomar.Contracts.Enums;

namespace Otomar.Contracts.Dtos.Payment
{
    public class PaymentDto
    {
        public Guid Id { get; set; }
        public string? UserId { get; set; }
        public string OrderCode { get; set; }
        public decimal TotalAmount { get; set; }
        public string BankCardBrand { get; set; }
        public string BankCardIssuer { get; set; }
        public PaymentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string BankProcReturnCode { get; set; }
        public string MaskedCreditCard { get; set; }
        public string? BankErrorCode { get; set; }
        public string? BankErrMsg { get; set; }
        public bool IsSuccess { get; set; }
    }
}
