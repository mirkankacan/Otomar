using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Otomar.Application.Interfaces;

namespace Otomar.Persistence.HealthChecks
{
    public class DatabaseHealthCheck(
        IUnitOfWork context,
        ILogger<DatabaseHealthCheck> logger) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext healthCheckContext,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // Stok tablosundan hızlı count (gerçek yük testi)
                var stockCount = await context.Connection.QueryFirstOrDefaultAsync<int>(
                    new CommandDefinition(
                        "SELECT COUNT(1) FROM IdvStock WITH (NOLOCK)",
                        cancellationToken: cancellationToken));

                var duration = DateTime.UtcNow - startTime;

                if (duration.TotalSeconds > 3)
                {
                    return HealthCheckResult.Degraded(
                        "Veritabanı yavaş yanıt veriyor",
                        data: new Dictionary<string, object>
                        {
                            { "response_time_ms", duration.TotalMilliseconds },
                            { "threshold_ms", 3000 },
                            { "stock_count", stockCount },
                            { "timestamp", DateTime.UtcNow }
                        });
                }

                return HealthCheckResult.Healthy(
                    "Veritabanı çalışıyor",
                    data: new Dictionary<string, object>
                    {
                        { "response_time_ms", duration.TotalMilliseconds },
                        { "stock_count", stockCount },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database health check başarısız");
                return HealthCheckResult.Unhealthy(
                    "Veritabanı hatası",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        { "error", ex.Message },
                        { "timestamp", DateTime.UtcNow }
                    });
            }
        }
    }
}