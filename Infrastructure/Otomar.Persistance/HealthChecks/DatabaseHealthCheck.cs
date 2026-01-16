using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Otomar.Persistance.Data;

namespace Otomar.Persistance.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly IAppDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(
            IAppDbContext context,
            ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var startTime = DateTime.UtcNow;

                // Stok tablosundan hızlı count (gerçek yük testi)
                var stockCount = await _context.Connection.QueryFirstOrDefaultAsync<int>(
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
                _logger.LogError(ex, "Database health check başarısız");
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