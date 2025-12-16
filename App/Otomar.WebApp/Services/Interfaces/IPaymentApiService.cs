using Otomar.WebApp.Models.Payment;
using Otomar.WebApp.Responses;

namespace Otomar.WebApp.Services.Interfaces
{
    public interface IPaymentApiService
    {
        Task<ApiResponse<Guid>> CreatePaymentAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default);

        Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsAsync(CancellationToken cancellationToken = default);

        Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsByUserAsync(string userId, CancellationToken cancellationToken = default);

        Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    }
}