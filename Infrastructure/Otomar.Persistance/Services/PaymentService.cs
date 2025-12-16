using Dapper;
using MassTransit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Payment;
using Otomar.Application.Enums;
using Otomar.Persistance.Data;
using Otomar.Persistance.Options;
using System.Net;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Otomar.Persistance.Services
{
    public class PaymentService(IAppDbContext context, HttpClient httpClient, IHttpContextAccessor accessor, IIdentityService identityService, PaymentOptions paymentOptions, ILogger<PaymentService> logger) : IPaymentService
    {
        public async Task<ServiceResult<Guid>> CreatePaymentAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken)
        {
            using var transaction = context.Connection.BeginTransaction();
            try
            {
                string orderCode = parameters.GetValueOrDefault("oid");
                logger.LogInformation($"{orderCode} sipariş kodlu ödeme işlemi başladı");

                var isBankResponse = await IsBankPaymentRequest(parameters, cancellationToken);
                if (isBankResponse == null)
                {
                    transaction.Rollback();
                    logger.LogError($"Bankadan cevap alınamadı");
                    return ServiceResult<Guid>.Error("Bankadan cevap alınamadı", HttpStatusCode.BadRequest);
                }
                var isPaymentSuccessful = isBankResponse.ProcReturnCode == "00" && isBankResponse.Response.ToLower() == "approved";

                var paymentId = NewId.NextGuid();
                var userId = identityService.GetUserId() ?? null;
                var totalAmount = Convert.ToDecimal(parameters.GetValueOrDefault("amount"));
                var paymentInsertQuery = @"
                INSERT INTO IdtPayments (Id, UserId, OrderCode, TotalAmount, Status, CreatedAt, IpAddress, BankResponse, BankAuthCode, BankHostRefNum, BankProcReturnCode, BankTransId, BankErrMsg, BankErrorCode, BankSettleId, BankTrxDate, BankCardBrand, BankCardIssuer, BankAvsApprove, BankHostDate, BankAvsErrorCodeDetail, BankNumCode, CardNumber)
                VALUES (@Id, @UserId, @OrderCode, @TotalAmount, @Status, @CreatedAt, @IpAddress, @BankResponse, @BankAuthCode, @BankHostRefNum, @BankProcReturnCode, @BankTransId, @BankErrMsg, @BankErrorCode, @BankSettleId, @BankTrxDate, @BankCardBrand, @BankCardIssuer, @BankAvsApprove, @BankHostDate, @BankAvsErrorCodeDetail, @BankNumCode, @CardNumber);";

                var paymentParameters = new DynamicParameters();
                paymentParameters.Add("Id", paymentId);
                paymentParameters.Add("UserId", userId);
                paymentParameters.Add("OrderCode", orderCode);
                paymentParameters.Add("TotalAmount", totalAmount);
                paymentParameters.Add("Status", isPaymentSuccessful ? PaymentStatus.Completed : PaymentStatus.Failed);
                paymentParameters.Add("CreatedAt", DateTime.Now);
                paymentParameters.Add("IpAddress", GetIpAddress());
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
                paymentParameters.Add("CardNumber", parameters.GetValueOrDefault("md"));

                await context.Connection.ExecuteAsync(paymentInsertQuery, paymentParameters, transaction);

                // Order'ı bul ve güncelle
                var orderUpdateParameters = new DynamicParameters();
                orderUpdateParameters.Add("OrderCode", orderCode);
                orderUpdateParameters.Add("PaymentId", paymentId);
                orderUpdateParameters.Add("Status", isPaymentSuccessful ? OrderStatus.Paid : OrderStatus.PaymentFailed);

                var orderUpdateQuery = @"
                    UPDATE IdtOrders
                    SET Status = @Status, PaymentId = @PaymentId
                    WHERE Code = @OrderCode";

                var affectedRows = await context.Connection.ExecuteAsync(orderUpdateQuery, orderUpdateParameters, transaction);

                if (affectedRows == 0)
                {
                    transaction.Rollback();
                    logger.LogWarning($"{orderCode} kodlu sipariş bulunamadı");
                    return ServiceResult<Guid>.Error($"{orderCode} kodlu sipariş bulunamadı", HttpStatusCode.NotFound);
                }

                transaction.Commit();

                if (!isPaymentSuccessful)
                {
                    logger.LogError($"{orderCode} sipariş kodlu ödeme başarısız. Bankadan dönen Mesaj:{isBankResponse.ErrMsg} Kod: {isBankResponse.ErrorCode}");
                    return ServiceResult<Guid>.Error($"Ödeme başarısız", HttpStatusCode.BadRequest);
                }
                logger.LogInformation($"{orderCode} sipariş kodlu ödeme başarılı ve sipariş durumu güncellendi");

                return ServiceResult<Guid>.SuccessAsCreated(paymentId, $"/api/payments/{paymentId}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, message: "CreatePaymentAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(Guid paymentId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("paymentId", paymentId);
                var query = $@"SELECT TOP 1 Id, UserId, OrderCode, TotalAmount,CardNumber, CardBrand, Status, CreatedAt FROM IdtPayments WITH (NOLOCK) WHERE Id = @paymentId";

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
                var parameters = new DynamicParameters();
                parameters.Add("orderCode", orderCode);
                var query = $@"SELECT TOP 1 Id, UserId, OrderCode, TotalAmount, CardNumber,CardBrand, Status, CreatedAt FROM IdtPayments WITH (NOLOCK) WHERE OrderCode = @orderCode";

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
                 SELECT  Id,UserId, OrderCode, TotalAmount,CardNumber,CardBrand, Status, CreatedAt
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
                 SELECT Id,UserId, OrderCode, TotalAmount,CardNumber,CardBrand, Status, CreatedAt
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

        private string GetIpAddress()
        {
            var httpContext = accessor.HttpContext;
            if (httpContext == null)
                return string.Empty;

            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (ips.Length > 0)
                {
                    return ips[0].Trim();
                }
            }

            var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString();
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
                new XElement("Name", paymentOptions.UserName),
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
    }
}