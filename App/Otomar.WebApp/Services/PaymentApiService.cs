using Otomar.WebApp.Models.Payment;
using Otomar.WebApp.Responses;
using Otomar.WebApp.Services.Interfaces;

namespace Otomar.WebApp.Services
{
    public class PaymentApiService(IApiService apiService) : IPaymentApiService
    {
        private const string BaseEndpoint = "api/payments";

        public async Task<ApiResponse<Guid>> CreatePaymentAsync(Dictionary<string, string> parameters, CancellationToken cancellationToken = default)
        {
            return await apiService.PostAsync<Guid>($"{BaseEndpoint}/", parameters, cancellationToken);
        }

        public async Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsAsync(CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<IEnumerable<PaymentDto>>($"{BaseEndpoint}/", cancellationToken);
        }

        public async Task<ApiResponse<IEnumerable<PaymentDto>>> GetPaymentsByUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<IEnumerable<PaymentDto>>($"{BaseEndpoint}/user/{userId}", cancellationToken);
        }

        public async Task<ApiResponse<PaymentDto>> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<PaymentDto>($"{BaseEndpoint}/{paymentId}", cancellationToken);
        }

        public async Task<ApiResponse<Dictionary<string, string>>> InitializePaymentAsync(InitializePaymentDto initializePaymentDto, CancellationToken cancellationToken)
        {
            return await apiService.PostAsync<Dictionary<string, string>>($"{BaseEndpoint}/initialize", initializePaymentDto, cancellationToken);
        }

        public async Task<ApiResponse<Dictionary<string, string>>> GetPaymentParamsAsync(string orderCode, CancellationToken cancellationToken = default)
        {
            return await apiService.GetAsync<Dictionary<string, string>>($"{BaseEndpoint}/params/{orderCode}", cancellationToken);
        }
    }
}