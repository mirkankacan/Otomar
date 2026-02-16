using System.Collections.ObjectModel;
using System.Data;
using System.Net;
using Carter;
using Microsoft.OpenApi.Models;
using Otomar.Persistance.Options;
using Otomar.WebApi.Middlewares;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Display;
using Serilog.Sinks.Email;
using Serilog.Sinks.MSSqlServer;

namespace Otomar.WebApi.Extensions
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddWebApiServices(this IServiceCollection services, IConfiguration configuration, IHostBuilder host)
        {
            var connectionString = configuration.GetConnectionString("SqlConnection");
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            var emailOptions = configuration.GetSection("EmailOptions").Get<EmailOptions>();

            services.AddScoped<GlobalExceptionMiddleware>();
            services.AddEndpointsApiExplorer();
            services.AddAuthorization();

            var loggerConfig = new LoggerConfiguration()
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
                    columnOptions: GetColumnOptions());

            if (emailOptions?.ErrorTo?.Count > 0 && emailOptions.Credentials is not null)
            {
                loggerConfig.WriteTo.Logger(lc => lc
                    .Filter.ByExcluding(e =>
                        e.Exception is TimeoutException
                        || e.Exception is TaskCanceledException
                        || e.Exception is OperationCanceledException
                        || (e.Exception?.InnerException is TimeoutException)
                        || (e.Exception?.InnerException is TaskCanceledException))
                    .WriteTo.Email(
                        new EmailSinkOptions
                        {
                            From = "Mirkan Kaçan",
                            To = emailOptions.ErrorTo,
                            Host = emailOptions.Host,
                            Port = emailOptions.Port,
                            ConnectionSecurity = emailOptions.EnableSsl
                                ? MailKit.Security.SecureSocketOptions.StartTls
                                : MailKit.Security.SecureSocketOptions.Auto,
                            Credentials = new NetworkCredential(
                                "mirkankacan@ideaktif.com.tr",
                                "Poj74043"),
                            Subject = new MessageTemplateTextFormatter(
                                "[{Level:u3}] Otomar.WebApi - {Message:lj}"),
                            Body = new MessageTemplateTextFormatter(GetErrorEmailHtmlTemplate()),
                            IsBodyHtml = true
                        },
                        restrictedToMinimumLevel: LogEventLevel.Error));
            }

            Log.Logger = loggerConfig.CreateLogger();

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

        private static string GetErrorEmailHtmlTemplate()
        {
            return """
                <!DOCTYPE html>
                <html>
                <head><meta charset="utf-8"/></head>
                <body style="margin:0;padding:0;background:#f2f2f2;font-family:'Segoe UI',Arial,sans-serif;">
                <div style="max-width:640px;margin:20px auto;background:#ffffff;border-radius:8px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,0.08);">

                  <!-- Header -->
                  <div style="background:#ED1D24;padding:20px 28px;">
                    <table width="100%" cellpadding="0" cellspacing="0"><tr>
                      <td><span style="color:#fff;font-size:18px;font-weight:700;">OTOMAR WebApi</span></td>
                      <td align="right"><span style="color:rgba(255,255,255,0.85);font-size:13px;">{Timestamp:dd.MM.yyyy HH:mm:ss}</span></td>
                    </tr></table>
                  </div>

                  <!-- Severity Badge -->
                  <div style="padding:24px 28px 0;">
                    <span style="display:inline-block;background:#DC3545;color:#fff;padding:4px 14px;border-radius:4px;font-size:12px;font-weight:700;letter-spacing:0.5px;">{Level:u3}</span>
                    <span style="display:inline-block;background:#f0f0f0;color:#555;padding:4px 14px;border-radius:4px;font-size:12px;margin-left:6px;">{Environment}</span>
                  </div>

                  <!-- Error Details -->
                  <div style="padding:20px 28px;">
                    <table width="100%" cellpadding="0" cellspacing="0" style="font-size:14px;color:#333;">
                      <tr>
                        <td style="padding:10px 0;color:#888;width:110px;vertical-align:top;border-bottom:1px solid #f0f0f0;">Sunucu</td>
                        <td style="padding:10px 0;border-bottom:1px solid #f0f0f0;font-weight:500;">{MachineName}</td>
                      </tr>
                      <tr>
                        <td style="padding:10px 0;color:#888;vertical-align:top;border-bottom:1px solid #f0f0f0;">Thread</td>
                        <td style="padding:10px 0;border-bottom:1px solid #f0f0f0;">{ThreadId}</td>
                      </tr>
                      <tr>
                        <td style="padding:10px 0;color:#888;vertical-align:top;border-bottom:1px solid #f0f0f0;">IP Adresi</td>
                        <td style="padding:10px 0;border-bottom:1px solid #f0f0f0;">{ClientIP}</td>
                      </tr>
                    </table>
                  </div>

                  <!-- Error Message -->
                  <div style="padding:0 28px;">
                    <div style="font-size:12px;color:#888;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px;">Hata Mesaji</div>
                    <div style="background:#FFF5F5;border-left:4px solid #ED1D24;padding:14px 16px;border-radius:0 6px 6px 0;font-size:14px;color:#333;line-height:1.6;word-break:break-word;">
                      {Message:lj}
                    </div>
                  </div>

                  <!-- Exception / Stack Trace -->
                  <div style="padding:20px 28px;">
                    <div style="font-size:12px;color:#888;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px;">Stack Trace</div>
                    <pre style="background:#1e1e1e;color:#d4d4d4;padding:16px;border-radius:6px;font-size:12px;line-height:1.7;overflow-x:auto;white-space:pre-wrap;word-break:break-all;margin:0;">{Exception}</pre>
                  </div>

                  <!-- Properties -->
                  <div style="padding:0 28px 24px;">
                    <div style="font-size:12px;color:#888;font-weight:600;text-transform:uppercase;letter-spacing:0.5px;margin-bottom:8px;">Ek Bilgiler</div>
                    <div style="background:#f8f9fa;padding:14px 16px;border-radius:6px;font-size:13px;color:#555;line-height:1.6;word-break:break-word;">
                      {Properties:j}
                    </div>
                  </div>

                  <!-- Footer -->
                  <div style="background:#f8f9fa;padding:14px 28px;border-top:1px solid #eee;text-align:center;">
                    <span style="font-size:11px;color:#aaa;">Bu e-posta Otomar.WebApi Serilog tarafindan otomatik olarak gönderilmiştir.</span>
                  </div>

                </div>
                </body>
                </html>
                """;
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