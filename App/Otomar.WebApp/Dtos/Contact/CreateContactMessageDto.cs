using System.ComponentModel.DataAnnotations;

namespace Otomar.WebApp.Dtos.Contact
{
    public class CreateContactMessageDto
    {
        [Required(ErrorMessage = "İsim alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "İsim en fazla 100 karakter olabilir.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Soyisim alanı zorunludur.")]
        [StringLength(100, ErrorMessage = "Soyisim en fazla 100 karakter olabilir.")]
        public string Surname { get; set; }

        [Required(ErrorMessage = "Telefon numarası zorunludur.")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mesaj alanı zorunludur.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Mesaj en az 10, en fazla 1000 karakter olabilir.")]
        public string Message { get; set; }

        public string? RecaptchaToken { get; set; }
    }
}
