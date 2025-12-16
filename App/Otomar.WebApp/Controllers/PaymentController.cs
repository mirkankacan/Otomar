using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Models.Payment;
using Otomar.WebApp.Options;
using Otomar.WebApp.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Otomar.WebApp.Controllers
{
    [Route("odeme")]
    public class PaymentController(IPaymentApiService paymentApiService, PaymentOptions paymentOptions) : Controller
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

        [HttpPost("3d/dogrulama-yap")]
        [ValidateAntiForgeryToken]
        public IActionResult InitializeThreeDVerification([FromBody] InitializePaymentDto dto)
        {
            var transactionType = paymentOptions.TransactionType;
            var orderCode = GenerateOrderCode();
            var currency = paymentOptions.Currency; //TRY 949
            var okUrl = paymentOptions.OkUrl;
            var failUrl = paymentOptions.FailUrl;
            var storeType = paymentOptions.StoreType;
            var hashAlgorithm = paymentOptions.HashAlgorithm;
            var lang = paymentOptions.Lang;
            var refreshTime = paymentOptions.RefreshTime;
            var rnd = DateTime.Now.Ticks.ToString();
            var installment = string.Empty; //Taksit yoksa empty gönderilmeli
            var parameters = new Dictionary<string, string>
                {
                    { "clientid", paymentOptions.ClientId },
                    { "storetype",  storeType },
                    { "TranType",  transactionType },
                    { "currency",  currency },
                    { "amount",  dto.TotalAmount.ToString() },
                    { "oid",  orderCode },
                    { "okUrl",  okUrl },
                    { "failUrl",  failUrl },
                    { "Instalment",  installment },
                    { "lang",  lang },
                    { "rnd",  rnd },
                    { "hashAlgorithm",  hashAlgorithm },
                    { "refreshTime",  refreshTime },
                    { "pan",  dto.CreditCardNumber },
                    { "cv2",  dto.CreditCardCvv },
                    { "Ecom_Payment_Card_ExpDate_Year",  dto.CreditCardExpDateYear },
                    { "Ecom_Payment_Card_ExpDate_Month",  dto.CreditCardExpDateMonth }
                };

            parameters["hash"] = GenerateHash(parameters);

            TempData["PaymentParameters"] = JsonSerializer.Serialize(parameters);

            return Ok("/odeme/3d/yonlendirme");
        }

        [HttpGet("3d/yonlendirme")]
        public IActionResult ThreeDRouting()
        {
            var parametersJson = TempData["PaymentParameters"] as string;

            if (string.IsNullOrEmpty(parametersJson))
            {
                TempData["3DErrorMessage"] = "3D doğrulama sayfasına yönlendirme yapılamadı.";
                return Redirect("/odeme/3d/basarisiz");
            }
            var parameters = JsonSerializer.Deserialize<Dictionary<string, string>>(parametersJson);
            ViewBag.ThreeDVerificationUrl = paymentOptions.ThreeDVerificationUrl;
            return View(parameters);
        }

        [HttpPost("3d/cevap")]
        public async Task<IActionResult> ThreeDVerificationAnswer([FromForm] Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            var mdStatus = parameters.GetValueOrDefault("mdStatus");
            var errorMessage = parameters.GetValueOrDefault("ErrMsg", "");
            var mdErrorMessage = parameters.GetValueOrDefault("mdErrorMsg", "");

            var validStatuses = new[] { "1", "2", "3", "4", "5", "7", "8" };

            if (/*!ValidateHash(parameters) ||*/ !validStatuses.Contains(mdStatus))
            {
                TempData["3DErrorMessage"] = "3D doğrulama başarısız. Tekrar deneyiniz!";
                return Redirect("/odeme/3d/basarisiz");
            }
            decimal totalAmount = Convert.ToDecimal(parameters.GetValueOrDefault("amount"));
            var createPayment = await paymentApiService.CreatePaymentAsync(parameters, cancellationToken);
            if (!createPayment.IsSuccess)
            {
                return Redirect($"/odeme/basarisiz/{createPayment.Data}");
            }
            return Redirect($"/odeme/basarili/{createPayment.Data}");
        }

        [HttpGet("/basarili/{paymentId:guid}")]
        public async Task<IActionResult> PaymentSuccess(Guid paymentId, CancellationToken cancellationToken)
        {
            var payment = await paymentApiService.GetPaymentByIdAsync(paymentId, cancellationToken);
            if (payment.IsSuccess && payment.Data != null)
            {
                return View(payment.Data);
            }
            return Redirect("/ana-sayfa");
        }

        [HttpGet("/basarisiz/{paymentId:guid?}")]
        public async Task<IActionResult> PaymentFailed(Guid? paymentId, CancellationToken cancellationToken)
        {
            if (paymentId.HasValue)
            {
                var payment = await paymentApiService.GetPaymentByIdAsync(paymentId.Value, cancellationToken);
                if (payment.IsSuccess && payment.Data != null)
                {
                    return View(payment.Data);
                }
                return Redirect("/ana-sayfa");
            }
            return View();
        }

        [HttpGet("3d/basarisiz")]
        public IActionResult ThreeDFailed()
        {
            var errorMessage = TempData["3DErrorMessage"] as string;
            if (string.IsNullOrEmpty(errorMessage))
                return Redirect("/ana-sayfa");

            return View(errorMessage);
        }

        private string GenerateOrderCode()
        {
            var random = new Random();
            var randomNumber = random.Next(10000, 99999);
            return "OTOMAR" + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + randomNumber;
        }

        private string GenerateHash(Dictionary<string, string> parameters)
        {
            // "encoding" ve "hash" parametreleri hash hesaplamasına dahil edilmez
            var filteredParams = parameters
                .Where(p => p.Key != "encoding" && p.Key != "hash")
                .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase) // Alfabetik sıralama (A-Z)
                .ToList();

            // Plain text oluşturma
            var plainTextBuilder = new StringBuilder();

            foreach (var param in filteredParams)
            {
                // Değer içindeki özel karakterleri escape et
                var escapedValue = EscapeValue(param.Value ?? string.Empty);
                plainTextBuilder.Append(escapedValue);
                plainTextBuilder.Append("|");
            }

            // Sonuna storeKey ekle
            plainTextBuilder.Append(paymentOptions.StoreKey);

            var plainText = plainTextBuilder.ToString();

            // SHA-512 ile hash'le ve Base64 encode et
            using (var sha512 = SHA512.Create())
            {
                var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(plainText));
                return Convert.ToBase64String(hashBytes);
            }
        }

        private string EscapeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Önce "\" karakterini "\\" ile değiştir
            value = value.Replace("\\", "\\\\");

            // Sonra "|" karakterini "\|" ile değiştir
            value = value.Replace("|", "\\|");

            return value;
        }

        public bool ValidateHash(Dictionary<string, string> parameters)
        {
            var receivedHash = parameters.GetValueOrDefault("hash");

            if (string.IsNullOrEmpty(receivedHash))
                return false;

            var calculatedHash = GenerateHash(parameters);

            return calculatedHash.Equals(receivedHash, StringComparison.Ordinal);
        }
    }
}