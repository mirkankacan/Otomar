using System.ComponentModel.DataAnnotations;

namespace Otomar.Persistance.Options
{
    public class PaymentOptions
    {
        [Required]
        public string ClientId { get; set; } = default!;

        [Required]
        public string Username { get; set; } = default!;

        [Required]
        public string Password { get; set; } = default!;

        [Required]
        public string ApiUrl { get; set; } = default!;

        [Required]
        public string StoreKey { get; set; } = default!;

        [Required]
        public string ThreeDVerificationUrl { get; set; } = default!;

        [Required]
        public string TransactionType { get; set; } = default!;

        [Required]
        public string Currency { get; set; } = default!;

        [Required]
        public string OkUrl { get; set; } = default!;

        [Required]
        public string FailUrl { get; set; } = default!;

        [Required]
        public string StoreType { get; set; } = default!;

        [Required]
        public string HashAlgorithm { get; set; } = default!;

        [Required]
        public string Lang { get; set; } = default!;

        [Required]
        public string RefreshTime { get; set; } = default!;
    }
}