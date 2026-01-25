using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Models.Payment;
using Otomar.WebApp.Responses;
using Otomar.WebApp.Services.Interfaces;
using System.Text.Json;

namespace Otomar.WebApp.Controllers
{
    [Route("odeme")]
    public class PaymentController(IPaymentApiService paymentApiService, IOrderApiService orderApiService, ILogger<PaymentController> logger) : Controller
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

        [HttpPost("sanal-pos-baslat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializePurchasePayment([FromBody] InitializeVirtualPosPaymentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await paymentApiService.InitializeVirtualPosPaymentAsync(dto, cancellationToken);

                return PrepareToPayment(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ödeme başlatma hatası");
                return RedirectToAction(nameof(Failed));
            }
        }

        [HttpPost("satin-alim-baslat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializePurchasePayment([FromBody] InitializePurchasePaymentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await paymentApiService.InitializePurchasePaymentAsync(dto, cancellationToken);

                return PrepareToPayment(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ödeme başlatma hatası");
                return RedirectToAction(nameof(Failed));
            }
        }

        [HttpGet("3d-yonlendirme")]
        public async Task<IActionResult> ThreeDRedirect()
        {
            try
            {
                var parametersJson = HttpContext.Session.GetString("ThreeDSecureParameters");
                var threeDVerificationUrl = HttpContext.Session.GetString("ThreeDVerificationUrl");
                if (string.IsNullOrEmpty(parametersJson) || string.IsNullOrEmpty(threeDVerificationUrl))
                {
                    logger.LogWarning("3D doğrulama verisi bulunamadı");
                    return RedirectToAction(nameof(Failed));
                }
                var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson);
                ViewBag.ThreeDVerificationUrl = threeDVerificationUrl;
                ClearPaymentSession();
                return View(parameters);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "3D doğrulama yönlendirme hatası");
                return RedirectToAction(nameof(Failed));
            }
        }

        [HttpGet("basarili/{orderCode}")]
        public async Task<IActionResult> Success(string orderCode, CancellationToken cancellationToken)
        {
            var order = await orderApiService.GetOrderByCodeAsync(orderCode, cancellationToken);
            if (order.IsSuccess && order.Data != null)
            {
                if (order.Data.Payment != null && order.Data.Payment.BankProcReturnCode != "00")
                {
                    return Redirect($"/odeme/basarisiz/{orderCode}");
                }
                return View(order.Data);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("basarisiz/{orderCode?}")]
        public async Task<IActionResult> Failed(string? orderCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(orderCode))
            {
                var payment = await paymentApiService.GetPaymentByOrderCodeAsync(orderCode, cancellationToken);
                if (payment.IsSuccess)
                {
                    if (payment.Data.BankProcReturnCode == "00")
                    {
                        return Redirect($"/odeme/basarili/{orderCode}");
                    }
                    return View(payment.Data);
                }
            }
            return View();
        }

        private void ClearPaymentSession()
        {
            HttpContext.Session.Remove("ThreeDSecureParameters");
            HttpContext.Session.Remove("ThreeDVerificationUrl");
        }

        private IActionResult PrepareToPayment(ApiResponse<InitializePaymentResponseDto> result)
        {
            if (!result.IsSuccess || result.Data == null)
            {
                return result.ToActionResult();
            }
            HttpContext.Session.SetString("ThreeDSecureParameters", JsonSerializer.Serialize(result.Data.Parameters));
            HttpContext.Session.SetString("ThreeDVerificationUrl", result.Data.ThreeDVerificationUrl);

            return Ok(new { redirectUrl = Url.Action(nameof(ThreeDRedirect), "Payment") });
        }
    }
}