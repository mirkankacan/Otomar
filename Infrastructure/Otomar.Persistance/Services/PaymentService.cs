using Dapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Order;
using Otomar.Application.Dtos.Payment;
using Otomar.Domain.Enums;
using Otomar.Persistance.Data;
using Otomar.Persistance.Helpers;
using Otomar.Persistance.Options;
using System.Globalization;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class PaymentService(IAppDbContext context, HttpClient httpClient, IHttpContextAccessor accessor, IIdentityService identityService, PaymentOptions paymentOptions, ILogger<PaymentService> logger, IDistributedCache cache, IOrderService orderService, ICartService cartService) : IPaymentService
    {
        public async Task<ServiceResult<InitializePaymentResponseDto>> InitializeVirtualPosPaymentAsync(InitializeVirtualPosPaymentDto dto, CancellationToken cancellationToken)
        {
            using var transaction = context.Connection.BeginTransaction();

            try
            {
                var orderCode = OrderCodeGeneratorHelper.Generate();
                var amountStr = dto.Amount.ToString("0.##", CultureInfo.InvariantCulture);

                var parameters = BuildPaymentParameters(
                    orderCode,
                    amountStr,
                    dto.CreditCardNumber,
                    dto.CreditCardCvv,
                    dto.CreditCardExpDateYear,
                    dto.CreditCardExpDateMonth,
                    dto.Email
                );

                if (parameters.GetValueOrDefault("hash") == null || parameters.Count < 18)
                {
                    return ServiceResult<InitializePaymentResponseDto>.Error("3D Doğrulama", "3D doğrulama için parametreler oluşturulamadı", HttpStatusCode.BadRequest);
                }
                var order = new CreateVirtualPosOrderDto()
                {
                    Email = dto.Email,
                    Code = orderCode,
                    IdentityNumber = dto.TaxNumber.Length == 11 ? dto.TaxNumber : null,
                    Amount = dto.Amount,
                    OrderType = OrderType.VirtualPOS,
                    Corporate = new CorporateDto()
                    {
                        CompanyName = dto.ClientName,
                        TaxNumber = dto.TaxNumber.Length == 10 ? dto.TaxNumber : null,
                        TaxOffice = dto.TaxOffice
                    },
                    BillingAddress = new AddressDto()
                    {
                        Name = dto.ClientName
                    }
                };
                var createOrderResult = await orderService.CreateVirtualPosOrderAsync(order, transaction, cancellationToken);
                if (!createOrderResult.IsSuccess)
                {
                    transaction.Rollback();
                    return ServiceResult<InitializePaymentResponseDto>.Error("Sipariş Oluşturulamadı", "Sipariş oluşturmada hata meydana geldi", HttpStatusCode.BadRequest);
                }
                transaction.Commit();

                return ServiceResult<InitializePaymentResponseDto>.SuccessAsOk(new InitializePaymentResponseDto()
                {
                    Parameters = parameters,
                    ThreeDVerificationUrl = paymentOptions.ThreeDVerificationUrl
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();

                logger.LogError(ex, "InitializeVirtualPosPaymentAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<InitializePaymentResponseDto>> InitializePurchasePaymentAsync(InitializePurchasePaymentDto dto, CancellationToken cancellationToken)
        {
            using var transaction = context.Connection.BeginTransaction();

            try
            {
                var orderCode = OrderCodeGeneratorHelper.Generate();

                var cart = await cartService.GetCartAsync(cancellationToken);
                if (!cart.IsSuccess || cart.Data == null)
                {
                    return ServiceResult<InitializePaymentResponseDto>.Error("Sepet Bulunamadı", "Ödeme işlemi başlatılamadı sepet bulunamadı.", HttpStatusCode.BadRequest);
                }

                var amountStr = cart.Data.Total.ToString("0.##", CultureInfo.InvariantCulture);
                dto.Order.Code = orderCode;
                dto.Order.OrderType = OrderType.Purchase;
                var parameters = BuildPaymentParameters(
                    orderCode,
                    amountStr,
                    dto.CreditCardNumber,
                    dto.CreditCardCvv,
                    dto.CreditCardExpDateYear,
                    dto.CreditCardExpDateMonth,
                    dto.Order.Email
                );

                if (parameters.GetValueOrDefault("hash") == null || parameters.Count < 18)
                {
                    return ServiceResult<InitializePaymentResponseDto>.Error("3D Doğrulama", "3D doğrulama için parametreler oluşturulamadı", HttpStatusCode.BadRequest);
                }

                var createOrderResult = await orderService.CreatePurchaseOrderAsync(dto.Order, transaction, cancellationToken);
                if (!createOrderResult.IsSuccess)
                {
                    transaction.Rollback();
                    return ServiceResult<InitializePaymentResponseDto>.Error("Sipariş Oluşturulamadı", "Sipariş oluşturmada hata meydana geldi", HttpStatusCode.BadRequest);
                }

                transaction.Commit();
                return ServiceResult<InitializePaymentResponseDto>.SuccessAsOk(new InitializePaymentResponseDto()
                {
                    Parameters = parameters,
                    ThreeDVerificationUrl = paymentOptions.ThreeDVerificationUrl
                });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "InitializePurchasePaymentAsync işleminde hata");
                throw;
            }
        }

        private Dictionary<string, string> BuildPaymentParameters(
            string orderCode,
            string amount,
            string creditCardNumber,
            string creditCardCvv,
            string expDateYear,
            string expDateMonth,
            string email)
        {
            var rnd = DateTime.Now.Ticks.ToString();
            var installment = string.Empty;

            var parameters = new Dictionary<string, string>
    {
        { "clientid", paymentOptions.ClientId },
        { "storetype", paymentOptions.StoreType },
        { "TranType", paymentOptions.TransactionType },
        { "currency", paymentOptions.Currency },
        { "amount", amount },
        { "oid", orderCode },
        { "okUrl", paymentOptions.OkUrl },
        { "failUrl", paymentOptions.FailUrl },
        { "Instalment", installment },
        { "lang", paymentOptions.Lang },
        { "rnd", rnd },
        { "hashAlgorithm", paymentOptions.HashAlgorithm },
        { "refreshTime", paymentOptions.RefreshTime },
        { "pan", creditCardNumber },
        { "cv2", creditCardCvv },
        { "Ecom_Payment_Card_ExpDate_Year", expDateYear },
        { "Ecom_Payment_Card_ExpDate_Month", expDateMonth },
        { "Email", email }
    };

            parameters["hash"] = IsBankHelper.GenerateHash(parameters, paymentOptions);

            return parameters;
        }

        public async Task<ServiceResult<string>> CompletePaymentAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            if (parameters == null)
            {
                logger.LogWarning("3D Secure'den dönen parametreler boş");

                return ServiceResult<string>.Error(
                    "Geçersiz İstek",
                    "Callback verileri boş olamaz",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                string mdStatus = parameters.GetValueOrDefault("mdStatus")!;
                var mdMessage = IsBankHelper.GetThreeDSecureStatusMessage(mdStatus);
                if (!IsBankHelper.IsThreeDSecureValid(mdStatus))
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

                var isBankResponse = await IsBankHelper.IsBankPaymentRequest(httpClient, parameters, paymentOptions, cancellationToken);
                if (isBankResponse == null)
                {
                    logger.LogError($"Bankadan cevap alınamadı");
                    return ServiceResult<string>.Error("Banka Cevap", "Bankadan cevap alınamadı", HttpStatusCode.BadRequest);
                }
                using var transaction = context.Connection.BeginTransaction();

                try
                {
                    var order = await orderService.GetOrderByCodeAsync(orderCode, transaction);
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
                    var isPaymentSuccessful = IsBankHelper.IsPaymentSuccess(isBankResponse);
                    var paymentId = NewId.NextGuid();
                    var userId = identityService.GetUserId() ?? null;
                    var totalAmount = Convert.ToDecimal(parameters.GetValueOrDefault("amount"));
                    string maskedCreditCard = parameters.GetValueOrDefault("maskedCreditCard");

                    var paymentInsertQuery = @"
        INSERT INTO IdtPayments (Id, UserId, OrderCode, TotalAmount, Status, CreatedAt, IpAddress, BankResponse, BankAuthCode, BankHostRefNum, BankProcReturnCode, BankTransId, BankErrMsg, BankErrorCode, BankSettleId, BankTrxDate, BankCardBrand, BankCardIssuer, BankAvsApprove, BankHostDate, BankAvsErrorCodeDetail, BankNumCode,MaskedCreditCard)
        VALUES (@Id, @UserId, @OrderCode, @TotalAmount, @Status, @CreatedAt, @IpAddress, @BankResponse, @BankAuthCode, @BankHostRefNum, @BankProcReturnCode, @BankTransId, @BankErrMsg, @BankErrorCode, @BankSettleId, @BankTrxDate, @BankCardBrand, @BankCardIssuer, @BankAvsApprove, @BankHostDate, @BankAvsErrorCodeDetail, @BankNumCode, @MaskedCreditCard);";

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
                    paymentParameters.Add("MaskedCreditCard", maskedCreditCard);

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
                        return ServiceResult<string>.Error("Sipariş Güncellenemedi", "Sipariş durumu güncellenemedi", HttpStatusCode.BadRequest);
                    }

                    logger.LogInformation($"{orderId} ID'li sipariş, {paymentId} ID'li ödeme ile ilişkilendirildi ve güncellendi");

                    transaction.Commit();

                    if (!isPaymentSuccessful)
                    {
                        logger.LogError($"{orderCode} sipariş kodlu ödeme başarısız. Bankadan dönen Mesaj:{isBankResponse.ErrMsg} Kod: {isBankResponse.ErrorCode}");
                        return ServiceResult<string>.Error("Ödeme Başarısız", $"{isBankResponse.ErrMsg}", HttpStatusCode.BadRequest);
                    }
                    else
                    {
                        await cartService.ClearCartAsync(cancellationToken);
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
                var query = $@"SELECT TOP 1 Id, UserId, OrderCode, TotalAmount,SubTotalAmount, ShippingAmount, BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode,MaskedCreditCard FROM IdtPayments WITH (NOLOCK) WHERE Id = @paymentId";

                var result = await context.Connection.QueryFirstOrDefaultAsync<PaymentDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"{paymentId} ID'li ödeme bulunamadı");
                    return ServiceResult<PaymentDto>.Error("Ödeme Bulunamadı", $"{paymentId} ID'li ödeme bulunamadı", HttpStatusCode.NotFound);
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
                    return ServiceResult<PaymentDto>.Error("Geçersiz Sipariş Kodu", "Sipariş kodu boş geçilemez", HttpStatusCode.BadRequest);
                }

                var parameters = new DynamicParameters();
                parameters.Add("orderCode", orderCode);
                var query = $@"SELECT TOP 1 Id, UserId, OrderCode, TotalAmount,SubTotalAmount, ShippingAmount, BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode,MaskedCreditCard FROM IdtPayments WITH (NOLOCK) WHERE OrderCode = @orderCode";

                var result = await context.Connection.QueryFirstOrDefaultAsync<PaymentDto>(query, parameters);
                if (result == null)
                {
                    logger.LogWarning($"{orderCode} sipariş kodlu ödeme bulunamadı");
                    return ServiceResult<PaymentDto>.Error("Ödeme Bulunamadı", $"{orderCode} sipariş kodlu ödeme bulunamadı", HttpStatusCode.NotFound);
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
                 SELECT  Id, UserId, OrderCode, TotalAmount,SubTotalAmount, ShippingAmount, BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode,MaskedCreditCard
                 FROM IdtPayments WITH (NOLOCK)";

                var result = await context.Connection.QueryAsync<PaymentDto>(query);
                if (!result.Any())
                {
                    logger.LogWarning($"Ödemeler bulunamadı");

                    return ServiceResult<IEnumerable<PaymentDto>>.Error("Ödemeler Bulunamadı", "Sistemde ödeme bulunamadı", HttpStatusCode.NotFound);
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
                    return ServiceResult<IEnumerable<PaymentDto>>.Error("Geçersiz Kullanıcı ID'si", "Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);

                var query = $@"
                 SELECT Id, UserId, OrderCode, TotalAmount,SubTotalAmount, ShippingAmount, BankCardBrand, BankCardIssuer, Status, CreatedAt,BankProcReturnCode,MaskedCreditCard
                 FROM IdtPayments WITH (NOLOCK)
                 WHERE UserId = @userId";

                var result = await context.Connection.QueryAsync<PaymentDto>(query, parameters);
                if (!result.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının ödemeleri bulunamadı");

                    return ServiceResult<IEnumerable<PaymentDto>>.Error("Ödemeler Bulunamadı", $"'{userId}' ID'li kullanıcının ödemeleri bulunamadı", HttpStatusCode.NotFound);
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
    }
}