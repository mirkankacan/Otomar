namespace Otomar.Application.Dtos.Payment
{
    public class ThreeDVerificationRequestDto
    {
        public string? TransactionType { get; set; }
        public string? OrderCode { get; set; }

        public string Amount { get; set; } = null!;
        public string? Currency { get; set; }
        public string CreditCardNumber { get; set; } = null!;
        public string CreditCardExpDateYear { get; set; } = null!;
        public string CreditCardExpDateMonth { get; set; } = null!;
        public string CreditCardCvv { get; set; } = null!;
        public string? OkUrl { get; set; }
        public string? FailUrl { get; set; }

        public string? StoreType { get; set; }
        public string? HashAlgorithm { get; set; }
        public string? Hash { get; set; }

        public string? Lang { get; set; }

        public string? RefreshTime { get; set; }

        public string? Rnd { get; set; }

        public string? Installment { get; set; }
    }
}