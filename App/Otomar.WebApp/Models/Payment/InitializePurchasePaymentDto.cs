using Otomar.WebApp.Models.Order;

namespace Otomar.WebApp.Models.Payment
{
    public class InitializePurchasePaymentDto
    {
        //public decimal TotalAmount { get; set; }

        public string CreditCardNumber { get; set; }

        public string CreditCardExpDateYear { get; set; }

        public string CreditCardExpDateMonth { get; set; }

        public string CreditCardCvv { get; set; }
        public CreateOrderDto? Order { get; set; }
    }
}