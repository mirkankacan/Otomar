using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Otomar.Application.Options;

namespace Otomar.Persistence.HealthChecks
{
    public class PaymentGatewayHealthCheck(
        IHttpClientFactory httpClientFactory,
        PaymentOptions paymentOptions,
        ILogger<PaymentGatewayHealthCheck> logger) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                using var response = await client.GetAsync(
                    paymentOptions.ApiUrl,
                    cancellationToken);

                var duration = DateTime.UtcNow - startTime;

                return HealthCheckResult.Healthy(
                    "Ödeme servisi erişilebilir",
                    data: new Dictionary<string, object>
                    {
                        { "api_url", paymentOptions.ApiUrl },
                        { "status_code", (int)response.StatusCode },
                        { "response_time_ms", duration.TotalMilliseconds },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment gateway health check başarısız");

                return HealthCheckResult.Degraded(
                    "Ödeme servisine erişilemiyor",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "api_url", paymentOptions.ApiUrl },
                        { "timestamp", DateTime.UtcNow },
                        { "error", ex.Message }
                    });
            }
        }
    }
}
