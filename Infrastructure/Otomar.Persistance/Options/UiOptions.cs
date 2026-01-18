using System.ComponentModel.DataAnnotations;

namespace Otomar.Persistance.Options
{
    public class UiOptions
    {
        [Required]
        public string BaseUrl { get; set; } = default!;

        [Required]
        public string PaymentSuccessUrl { get; set; } = default!;

        [Required]
        public string PaymentFailureUrl { get; set; } = default!;
    }
}