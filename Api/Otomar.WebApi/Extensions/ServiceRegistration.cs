using Carter;
using Microsoft.OpenApi.Models;
using Otomar.WebApi.Middlewares;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using System.Collections.ObjectModel;
using System.Data;

namespace Otomar.WebApi.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration, IHostBuilder host)
        {
            var connectionString = configuration.GetConnectionString("SqlConnection");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            services.AddScoped<GlobalExceptionMiddleware>();
            services.AddEndpointsApiExplorer();

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Otomar.WebApi")
                .Enrich.WithProperty("Environment", environment)
                .Enrich.WithClientIp()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Logger(lc =>
                {
                    if (environment == "Development")
                    {
                        lc.WriteTo.Console(
                            restrictedToMinimumLevel: LogEventLevel.Debug,
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
                    }
                })
                .WriteTo.File("logs/api-.log",
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10_000_000,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .WriteTo.MSSqlServer(
                    connectionString: connectionString,
                    sinkOptions: new MSSqlServerSinkOptions
                    {
                        TableName = "IdtLogs",
                        SchemaName = "dbo",
                        AutoCreateSqlTable = true,
                        BatchPostingLimit = 500,
                        BatchPeriod = TimeSpan.FromSeconds(15)
                    },
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    columnOptions: GetColumnOptions())
                //.WriteTo.Email(
                //    from: emailOptions.Credentials.Username,
                //    to: emailOptions.ErrorTo,
                //    host: emailOptions.Host,
                //    port: emailOptions.Port,
                //    connectionSecurity: emailOptions.EnableSsl
                //        ? MailKit.Security.SecureSocketOptions.SslOnConnect
                //        : MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable,
                //    credentials: new NetworkCredential(
                //        emailOptions.Credentials.Username,
                //        emailOptions.Credentials.Password
                //    ),
                //    subject: "🚨 Otomar.WebApi - {Level} - {Timestamp:dd-MM-yyyy HH:mm}",
                //    restrictedToMinimumLevel: LogEventLevel.Error
                //)
                .CreateLogger();

            host.UseSerilog();
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Otomar.WebApi",
                    Version = "v1",
                    Description = "OTOMAR Yedek Parça E-Ticaret Sitesi API"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT token giriniz. Sadece token yazın, 'Bearer' eklemeyin.\n\nÖrnek: eyJhbGciOiJIUzI1NiIs..."
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            services.AddCarter();

            return services;
        }

        private static ColumnOptions GetColumnOptions()
        {
            var columnOptions = new ColumnOptions();

            columnOptions.Store.Remove(StandardColumn.MessageTemplate);

            columnOptions.Store.Add(StandardColumn.LogEvent);

            columnOptions.DisableTriggers = true;

            columnOptions.AdditionalColumns = new Collection<SqlColumn>
            {
                new SqlColumn { ColumnName = "UserId", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
                new SqlColumn { ColumnName = "ClientCode", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true },
                new SqlColumn { ColumnName = "Action", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
                new SqlColumn { ColumnName = "Module", DataType = SqlDbType.NVarChar, DataLength = 100, AllowNull = true },
                new SqlColumn { ColumnName = "ClientIP", DataType = SqlDbType.NVarChar, DataLength = 45, AllowNull = true },
                new SqlColumn { ColumnName = "UserAgent", DataType = SqlDbType.NVarChar, DataLength = 500, AllowNull = true },
                new SqlColumn { ColumnName = "RequestId", DataType = SqlDbType.NVarChar, DataLength = 50, AllowNull = true }
            };

            return columnOptions;
        }
    }
}