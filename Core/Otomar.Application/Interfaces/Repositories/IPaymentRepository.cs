using Otomar.Shared.Dtos.Payment;
using Otomar.Shared.Enums;

namespace Otomar.Application.Interfaces.Repositories
{
    /// <summary>
    /// Ödeme verilerine erişim sözleşmesi.
    /// Tüm transactional metodlar IUnitOfWork parametresi alır.
    /// </summary>
    public interface IPaymentRepository
    {
        /// <summary>
        /// IdtPayments tablosuna yeni ödeme kaydı ekler.
        /// </summary>
        Task CreatePaymentAsync(
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
            IUnitOfWork unitOfWork);

        /// <summary>
        /// Siparişe ödeme bağlar ve durumunu günceller. Etkilenen satır sayısını döner.
        /// </summary>
        Task<int> UpdateOrderPaymentLinkAsync(Guid orderId, Guid paymentId, OrderStatus status, IUnitOfWork unitOfWork);

        /// <summary>
        /// Ödeme ID'sine göre ödeme getirir.
        /// </summary>
        Task<PaymentDto?> GetByIdAsync(Guid paymentId);

        /// <summary>
        /// Sipariş koduna göre ödeme getirir.
        /// </summary>
        Task<PaymentDto?> GetByOrderCodeAsync(string orderCode);

        /// <summary>
        /// Tüm ödemeleri getirir.
        /// </summary>
        Task<IEnumerable<PaymentDto>> GetAllAsync();

        /// <summary>
        /// Kullanıcıya ait ödemeleri getirir.
        /// </summary>
        Task<IEnumerable<PaymentDto>> GetByUserAsync(string userId);
    }
}
