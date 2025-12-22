using System.ComponentModel.DataAnnotations;

namespace Otomar.Persistance.Options
{
    public class ShippingOptions
    {
        [Required]
        public decimal Threshold { get; set; } = default!;

        [Required]
        public decimal Cost { get; set; } = default!;
    }
}