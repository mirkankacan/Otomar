using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Models.Payment;
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

        [HttpPost("baslat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializePayment([FromBody] InitializePaymentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await paymentApiService.InitializePaymentAsync(dto, cancellationToken);

                if (!result.IsSuccess)
                {
                    return result.ToActionResult();
                }

                HttpContext.Session.SetString("ThreeDSecureParameters", JsonSerializer.Serialize(result.Data));

                return Ok(new { redirectUrl = Url.Action("ThreeDSecure", "Payment") });
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
                var parametersJson = HttpContext.Session.GetString("PaymentParameters");
                if (string.IsNullOrEmpty(parametersJson))
                {
                    logger.LogWarning("3D doğrulama verisi bulunamadı");
                    return RedirectToAction(nameof(Failed));
                }
                var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson);
                HttpContext.Session.Remove("PaymentParameters");
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
            if (order.IsSuccess)
            {
                if (order.Data.Payment.BankProcReturnCode != "00")
                {
                    return Redirect($"/odeme/basarisiz/{orderCode}");
                }
                return View(order.Data);
            }
            return View();
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
    }
}