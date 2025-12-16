using Otomar.Application.Common;
using Otomar.Application.Dtos.Payment;

namespace Otomar.Application.Contracts.Services
{
    public interface IPaymentService
    {
        Task<ServiceResult<Guid>> CreatePaymentAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken);

        Task<ServiceResult<IEnumerable<PaymentDto>>> GetPaymentsByUserAsync(string userId);

        Task<ServiceResult<PaymentDto>> GetPaymentByIdAsync(Guid paymentId);

        Task<ServiceResult<PaymentDto>> GetPaymentByOrderCodeAsync(string orderCode);

        Task<ServiceResult<IEnumerable<PaymentDto>>> GetPaymentsAsync();
    }
}