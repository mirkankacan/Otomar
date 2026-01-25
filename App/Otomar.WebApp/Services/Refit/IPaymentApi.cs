using Otomar.WebApp.Dtos.Payment;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IPaymentApi
    {
        [Post("/api/payments/initialize/purchase")]
        Task<InitializePaymentResponseDto> InitializePurchasePaymentAsync([Body] InitializePurchasePaymentDto dto, CancellationToken cancellationToken = default);

        [Post("/api/payments/initialize/virtual-pos")]
        Task<InitializePaymentResponseDto> InitializeVirtualPosPaymentAsync([Body] InitializeVirtualPosPaymentDto dto, CancellationToken cancellationToken = default);

        [Get("/api/payments")]
        Task<IEnumerable<PaymentDto>> GetPaymentsAsync(CancellationToken cancellationToken = default);

        [Get("/api/payments/user/{userId}")]
        Task<IEnumerable<PaymentDto>> GetPaymentsByUserAsync(string userId, CancellationToken cancellationToken = default);

        [Get("/api/payments/{paymentId}")]
        Task<PaymentDto> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

        [Get("/api/payments/{orderCode}")]
        Task<PaymentDto> GetPaymentByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    }
}
