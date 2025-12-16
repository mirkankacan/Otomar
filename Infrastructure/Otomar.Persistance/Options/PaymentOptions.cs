using System.ComponentModel.DataAnnotations;

namespace Otomar.Persistance.Options
{
    public class PaymentOptions
    {
        [Required]
        public string ClientId { get; set; } = default!;

        [Required]
        public string UserName { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        [Required]
        public string ApiUrl { get; set; } = default!;

        [Required]
        public decimal ShippingThreshold { get; set; } = default!;
    }
}