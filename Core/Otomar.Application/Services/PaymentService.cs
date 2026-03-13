using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Interfaces;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Dtos.Notification;
using Otomar.Shared.Dtos.Payment;
using Otomar.Shared.Enums;
using Otomar.Application.Helpers;
using Otomar.Application.Options;
using System.Globalization;
using System.Net;

namespace Otomar.Application.Services
{
    public class PaymentService(IHttpContextAccessor accessor, IIdentityService identityService, IClientInfoProvider clientInfoProvider, ICartSessionService cartSessionService, IIsBankPaymentService isBankPaymentService, PaymentOptions paymentOptions, RedisOptions redisOptions, ILogger<PaymentService> logger, IOrderService orderService, ICartService cartService, IProductService productService, IEmailService emailService, INotificationService notificationService, IUnitOfWork paymentUnitOfWork, IPaymentRepository paymentRepository) : IPaymentService
    {
        public async Task<ServiceResult<InitializePaymentResponseDto>> InitializeVirtualPosPaymentAsync(InitializeVirtualPosPaymentDto dto, CancellationToken cancellationToken)
        {
            paymentUnitOfWork.BeginTransaction();

            try
            {
                var orderCode = OrderCodeGeneratorHelper.Generate();
                var amountStr = IsBankHelper.IsBankAmountConvert(dto.Amount);

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
                    Amount = dto.Amount,
                    IdentityNumber = dto.IdentityNumber,
                    Corporate = new CorporateDto()
                    {
                        CompanyName = dto.ClientName,
                        TaxNumber = dto.TaxNumber,
                        TaxOffice = dto.TaxOffice
                    },
                    BillingAddress = new AddressDto()
                    {
                        Name = dto.ClientName
                    }
                };
                var createOrderResult = await orderService.CreateVirtualPosOrderAsync(order, paymentUnitOfWork, cancellationToken);
                if (!createOrderResult.IsSuccess)
                {
                    paymentUnitOfWork.Rollback();
                    return ServiceResult<InitializePaymentResponseDto>.Error("Sipariş Oluşturulamadı", "Sipariş oluşturmada hata meydana geldi", HttpStatusCode.BadRequest);
                }
                paymentUnitOfWork.Commit();

                return ServiceResult<InitializePaymentResponseDto>.SuccessAsOk(new InitializePaymentResponseDto()
                {
                    Parameters = parameters,
                    ThreeDVerificationUrl = paymentOptions.ThreeDVerificationUrl
                });
            }
            catch (Exception ex)
            {
                paymentUnitOfWork.Rollback();

                logger.LogWarning(ex, "InitializeVirtualPosPaymentAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<InitializePaymentResponseDto>> InitializePurchasePaymentAsync(InitializePurchasePaymentDto dto, CancellationToken cancellationToken)
        {
            paymentUnitOfWork.BeginTransaction();

            try
            {
                var orderCode = OrderCodeGeneratorHelper.Generate();

                var cart = await cartService.GetCartAsync(cancellationToken, paymentUnitOfWork);
                if (!cart.IsSuccess || cart.Data == null || cart.Data.ItemCount == 0)
                {
                    return ServiceResult<InitializePaymentResponseDto>.Error("Sepet Bulunamadı", "Ödeme işlemi başlatılamadı sepet bulunamadı.", HttpStatusCode.BadRequest);
                }

                var amountStr = IsBankHelper.IsBankAmountConvert(cart.Data.Total);
                dto.Order.Code = orderCode;
                dto.Order.CartSessionId = cartSessionService.GetCartKey();
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

                var createOrderResult = await orderService.CreatePurchaseOrderAsync(dto.Order, cart.Data, paymentUnitOfWork, cancellationToken);
                if (!createOrderResult.IsSuccess)
                {
                    paymentUnitOfWork.Rollback();
                    return ServiceResult<InitializePaymentResponseDto>.Error("Sipariş Oluşturulamadı", "Sipariş oluşturmada hata meydana geldi", HttpStatusCode.BadRequest);
                }

                paymentUnitOfWork.Commit();

                return ServiceResult<InitializePaymentResponseDto>.SuccessAsOk(new InitializePaymentResponseDto()
                {
                    Parameters = parameters,
                    ThreeDVerificationUrl = paymentOptions.ThreeDVerificationUrl
                });
            }
            catch (Exception ex)
            {
                paymentUnitOfWork.Rollback();
                logger.LogWarning(ex, "InitializePurchasePaymentAsync işleminde hata");
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

            parameters["hash"] = isBankPaymentService.GenerateHash(parameters);

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

                var isBankResponse = await isBankPaymentService.SendPaymentRequestAsync(parameters, cancellationToken);
                if (isBankResponse == null)
                {
                    logger.LogError($"Bankadan cevap alınamadı");
                    return ServiceResult<string>.Error("Banka Cevap", "Bankadan cevap alınamadı", HttpStatusCode.BadRequest);
                }
                var isPaymentSuccessful = IsBankHelper.IsPaymentSuccess(isBankResponse);
                var paymentId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;
                var totalAmount = decimal.Parse(parameters.GetValueOrDefault("amount")?.Replace(",", ".") ?? "0", CultureInfo.InvariantCulture);
                string maskedCreditCard = parameters.GetValueOrDefault("maskedCreditCard");
                ServiceResult<OrderDto> order;
                Guid orderId;

                paymentUnitOfWork.BeginTransaction();

                try
                {
                    order = await orderService.GetOrderByCodeAsync(orderCode, paymentUnitOfWork);
                    if (order == null)
                    {
                        paymentUnitOfWork.Rollback();
                        logger.LogError("Güncellenecek sipariş kaydı bulunamadı. OrderCode: {OrderCode}", orderCode);
                        return ServiceResult<string>.Error(
                            "Sipariş Bulunamadı",
                            $"{orderCode} sipariş kodu ile sipariş kaydı bulunamadı",
                            HttpStatusCode.NotFound);
                    }

                    orderId = order.Data.Id;

                    // 1. ÖDEME OLUŞTUR
                    await paymentRepository.CreatePaymentAsync(
                        paymentId,
                        userId,
                        orderCode,
                        totalAmount,
                        isPaymentSuccessful ? PaymentStatus.Completed : PaymentStatus.Failed,
                        clientInfoProvider.GetClientIp(),
                        isBankResponse.Response,
                        isBankResponse.AuthCode,
                        isBankResponse.HostRefNum,
                        isBankResponse.ProcReturnCode,
                        isBankResponse.TransId,
                        isBankResponse.ErrMsg,
                        isBankResponse.ErrorCode,
                        isBankResponse.SettleId,
                        isBankResponse.TrxDate,
                        isBankResponse.CardBrand,
                        isBankResponse.CardIssuer,
                        isBankResponse.AvsApprove,
                        isBankResponse.HostDate,
                        isBankResponse.AvsErrorCodeDetail,
                        isBankResponse.NumCode,
                        maskedCreditCard,
                        paymentUnitOfWork);

                    logger.LogInformation($"{paymentId} ID'li ödeme kaydı oluşturuldu");

                    // 2. ÖDEMEYE AİT SİPARİŞİ GÜNCELLE
                    var affectedRows = await paymentRepository.UpdateOrderPaymentLinkAsync(
                        orderId,
                        paymentId,
                        isPaymentSuccessful ? OrderStatus.Paid : OrderStatus.PaymentFailed,
                        paymentUnitOfWork);

                    if (affectedRows == 0)
                    {
                        paymentUnitOfWork.Rollback();
                        logger.LogError("Sipariş güncellenemedi. OrderCode: {OrderCode}", orderCode);
                        return ServiceResult<string>.Error("Sipariş Güncellenemedi", "Sipariş durumu güncellenemedi", HttpStatusCode.BadRequest);
                    }

                    logger.LogInformation($"{orderId} ID'li sipariş, {paymentId} ID'li ödeme ile ilişkilendirildi ve güncellendi");

                    paymentUnitOfWork.Commit();
                }
                catch (Exception ex)
                {
                    paymentUnitOfWork.Rollback();
                    logger.LogError(ex, "Ödeme/sipariş DB işleminde hata. OrderCode: {OrderCode}", orderCode);
                    return ServiceResult<string>.Error("Ödeme Başarısız", $"{isBankResponse.ErrMsg}", HttpStatusCode.BadRequest);
                }

                // E-posta gönderimi ve sepet temizleme transaction dışında yapılır.
                // DB commit edildiyse, bu işlemlerdeki hatalar ödeme sonucunu etkilemez.
                var paymentDto = new PaymentDto
                {
                    Id = paymentId,
                    UserId = userId,
                    OrderCode = orderCode,
                    TotalAmount = totalAmount,
                    MaskedCreditCard = maskedCreditCard,
                    BankCardBrand = isBankResponse.CardBrand,
                    BankCardIssuer = isBankResponse.CardIssuer,
                    BankProcReturnCode = isBankResponse.ProcReturnCode,
                    BankErrorCode = isBankResponse.ErrorCode,
                    BankErrMsg = isBankResponse.ErrMsg,
                    CreatedAt = DateTime.Now,
                    Status = isPaymentSuccessful ? PaymentStatus.Completed : PaymentStatus.Failed,
                    IsSuccess = isPaymentSuccessful
                };

                if (!isPaymentSuccessful)
                {
                    try
                    {
                        await emailService.SendPaymentFailedMailAsync(order.Data, paymentDto, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Ödeme başarısız e-postası gönderilemedi. OrderCode: {OrderCode}", orderCode);
                    }

                    logger.LogError($"{orderCode} sipariş kodlu ödeme başarısız. Bankadan dönen Mesaj:{isBankResponse.ErrMsg} Kod: {isBankResponse.ErrorCode}");
                    return ServiceResult<string>.Error("Ödeme Başarısız", $"{isBankResponse.ErrMsg}", HttpStatusCode.BadRequest);
                }

                try
                {
                    // Sipariş oluşturulurken kaydedilen cart key ile sepeti temizle
                    if (!string.IsNullOrEmpty(order.Data.CartSessionId))
                    {
                        await cartService.ClearCartBySessionIdAsync(order.Data.CartSessionId, cancellationToken);
                        logger.LogInformation("Sepet temizlendi. CartSessionId: {CartSessionId}, OrderCode: {OrderCode}", order.Data.CartSessionId, orderCode);
                    }

                    switch (order.Data.OrderType)
                    {
                        case OrderType.VirtualPOS:
                            await emailService.SendVirtualPosPaymentSuccessMailAsync(order.Data, paymentDto, cancellationToken);
                            break;

                        case OrderType.Purchase:
                            await emailService.SendPaymentSuccessMailAsync(order.Data, cancellationToken);
                            break;

                        default:
                            await emailService.SendPaymentSuccessMailAsync(order.Data, cancellationToken);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Ödeme sonrası işlemlerde hata (sepet/e-posta). OrderCode: {OrderCode}", orderCode);
                }

                // Ödeme başarılı bildirimi
                try
                {
                    if (!string.IsNullOrEmpty(userId))
                    {
                        var (title, message) = order.Data.OrderType switch
                        {
                            OrderType.VirtualPOS => ("Ödeme Başarılı", $"Sanal POS ödemesi ({orderCode}) başarıyla tamamlandı."),
                            OrderType.Purchase => ("Sipariş Başarılı", $"Siparişiniz ({orderCode}) başarıyla oluşturuldu."),
                            _ => ("Ödeme Başarılı", $"Ödemeniz ({orderCode}) başarıyla tamamlandı.")
                        };

                        await notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            Title = title,
                            Message = message,
                            Type = NotificationType.OrderStatusChanged,
                            RedirectUrl = "/siparislerim",
                            TargetUserId = userId
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Ödeme başarılı bildirimi gönderilemedi. OrderCode: {OrderCode}", orderCode);
                }

                logger.LogInformation($"{orderCode} sipariş kodlu ödeme işlemi başarıyla tamamlandı");

                return ServiceResult<string>.SuccessAsCreated(orderCode, $"/api/payments/{paymentId}");
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
                var result = await paymentRepository.GetByIdAsync(paymentId);
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

                var result = await paymentRepository.GetByOrderCodeAsync(orderCode);
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
                var result = await paymentRepository.GetAllAsync();
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

                var result = await paymentRepository.GetByUserAsync(userId);
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
