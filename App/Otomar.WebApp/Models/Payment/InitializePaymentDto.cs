using System.ComponentModel.DataAnnotations;

namespace Otomar.WebApp.Models.Payment
{
    public class InitializePaymentDto
    {
        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string CreditCardNumber { get; set; }

        [Required]
        public string CreditCardExpDateYear { get; set; }

        [Required]
        public string CreditCardExpDateMonth { get; set; }

        [Required]
        public string CreditCardCvv { get; set; }
    }
}