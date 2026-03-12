using Otomar.Shared.Dtos.Order;

namespace Otomar.Shared.Dtos.Payment
{
    public class InitializePurchasePaymentDto
    {
        public string CreditCardNumber { get; set; }
        public string CreditCardExpDateYear { get; set; }
        public string CreditCardExpDateMonth { get; set; }
        public string CreditCardCvv { get; set; }
        public CreatePurchaseOrderDto? Order { get; set; }
    }
}
