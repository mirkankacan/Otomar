using System.ComponentModel.DataAnnotations;

namespace Otomar.WebApp.Options
{
    public class ShippingOptions
    {
        [Required]
        public decimal Threshold { get; set; } = default!;

        [Required]
        public decimal Cost { get; set; } = default!;
    }
}