using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Models.Payment;
using Otomar.WebApp.Services.Interfaces;

namespace Otomar.WebApp.Controllers
{
    [Route("odeme")]
    public class PaymentController(IPaymentApiService paymentApiService, ILogger<PaymentController> logger) : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("sanal-pos")]
        public IActionResult VirtualPos()
        {
            return View();
        }

        [HttpPost("baslat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await paymentApiService.InitializePaymentAsync(dto, cancellationToken);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = "Ödeme başlatılamadı. Lütfen tekrar deneyiniz.";
                    return BadRequest("/odeme/3d/basarisiz");
                }

                TempData["ThreeDVerificationUrl"] = result.Data.GetValueOrDefault("ThreeDVerificationUrl");
                TempData["OrderCode"] = result.Data.GetValueOrDefault("oid");
                return Ok($"/odeme/3d/yonlendirme");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyiniz.";
                return BadRequest("/odeme/3d/basarisiz");
            }
        }

        [HttpGet("3d/yonlendirme")]
        public async Task<IActionResult> ThreeDRedirect(CancellationToken cancellationToken)
        {
            try
            {
                var orderCode = TempData["OrderCode"] as string;
                var ThreeDVerificationUrl = TempData["ThreeDVerificationUrl"] as string;
                if (string.IsNullOrEmpty(orderCode))
                {
                    TempData["ErrorMessage"] = "Sipariş kodu bulunamadı. Lütfen tekrar deneyiniz.";
                    return Redirect("/odeme/3d/basarisiz");
                }

                // API'den cache'teki parametreleri al
                var result = await paymentApiService.GetPaymentParamsAsync(orderCode, cancellationToken);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = "3D yönlendirmesi yapılamadı. Lütfen tekrar deneyiniz.";
                    return Redirect("/odeme/3d/basarisiz");
                }

                ViewBag.ThreeDVerificationUrl = ThreeDVerificationUrl;

                return View(result.Data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "3D yönlendirme hatası");
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen tekrar deneyiniz.";
                return Redirect("/odeme/3d/basarisiz");
            }
        }

        [HttpPost("3d/cevap")]
        public async Task<IActionResult> ThreeDVerification([FromForm] Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            try
            {
                var mdStatus = parameters.GetValueOrDefault("mdStatus");
                var validStatuses = new[] { "1", "2", "3", "4" };

                if (!validStatuses.Contains(mdStatus))
                {
                    TempData["ErrorMessage"] = "3D doğrulama başarısız";
                    return Redirect("/odeme/3d/basarisiz");
                }

                // API'ye ödeme tamamlama isteği gönder (API cache'ten okur, sipariş/ödeme oluşturur, cache'i temizler)
                var result = await paymentApiService.CreatePaymentAsync(parameters, cancellationToken);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "Ödeme oluşturulamadı";
                    Guid? paymentId = null;
                    if (result.Data != Guid.Empty)
                    {
                        paymentId = result.Data;
                    }
                    return Redirect($"/odeme/basarisiz/{paymentId}");
                }

                return Redirect($"/odeme/basarili/{result.Data}");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "3D doğrulama başarısız";
                return Redirect("/odeme/3d/basarisiz");
            }
        }

        [HttpGet("basarili/{paymentId:guid}")]
        public async Task<IActionResult> PaymentSuccess(Guid paymentId, CancellationToken cancellationToken)
        {
            var payment = await paymentApiService.GetPaymentByIdAsync(paymentId, cancellationToken);

            if (payment.IsSuccess && payment.Data != null)
            {
                return View(payment.Data);
            }

            return Redirect("/ana-sayfa");
        }

        [HttpGet("basarisiz/{paymentId:guid?}")]
        [HttpPost("basarisiz")]
        public async Task<IActionResult> PaymentFailed([FromForm] Dictionary<string, string>? parameters, Guid? paymentId, CancellationToken cancellationToken)
        {
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string ?? "Bir hata oluştu";

            if (paymentId.HasValue)
            {
                var payment = await paymentApiService.GetPaymentByIdAsync(paymentId.Value, cancellationToken);

                if (payment.IsSuccess && payment.Data != null)
                {
                    return View(payment.Data);
                }
            }

            return View();
        }

        [HttpGet("3d/basarisiz")]
        public IActionResult ThreeDFailed()
        {
            ViewBag.ErrorMessage = TempData["ErrorMessage"] as string ?? "Bir hata oluştu";
            return View();
        }
    }
}