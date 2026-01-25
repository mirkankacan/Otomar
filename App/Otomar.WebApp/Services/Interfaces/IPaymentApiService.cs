using Otomar.WebApp.Models.Payment;
using Otomar.WebApp.Responses;

namespace Otomar.WebApp.Services.Interfaces
{
    public interface IPaymentApiService
    {
        Task<ApiResponse<InitializePaymentResponseDto>> InitializePurchasePaymentAsync(InitializePurchasePaymentDto dto, CancellationToken cancellationToken);

        Task<ApiResponse<InitializePaymentResponseDto>> InitializeVirtualPosPaymentAsync(InitializeVirtualPosPaymentDto dto, CancellationToken cancellationToken);

        Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsAsync(CancellationToken cancellationToken = default);

        Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsByUserAsync(string userId, CancellationToken cancellationToken = default);

        Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

        Task<ApiResponse<PaymentDto>> GetPaymentByOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default);
    }
}