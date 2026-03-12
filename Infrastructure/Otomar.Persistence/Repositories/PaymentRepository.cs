using Dapper;
using Otomar.Application.Contracts.Persistence;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Shared.Dtos.Payment;
using Otomar.Shared.Enums;

namespace Otomar.Persistence.Repositories
{
    /// <summary>
    /// Ödeme verilerine erişim implementasyonu.
    /// Tüm SQL sorguları bu katmanda bulunur; tekrarlanan SELECT blokları const ile DRY tutulur.
    /// </summary>
    public class PaymentRepository(IAppDbContext context) : IPaymentRepository
    {
        #region SQL Column Constants

        private const string PaymentSelectColumns = @"
                        Id,
                        UserId,
                        OrderCode,
                        TotalAmount,
                        BankCardBrand,
                        BankCardIssuer,
                        Status,
                        CreatedAt,
                        BankProcReturnCode,
                        MaskedCreditCard,
                        BankErrorCode,
                        BankErrMsg";

        #endregion

        /// <inheritdoc />
        public async Task CreatePaymentAsync(
            Guid paymentId,
            string? userId,
            string orderCode,
            decimal totalAmount,
            PaymentStatus status,
            string? ipAddress,
            string? bankResponse,
            string? bankAuthCode,
            string? bankHostRefNum,
            string? bankProcReturnCode,
            string? bankTransId,
            string? bankErrMsg,
            string? bankErrorCode,
            string? bankSettleId,
            string? bankTrxDate,
            string? bankCardBrand,
            string? bankCardIssuer,
            string? bankAvsApprove,
            string? bankHostDate,
            string? bankAvsErrorCodeDetail,
            string? bankNumCode,
            string? maskedCreditCard,
            IUnitOfWork unitOfWork)
        {
            const string query = @"
                INSERT INTO IdtPayments (Id, UserId, OrderCode, TotalAmount, Status, CreatedAt, IpAddress,
                    BankResponse, BankAuthCode, BankHostRefNum, BankProcReturnCode, BankTransId,
                    BankErrMsg, BankErrorCode, BankSettleId, BankTrxDate, BankCardBrand, BankCardIssuer,
                    BankAvsApprove, BankHostDate, BankAvsErrorCodeDetail, BankNumCode, MaskedCreditCard)
                VALUES (@Id, @UserId, @OrderCode, @TotalAmount, @Status, @CreatedAt, @IpAddress,
                    @BankResponse, @BankAuthCode, @BankHostRefNum, @BankProcReturnCode, @BankTransId,
                    @BankErrMsg, @BankErrorCode, @BankSettleId, @BankTrxDate, @BankCardBrand, @BankCardIssuer,
                    @BankAvsApprove, @BankHostDate, @BankAvsErrorCodeDetail, @BankNumCode, @MaskedCreditCard)";

            var parameters = new DynamicParameters();
            parameters.Add("Id", paymentId);
            parameters.Add("UserId", userId);
            parameters.Add("OrderCode", orderCode);
            parameters.Add("TotalAmount", totalAmount);
            parameters.Add("Status", status);
            parameters.Add("CreatedAt", DateTime.Now);
            parameters.Add("IpAddress", ipAddress);
            parameters.Add("BankResponse", bankResponse);
            parameters.Add("BankAuthCode", bankAuthCode);
            parameters.Add("BankHostRefNum", bankHostRefNum);
            parameters.Add("BankProcReturnCode", bankProcReturnCode);
            parameters.Add("BankTransId", bankTransId);
            parameters.Add("BankErrMsg", bankErrMsg);
            parameters.Add("BankErrorCode", bankErrorCode);
            parameters.Add("BankSettleId", bankSettleId);
            parameters.Add("BankTrxDate", bankTrxDate);
            parameters.Add("BankCardBrand", bankCardBrand);
            parameters.Add("BankCardIssuer", bankCardIssuer);
            parameters.Add("BankAvsApprove", bankAvsApprove);
            parameters.Add("BankHostDate", bankHostDate);
            parameters.Add("BankAvsErrorCodeDetail", bankAvsErrorCodeDetail);
            parameters.Add("BankNumCode", bankNumCode);
            parameters.Add("MaskedCreditCard", maskedCreditCard);

            await unitOfWork.Connection.ExecuteAsync(query, parameters, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task<int> UpdateOrderPaymentLinkAsync(Guid orderId, Guid paymentId, OrderStatus status, IUnitOfWork unitOfWork)
        {
            const string query = @"
                UPDATE IdtOrders
                SET Status = @Status, PaymentId = @PaymentId, UpdatedAt = @UpdatedAt
                WHERE Id = @OrderId";

            var parameters = new DynamicParameters();
            parameters.Add("OrderId", orderId);
            parameters.Add("PaymentId", paymentId);
            parameters.Add("UpdatedAt", DateTime.Now);
            parameters.Add("Status", status);

            return await unitOfWork.Connection.ExecuteAsync(query, parameters, unitOfWork.Transaction);
        }

        /// <inheritdoc />
        public async Task<PaymentDto?> GetByIdAsync(Guid paymentId)
        {
            var query = $@"SELECT TOP 1 {PaymentSelectColumns}
                        FROM IdtPayments WITH (NOLOCK)
                        WHERE Id = @paymentId";

            return await context.Connection.QueryFirstOrDefaultAsync<PaymentDto>(query, new { paymentId });
        }

        /// <inheritdoc />
        public async Task<PaymentDto?> GetByOrderCodeAsync(string orderCode)
        {
            var query = $@"SELECT TOP 1 {PaymentSelectColumns}
                        FROM IdtPayments WITH (NOLOCK)
                        WHERE OrderCode = @orderCode";

            return await context.Connection.QueryFirstOrDefaultAsync<PaymentDto>(query, new { orderCode });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PaymentDto>> GetAllAsync()
        {
            var query = $@"SELECT {PaymentSelectColumns}
                        FROM IdtPayments WITH (NOLOCK)";

            return await context.Connection.QueryAsync<PaymentDto>(query);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<PaymentDto>> GetByUserAsync(string userId)
        {
            var query = $@"SELECT {PaymentSelectColumns}
                        FROM IdtPayments WITH (NOLOCK)
                        WHERE UserId = @userId";

            return await context.Connection.QueryAsync<PaymentDto>(query, new { userId });
        }
    }
}
