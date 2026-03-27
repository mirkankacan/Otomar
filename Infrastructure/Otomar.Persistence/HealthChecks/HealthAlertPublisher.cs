using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Otomar.Application.Interfaces.Services;

namespace Otomar.Persistence.HealthChecks
{
    public class HealthAlertPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<HealthAlertPublisher> logger) : IHealthCheckPublisher
    {
        private readonly ConcurrentDictionary<string, HealthStatus> _previousStatuses = new();

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            foreach (var (checkName, entry) in report.Entries)
            {
                var previousStatus = _previousStatuses.GetOrAdd(checkName, HealthStatus.Healthy);
                _previousStatuses[checkName] = entry.Status;

                if (entry.Status == previousStatus)
                    continue;

                if (entry.Status is HealthStatus.Unhealthy or HealthStatus.Degraded)
                {
                    await SendAlertSafeAsync(checkName, entry, cancellationToken);
                }
                else if (entry.Status == HealthStatus.Healthy)
                {
                    await SendRecoverySafeAsync(checkName, cancellationToken);
                }
            }
        }

        private async Task SendAlertSafeAsync(string checkName, HealthReportEntry entry, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendHealthAlertAsync(
                    checkName,
                    entry.Status.ToString(),
                    entry.Description,
                    entry.Exception?.Message,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health alert e-postası gönderilemedi. Check: {CheckName}", checkName);
            }
        }

        private async Task SendRecoverySafeAsync(string checkName, CancellationToken cancellationToken)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                await emailService.SendHealthAlertAsync(
                    checkName,
                    "Healthy",
                    "Servis normale döndü.",
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Health recovery e-postası gönderilemedi. Check: {CheckName}", checkName);
            }
        }
    }
}
