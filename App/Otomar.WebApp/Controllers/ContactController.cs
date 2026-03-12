using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Contact;
using Otomar.WebApp.Filters;

namespace Otomar.WebApp.Controllers
{
    [AllowAnonymous]
    [Route(template: "iletisim")]
    public class ContactController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("mail-gonder")]
        [ValidateRecaptcha("contact")]
        public async Task<IActionResult> SendMessage([FromBody] CreateContactMessageDto dto, CancellationToken cancellationToken = default)
        {
            // Model validasyonu
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    title = "Validasyon Hatası",
                    detail = "Lütfen tüm zorunlu alanları doldurunuz."
                });
            }

            // TODO: Mail gönderme işlemi burada yapılacak
            // Örnek: await _emailService.SendContactMessageAsync(dto);

            return Ok(new
            {
                success = true,
                message = "Mesajınız başarıyla alındı. En kısa sürede size dönüş yapacağız."
            });
        }
    }
}