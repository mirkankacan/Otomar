using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.Payment;
using Otomar.WebApp.Services.Refit;
using Refit;
using System.Text.Json;

namespace Otomar.WebApp.Controllers
{
    [Route("odeme")]
    public class PaymentController(IPaymentApi paymentApi, IOrderApi orderApi, ICartApi cartApi, ILogger<PaymentController> logger) : Controller
    {
        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
        {
            try
            {
                var cart = await cartApi.GetCartAsync(cancellationToken);
                if (cart?.Items == null || cart.Items.Count == 0)
                {
                    return RedirectToAction(nameof(CartController.Index), "Cart");
                }
            }
            catch
            {
                return RedirectToAction(nameof(CartController.Index), "Cart");
            }

            return View();
        }

        [HttpGet("sanal-pos")]
        public IActionResult VirtualPos()
        {
            return View();
        }

        [HttpPost("sanal-pos-baslat")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitializeVirtualPosPayment([FromBody] InitializeVirtualPosPaymentDto dto, CancellationToken cancellationToken)
        {
            try
            {
                return await PrepareToPayment(paymentApi.InitializeVirtualPosPaymentAsync(dto, cancellationToken));
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
                return await PrepareToPayment(paymentApi.InitializePurchasePaymentAsync(dto, cancellationToken));
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
            try
            {
                var order = await orderApi.GetOrderByCodeAsync(orderCode, cancellationToken);
                if (order != null)
                {
                    if (order.Payment != null && order.Payment.BankProcReturnCode != "00")
                    {
                        return Redirect($"/odeme/basarisiz/{orderCode}");
                    }
                    return View(order);
                }
            }
            catch (ApiException)
            {
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("basarisiz/{orderCode?}")]
        public async Task<IActionResult> Failed(string? orderCode, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(orderCode))
            {
                try
                {
                    var order = await orderApi.GetOrderByCodeAsync(orderCode, cancellationToken);
                    if (order != null && order.Payment != null)
                    {
                        if (order.Payment.BankProcReturnCode == "00")
                        {
                            return Redirect($"/odeme/basarili/{orderCode}");
                        }
                        return View(order);
                    }
                }
                catch (ApiException)
                {
                }
            }
            return View();
        }

        private void ClearPaymentSession()
        {
            HttpContext.Session.Remove("ThreeDSecureParameters");
            HttpContext.Session.Remove("ThreeDVerificationUrl");
        }

        private async Task<IActionResult> PrepareToPayment(Task<InitializePaymentResponseDto> task)
        {
            try
            {
                var response = await task;
                if (response == null)
                {
                    return BadRequest(new { title = "Bir hata oluştu", detail = "Yanıt alınamadı" });
                }
                HttpContext.Session.SetString("ThreeDSecureParameters", JsonSerializer.Serialize(response.Parameters));
                HttpContext.Session.SetString("ThreeDVerificationUrl", response.ThreeDVerificationUrl);

                return Ok(new { redirectUrl = Url.Action(nameof(ThreeDRedirect), "Payment") });
            }
            catch (ApiException ex)
            {
                var statusCode = (System.Net.HttpStatusCode)ex.StatusCode;
                return new ObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = ex.ReasonPhrase ?? "Bir hata oluştu",
                    status = (int)statusCode,
                    detail = ex.Content ?? ex.Message
                })
                {
                    StatusCode = (int)statusCode
                };
            }
        }
    }
}