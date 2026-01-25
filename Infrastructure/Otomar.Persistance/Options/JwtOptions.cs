using System.ComponentModel.DataAnnotations;

namespace Otomar.Persistance.Options
{
    public class JwtOptions
    {
        [Required]
        public string Issuer { get; set; } = default!;

        [Required]
        public string Audience { get; set; } = default!;

        [Required]
        public string SecretKey { get; set; } = default!;
    }
}