using Otomar.Application.Dtos.Order;

namespace Otomar.Application.Dtos.Payment
{
    public class InitializePaymentDto
    {
        public decimal TotalAmount { get; set; }

        public string CreditCardNumber { get; set; }

        public string CreditCardExpDateYear { get; set; }

        public string CreditCardExpDateMonth { get; set; }

        public string CreditCardCvv { get; set; }
        public CreateOrderDto? Order { get; set; }
    }
}