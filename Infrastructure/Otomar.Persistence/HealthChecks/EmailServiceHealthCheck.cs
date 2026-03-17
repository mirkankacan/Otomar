using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Otomar.Persistence.Options;

namespace Otomar.Persistence.HealthChecks
{
    public class EmailServiceHealthCheck(
        EmailOptions emailOptions,
        ILogger<EmailServiceHealthCheck> logger) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                using var client = new SmtpClient();

                var secureOption = emailOptions.EnableSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTlsWhenAvailable;

                if (emailOptions.Port == 587)
                    secureOption = SecureSocketOptions.StartTls;

                await client.ConnectAsync(
                    emailOptions.Host,
                    emailOptions.Port,
                    secureOption,
                    cancellationToken);

                var duration = DateTime.UtcNow - startTime;

                if (!client.IsConnected)
                {
                    return HealthCheckResult.Unhealthy(
                        "SMTP sunucusuna bağlanılamadı",
                        data: new Dictionary<string, object>
                        {
                            { "smtp_host", emailOptions.Host },
                            { "smtp_port", emailOptions.Port },
                            { "timestamp", DateTime.UtcNow }
                        });
                }

                await client.DisconnectAsync(true, cancellationToken);

                return HealthCheckResult.Healthy(
                    "Email servisi çalışıyor",
                    data: new Dictionary<string, object>
                    {
                        { "smtp_host", emailOptions.Host },
                        { "smtp_port", emailOptions.Port },
                        { "response_time_ms", duration.TotalMilliseconds },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Email service health check başarısız");

                return HealthCheckResult.Unhealthy(
                    "Email servisi erişilemez",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "smtp_host", emailOptions.Host },
                        { "smtp_port", emailOptions.Port },
                        { "timestamp", DateTime.UtcNow },
                        { "error", ex.Message }
                    });
            }
        }
    }
}