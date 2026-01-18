using Dapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Payment;
using Otomar.Application.Enums;
using Otomar.Persistance.Data;
using Otomar.Persistance.Helpers;
using Otomar.Persistance.Options;
using System.Globalization;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Otomar.Persistance.Services
{
    public class PaymentService(IAppDbContext context, HttpClient httpClient, IHttpContextAccessor accessor, IIdentityService identityService, PaymentOptions paymentOptions, ILogger<PaymentService> logger, IDistributedCache cache, IOrderService orderService, ICartService cartService) : IPaymentService
    {
        public async Task<ServiceResult<string>> CompletePaymentAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
            {
                return ServiceResult<string>.Error(
                    "Geçersiz İstek",
                    "Callback verileri boş olamaz",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                string mdStatus = parameters.GetValueOrDefault("mdStatus")!;
                var mdMessage = GetThreeDSecureStatusMessage(mdStatus);
                if (!IsThreeDSecureValid(mdStatus))
                {
                    logger.LogWarning("3D Secure MdStatus başarısız. MdStatus: {MdStatus}", mdStatus);
                    return ServiceResult<string>.Error(
                      "3D Secure Başarısız",
                      mdMessage,
                      HttpStatusCode.BadRequest);
                }
                string orderCode = parameters.GetValueOrDefault("oid");
                if (string.IsNullOrEmpty(orderCode))
                {
                    logger.LogError("oid (OrderCode) bulunamadı");
                    return ServiceResult<string>.Error(
                    "Geçersiz Sipariş Kodu",
                    "Oid verisinde sipariş kodu bulunamadı",
                    HttpStatusCode.BadRequest);
                }
                logger.LogInformation("Ödeme tamamlanıyor. Oid: {Oid}", orderCode);

                logger.LogDebug("Sanal POS API isteği gönderiliyor. URL: {Url}, MdStatus: {MdStatus}",
                   paymentOptions.ApiUrl, mdStatus);

                var isBankResponse = await IsBankPaymentRequest(parameters, cancellationToken);
                if (isBankResponse == null)
                {
                    logger.LogError($"Bankadan cevap alınamadı");
                    return ServiceResult<string>.Error("Banka Cevap", "Bankadan cevap alınamadı", HttpStatusCode.BadRequest);
                }
                using var transaction = context.Connection.BeginTransaction();

                try
                {
                    var order = await orderService.GetOrderByCodeAsync(orderCode);
                    if (order == null)
                    {
                        transaction.Rollback();
                        logger.LogError("Güncellenecek sipariş kaydı bulunamadı. OrderCode: {OrderCode}", orderCode);
                        return ServiceResult<string>.Error(
                            "Sipariş Bulunamadı",
                            $"{orderCode} sipariş kodu ile sipariş kaydı bulunamadı",
                            HttpStatusCode.NotFound);
                    }

                    // 1. ÖDEME OLUŞTUR
                    var orderId = order.Data.Id;
                    var isPaymentSuccessful = IsPaymentSuccess(isBankResponse);
                    var paymentId = NewId.NextGuid();
                    var userId = identityService.GetUserId() ?? null;
                    var totalAmount = Convert.ToDecimal(parameters.GetValueOrDefault("amount"));

                    var paymentInsertQuery = @"
        INSERT INTO IdtPayments (Id, UserId, OrderCode, TotalAmount, Status, CreatedAt, IpAddress, BankResponse, BankAuthCode, BankHostRefNum, BankProcReturnCode, BankTransId, BankErrMsg, BankErrorCode, BankSettleId, BankTrxDate, BankCardBrand, BankCardIssuer, BankAvsApprove, BankHostDate, BankAvsErrorCodeDetail, BankNumCode)
        VALUES (@Id, @UserId, @OrderCode, @TotalAmount, @Status, @CreatedAt, @IpAddress, @BankResponse, @BankAuthCode, @BankHostRefNum, @BankProcReturnCode, @BankTransId, @BankErrMsg, @BankErrorCode, @BankSettleId, @BankTrxDate, @BankCardBrand, @BankCardIssuer, @BankAvsApprove, @BankHostDate, @BankAvsErrorCodeDetail, @BankNumCode);";

                    var paymentParameters = new DynamicParameters();
                    paymentParameters.Add("Id", paymentId);
                    paymentParameters.Add("UserId", userId);
                    paymentParameters.Add("OrderCode", orderCode);
                    paymentParameters.Add("TotalAmount", totalAmount);
                    paymentParameters.Add("Status", isPaymentSuccessful ? PaymentStatus.Completed : PaymentStatus.Failed);
                    paymentParameters.Add("CreatedAt", DateTime.Now);
                    paymentParameters.Add("IpAddress", IpHelper.GetClientIp(accessor));
                    paymentParameters.Add("BankResponse", isBankResponse.Response);
                    paymentParameters.Add("BankAuthCode", isBankResponse.AuthCode);
                    paymentParameters.Add("BankHostRefNum", isBankResponse.HostRefNum);
                    paymentParameters.Add("BankProcReturnCode", isBankResponse.ProcReturnCode);
                    paymentParameters.Add("BankTransId", isBankResponse.TransId);
                    paymentParameters.Add("BankErrMsg", isBankResponse.ErrMsg);
                    paymentParameters.Add("BankErrorCode", isBankResponse.ErrorCode);
                    paymentParameters.Add("BankSettleId", isBankResponse.SettleId);
                    paymentParameters.Add("BankTrxDate", isBankResponse.TrxDate);
                    paymentParameters.Add("BankCardBrand", isBankResponse.CardBrand);
                    paymentParameters.Add("BankCardIssuer", isBankResponse.CardIssuer);
                    paymentParameters.Add("BankAvsApprove", isBankResponse.AvsApprove);
                    paymentParameters.Add("BankHostDate", isBankResponse.HostDate);
                    paymentParameters.Add("BankAvsErrorCodeDetail", isBankResponse.AvsErrorCodeDetail);
                    paymentParameters.Add("BankNumCode", isBankResponse.NumCode);

                    await context.Connection.ExecuteAsync(paymentInsertQuery, paymentParameters, transaction);
                    logger.LogInformation($"{paymentId} ID'li ödeme kaydı oluşturuldu");

                    // 2. ÖDEMEYE AİT SİPARİŞİ GÜNCELLE

                    var orderUpdateQuery = @"
                            UPDATE IdtOrders
                            SET Status = @Status, PaymentId = @PaymentId
                            WHERE Id = @OrderId";

                    var orderUpdateParameters = new DynamicParameters();
                    orderUpdateParameters.Add("OrderId", orderId);
                    orderUpdateParameters.Add("PaymentId", paymentId);
                    orderUpdateParameters.Add("Status", isPaymentSuccessful ? OrderStatus.Paid : OrderStatus.PaymentFailed);

                    var affectedRows = await context.Connection.ExecuteAsync(orderUpdateQuery, orderUpdateParameters, transaction);

                    if (affectedRows == 0)
                    {
                        transaction.Rollback();
                        logger.LogError("Sipariş güncellenemedi. OrderCode: {OrderCode}", orderCode);
                        return ServiceResult<string>.Error("Sipariş durumu güncellenemedi", HttpStatusCode.BadRequest);
                    }

                    logger.LogInformation($"{orderId} ID'li sipariş, {paymentId} ID'li ödeme ile ilişkilendirildi ve güncellendi");

                    transaction.Commit();

                    if (!isPaymentSuccessful)
                    {
                        logger.LogError($"{orderCode} sipariş kodlu ödeme başarısız. Bankadan dönen Mesaj:{isBankResponse.ErrMsg} Kod: {isBankResponse.ErrorCode}");
                        return ServiceResult<string>.Error("Ödeme Başarısız", $"{isBankResponse.ErrMsg}", HttpStatusCode.BadRequest);
                    }

                    logger.LogInformation($"{orderCode} sipariş kodlu ödeme işlemi başarıyla tamamlandı");

                    return ServiceResult<string>.SuccessAsCreated(orderCode, $"/api/payments/{paymentId}");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return ServiceResult<string>.Error("Ödeme Başarısız", $"{isBankResponse.ErrMsg}", HttpStatusCode.BadRequest);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "CreatePaymentAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(Guid paymentId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("paymentId", paymentId);
                var query = $@"SELECT TOP 1 Id, UserId, OrderCode, TotalAmount,BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode FROM IdtPayments WITH (NOLOCK) WHERE Id = @paymentId";

                var result = await context.Connection.QueryFirstOrDefaultAsync<PaymentDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"{paymentId} ID'li ödeme bulunamadı");
                    return ServiceResult<PaymentDto>.Error($"{paymentId} ID'li ödeme bulunamadı", HttpStatusCode.NotFound);
                }
                logger.LogInformation($"{paymentId} ID'li ödeme getirildi");
                return ServiceResult<PaymentDto>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetPaymentByIdAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<PaymentDto>> GetPaymentByOrderCodeAsync(string orderCode)
        {
            try
            {
                if (string.IsNullOrEmpty(orderCode))
                {
                    return ServiceResult<PaymentDto>.Error("Sipariş kodu boş geçilemez", HttpStatusCode.BadRequest);
                }

                var parameters = new DynamicParameters();
                parameters.Add("orderCode", orderCode);
                var query = $@"SELECT TOP 1 Id, UserId, OrderCode, TotalAmount,BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode FROM IdtPayments WITH (NOLOCK) WHERE OrderCode = @orderCode";

                var result = await context.Connection.QueryFirstOrDefaultAsync<PaymentDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"{orderCode} sipariş kodlu ödeme bulunamadı");
                    return ServiceResult<PaymentDto>.Error($"{orderCode} sipariş kodlu ödeme bulunamadı", HttpStatusCode.NotFound);
                }
                logger.LogInformation($"{orderCode} sipariş kodlu ödeme getirildi");
                return ServiceResult<PaymentDto>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetPaymentByOrderCodeAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<PaymentDto>>> GetPaymentsAsync()
        {
            try
            {
                var query = $@"
                 SELECT  Id, UserId, OrderCode, TotalAmount,BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode
                 FROM IdtPayments WITH (NOLOCK)";

                var result = await context.Connection.QueryAsync<PaymentDto>(query);
                if (!result.Any())
                {
                    logger.LogWarning($"Ödemeler bulunamadı");

                    return ServiceResult<IEnumerable<PaymentDto>>.Error($"Ödemeler bulunamadı", HttpStatusCode.NotFound);
                }
                logger.LogInformation($"Ödemeler getirildi");
                return ServiceResult<IEnumerable<PaymentDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetPaymentsAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<PaymentDto>>> GetPaymentsByUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<IEnumerable<PaymentDto>>.Error("Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);

                var query = $@"
                 SELECT Id, UserId, OrderCode, TotalAmount,BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode
                 FROM IdtPayments WITH (NOLOCK)
                 WHERE UserId = @userId";

                var result = await context.Connection.QueryAsync<PaymentDto>(query, parameters);
                if (!result.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının ödemeleri bulunamadı");

                    return ServiceResult<IEnumerable<PaymentDto>>.Error($"'{userId}' ID'li kullanıcının ödemeleri bulunamadı", HttpStatusCode.NotFound);
                }
                logger.LogInformation($"{userId} ID'li kullanıcının ödemeleri getirildi");

                return ServiceResult<IEnumerable<PaymentDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetPaymentsByUserAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<Dictionary<string, string>>> InitializePaymentAsync(InitializePaymentDto initializePaymentDto, CancellationToken cancellationToken)
        {
            using var transaction = context.Connection.BeginTransaction();

            try
            {
                var transactionType = paymentOptions.TransactionType;
                var orderCode = OrderCodeGeneratorHelper.Generate();
                var currency = paymentOptions.Currency; // TRY 949
                var okUrl = paymentOptions.OkUrl;
                var failUrl = paymentOptions.FailUrl;
                var storeType = paymentOptions.StoreType;
                var hashAlgorithm = paymentOptions.HashAlgorithm;
                var lang = paymentOptions.Lang;
                var refreshTime = paymentOptions.RefreshTime;
                var rnd = DateTime.Now.Ticks.ToString();
                var installment = string.Empty; // Taksit yoksa empty gönderilmeli
                var amountStr = initializePaymentDto.TotalAmount.ToString("0.##", CultureInfo.InvariantCulture);
                initializePaymentDto.Order.Code = orderCode;
                var parameters = new Dictionary<string, string>
                {
                    { "clientid", paymentOptions.ClientId },
                    { "storetype",  storeType },
                    { "TranType",  transactionType },
                    { "currency",  currency },
                    { "amount",  amountStr },
                    { "oid",  orderCode },
                    { "okUrl",  okUrl },
                    { "failUrl",  failUrl },
                    { "Instalment",  installment },
                    { "lang",  lang },
                    { "rnd",  rnd },
                    { "hashAlgorithm",  hashAlgorithm },
                    { "refreshTime",  refreshTime },
                    { "pan",  initializePaymentDto.CreditCardNumber},
                    { "cv2",  initializePaymentDto.CreditCardCvv},
                    { "Ecom_Payment_Card_ExpDate_Year",  initializePaymentDto.CreditCardExpDateYear },
                    { "Ecom_Payment_Card_ExpDate_Month",  initializePaymentDto.CreditCardExpDateMonth },
                    { "Email",  initializePaymentDto.Order.Email},
                };

                parameters["hash"] = GenerateHash(parameters);

                if (parameters.IsNullOrEmpty())
                {
                    return ServiceResult<Dictionary<string, string>>.Error("3D Doğrulama", "3D doğrulama için parametreler oluşturulamadı", HttpStatusCode.BadRequest);
                }

                var createOrderResult = await orderService.CreateOrderAsync(initializePaymentDto.Order, transaction);
                if (!createOrderResult.IsSuccess)
                {
                    transaction.Rollback();
                    return ServiceResult<Dictionary<string, string>>.Error("Sipariş Oluşturulamadı", "Sipariş oluşturmada hata meydana geldi", HttpStatusCode.BadRequest);
                }
                transaction.Commit();
                return ServiceResult<Dictionary<string, string>>.SuccessAsOk(parameters);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "InitializePaymentAsync işleminde hata");
                throw;
            }
        }

        private string GetCartKeyForPayment(string? userId, string? cartSessionId)
        {
            // 1. Öncelik: Giriş yapmış kullanıcı
            if (!string.IsNullOrEmpty(userId))
            {
                return $"cart:user:{userId}";
            }

            // 2. Öncelik: Cache'den gelen session ID
            if (!string.IsNullOrEmpty(cartSessionId))
            {
                return $"cart:session:{cartSessionId}";
            }

            // 3. Fallback: Mevcut cookie'den oku (nadiren olur)
            var currentSessionId = accessor.HttpContext?.Request.Cookies["CartSessionId"];
            if (!string.IsNullOrEmpty(currentSessionId))
            {
                return $"cart:session:{currentSessionId}";
            }

            // 4. Son çare: Yeni session (bu durumda sepet bulunamaz ama hata vermez)
            logger.LogWarning("Cart key belirlenemedi, fallback kullanılıyor");
            return $"cart:session:{Guid.NewGuid()}";
        }

        private string GenerateHash(Dictionary<string, string> formData)
        {
            var sortedParams = formData
                .Where(p => !string.Equals(p.Key, "encoding", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(p.Key, "hash", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key.ToLower(new CultureInfo("en-US", false)))
                .ToList();

            var hashVal = new StringBuilder();
            var paramsKeys = new StringBuilder();

            foreach (var pair in sortedParams)
            {
                var escapedValue = pair.Value?.Replace("\\", "\\\\").Replace("|", "\\|") ?? string.Empty;
                var lowerKey = pair.Key.ToLower(new CultureInfo("en-US", false));

                hashVal.Append(escapedValue).Append("|");
                paramsKeys.Append(lowerKey).Append("|");
            }

            hashVal.Append(paymentOptions.StoreKey);

            using var sha = System.Security.Cryptography.SHA512.Create();
            var hashBytes = Encoding.UTF8.GetBytes(hashVal.ToString());
            var computedHash = sha.ComputeHash(hashBytes);

            return Convert.ToBase64String(computedHash);
        }

        private bool ValidateHash(Dictionary<string, string> parameters)
        {
            var receivedHash = parameters.GetValueOrDefault("hash");

            if (string.IsNullOrEmpty(receivedHash))
                return false;

            var calculatedHash = GenerateHash(parameters);

            return calculatedHash.Equals(receivedHash, StringComparison.Ordinal);
        }

        private async Task<IsBankResponseDto> IsBankPaymentRequest(Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            var requestAsXmlString = ParseIsBankRequest(parameters);
            using var responseFromBank = await httpClient.PostAsync(paymentOptions.ApiUrl, new StringContent(requestAsXmlString, Encoding.UTF8, "text/xml"), cancellationToken);
            string responseAsString = await responseFromBank.Content.ReadAsStringAsync(cancellationToken);
            var responseAsDto = ParseIsBankResponse(responseAsString);
            return responseAsDto;
        }

        private string ParseIsBankRequest(Dictionary<string, string> parameters)
        {
            var xml = new XElement("CC5Request",
                new XElement("Name", paymentOptions.Username),
                new XElement("Password", paymentOptions.Password),
                new XElement("ClientId", paymentOptions.ClientId),
                new XElement("Type", parameters["TranType"]),
                new XElement("Email", parameters["Email"]),
                new XElement("OrderId", parameters["oid"]),
                new XElement("Total", parameters["amount"]),
                new XElement("Currency", parameters["currency"]),
                new XElement("Instalment", parameters["Instalment"]),
                new XElement("Number", parameters["md"]),
                new XElement("PayerAuthenticationCode", parameters["cavv"]),
                new XElement("PayerSecurityLevel", parameters["eci"]),
                new XElement("PayerTxnId", parameters["xid"])
            );

            return xml.ToString();
        }

        private IsBankResponseDto ParseIsBankResponse(string responseAsString)
        {
            var serializer = new XmlSerializer(typeof(IsBankResponseDto));
            using var reader = new StringReader(responseAsString);
            return (IsBankResponseDto)serializer.Deserialize(reader);
        }

        private bool IsPaymentSuccess(IsBankResponseDto dto)
        {
            return dto.Response.Equals("Approved", StringComparison.OrdinalIgnoreCase) && dto.ProcReturnCode == "00";
        }

        private bool IsThreeDSecureValid(string mdStatus)
        {
            var validStatuses = new[] { "1", "2", "3", "4" };
            if (!validStatuses.Contains(mdStatus))
            {
                return true;
            }
            return false;
        }

        private string GetThreeDSecureStatusMessage(string mdStatus)
        {
            return mdStatus switch
            {
                "1" => "3D Secure doğrulama başarılı (Full Secure)",
                "2" or "3" or "4" => "3D Secure doğrulama kısmen başarılı (Half Secure)",
                "5" or "6" or "7" or "8" => "Kart 3D Secure programına kayıtlı değil veya işlem reddedildi",
                "0" => "3D Secure doğrulama başarsız",
                _ => $"Bilinmeyen durum: {mdStatus}"
            };
        }
    }
}