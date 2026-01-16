using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Otomar.Persistance.Options;

namespace Otomar.Persistance.HealthChecks
{
    public class EmailServiceHealthCheck : IHealthCheck
    {
        private readonly EmailOptions _emailOptions;
        private readonly ILogger<EmailServiceHealthCheck> _logger;

        public EmailServiceHealthCheck(
            EmailOptions emailOptions,
            ILogger<EmailServiceHealthCheck> logger)
        {
            _emailOptions = emailOptions;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                using var client = new SmtpClient();

                var secureOption = _emailOptions.EnableSsl
                    ? SecureSocketOptions.SslOnConnect
                    : SecureSocketOptions.StartTlsWhenAvailable;

                if (_emailOptions.Port == 587)
                    secureOption = SecureSocketOptions.StartTls;

                await client.ConnectAsync(
                    _emailOptions.Host,
                    _emailOptions.Port,
                    secureOption,
                    cancellationToken);

                var duration = DateTime.UtcNow - startTime;

                if (!client.IsConnected)
                {
                    return HealthCheckResult.Unhealthy(
                        "SMTP sunucusuna bağlanılamadı",
                        data: new Dictionary<string, object>
                        {
                            { "smtp_host", _emailOptions.Host },
                            { "smtp_port", _emailOptions.Port },
                            { "timestamp", DateTime.UtcNow }
                        });
                }

                await client.DisconnectAsync(true, cancellationToken);

                return HealthCheckResult.Healthy(
                    "Email servisi çalışıyor",
                    data: new Dictionary<string, object>
                    {
                        { "smtp_host", _emailOptions.Host },
                        { "smtp_port", _emailOptions.Port },
                        { "response_time_ms", duration.TotalMilliseconds },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email service health check başarısız");

                return HealthCheckResult.Unhealthy(
                    "Email servisi erişilemez",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "smtp_host", _emailOptions.Host },
                        { "smtp_port", _emailOptions.Port },
                        { "timestamp", DateTime.UtcNow },
                        { "error", ex.Message }
                    });
            }
        }
    }
}