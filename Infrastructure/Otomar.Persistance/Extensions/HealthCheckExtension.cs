using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Otomar.Persistance.HealthChecks;
using Otomar.Persistance.Options;

namespace Otomar.Persistance.Extensions
{
    public static class HealthCheckExtension
    {
        public static IServiceCollection AddHealthCheckServices(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("SqlConnection");
            var uiOptions = configuration.GetSection(nameof(UiOptions)).Get<UiOptions>()!;
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            //  Health Checks
            services.AddHealthChecks()
                //  SQL Server Bağlantısı
                .AddSqlServer(
                    connectionString: connectionString!,
                    healthQuery: "SELECT 1;",
                    name: "sql-server",
                    failureStatus: HealthStatus.Unhealthy,
                    tags: new[] { "database" })

                //  Database İşlemleri
                .AddCheck<DatabaseHealthCheck>(
                    "database-operations",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "database" })

                //  Email SMTP
                .AddCheck<EmailServiceHealthCheck>(
                    "email-smtp",
                    failureStatus: HealthStatus.Degraded,
                    tags: new[] { "external" });

            //  Health Checks UI
            services
                .AddHealthChecksUI(setup =>
                {
                    setup.SetEvaluationTimeInSeconds(30);
                    setup.MaximumHistoryEntriesPerEndpoint(100);

                    // WebAPI kendi endpoint'i
                    setup.AddHealthCheckEndpoint(
                        "OTOMAR Yedek Parça Web Api",
                        "/health");

                    // WebUI endpoint'i (Development dışında)
                    if (environment != "Development")
                    {
                        setup.AddHealthCheckEndpoint(
                            "OTOMAR Yedek Parça Web App",
                            uiOptions.BaseUrl + "/health");
                    }
                    else
                    {
                        setup.AddHealthCheckEndpoint(
                          "OTOMAR Yedek Parça Web App",
                         "https://localhost:8484/health");
                    }
                })
                .AddInMemoryStorage();

            return services;
        }

        public static IEndpointRouteBuilder MapHealthCheckServices(this IEndpointRouteBuilder app)
        {
            // Health Check Endpoint
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
                ResultStatusCodes =
                {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
                }
            })
            .AllowAnonymous();

            //  Health UI Dashboard
            app.MapHealthChecksUI(setup =>
            {
                setup.UIPath = "/health-ui";
                setup.ApiPath = "/health-ui-api";
                setup.ResourcesPath = "/health-ui-resources";
                setup.WebhookPath = "/health-ui-webhook";
            })
            .AllowAnonymous();
            return app;
        }
    }
}