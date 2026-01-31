namespace Otomar.Application.Dtos.Payment
{
    public class InitializeVirtualPosPaymentDto
    {
        public string CreditCardNumber { get; set; }

        public string CreditCardExpDateYear { get; set; }

        public string CreditCardExpDateMonth { get; set; }

        public string CreditCardCvv { get; set; }

        public decimal Amount { get; set; }
        public string? Email { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public string ClientName { get; set; }
    }
}