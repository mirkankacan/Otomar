using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Helpers;
using Serilog.Context;
using System.Net;
using System.Text.Json;

namespace Otomar.WebApi.Middlewares
{
    public class GlobalExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IIdentityService _identityService;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger, IHttpContextAccessor httpContextAccessor, IIdentityService identityService)
        {
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _identityService = identityService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            SetLogContext(context);

            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await LogAndHandleExceptionAsync(context, ex);
            }
        }

        private void SetLogContext(HttpContext context)
        {
            var request = context.Request;

            var clientCode = _identityService.GetClientCode() ?? null;
            var userId = _identityService.GetUserId() ?? null;
            var clientIp = IpHelper.GetClientIp(_httpContextAccessor);
            var userAgent = IpHelper.GetUserAgent(_httpContextAccessor);
            var requestId = context.TraceIdentifier;
            var action = $"{request.Method} {request.Path}";

            var endpoint = context.GetEndpoint();
            var endpointName = endpoint?.DisplayName ?? "Unknown";

            var module = ExtractModuleName(endpointName, request.Path);

            LogContext.PushProperty("UserId", userId);
            LogContext.PushProperty("ClientCode", clientCode);
            LogContext.PushProperty("Action", action);
            LogContext.PushProperty("Module", module);
            LogContext.PushProperty("ClientIP", clientIp);
            LogContext.PushProperty("UserAgent", userAgent);
            LogContext.PushProperty("RequestId", requestId);
        }

        private string ExtractModuleName(string endpointName, PathString path)
        {
            if (endpointName.Contains("=>"))
            {
                var parts = endpointName.Split("=>");
                if (parts.Length > 1)
                {
                    return parts[1].Trim();
                }
            }

            var pathSegments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments?.Length >= 2)
            {
                return char.ToUpper(pathSegments[1][0]) + pathSegments[1].Substring(1);
            }

            return "UnknownModule";
        }

        private async Task LogAndHandleExceptionAsync(HttpContext context, Exception exception)
        {
            var request = context.Request;

            // LogContext zaten SetLogContext() tarafından set edildi
            _logger.LogError(exception,
                "HTTP {Method} {Path} başarısız oldu. QueryString: {QueryString}",
                request.Method,
                request.Path,
                request.QueryString.ToString());

            await HandleExceptionAsync(context, exception);
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var problemDetails = exception switch
            {
                ValidationException validationException => CreateValidationProblemDetails(context, validationException),
                UnauthorizedAccessException => CreateUnauthorizedProblemDetails(context),
                KeyNotFoundException => CreateNotFoundProblemDetails(context, exception),
                ArgumentNullException => CreateBadRequestProblemDetails(context, exception),
                ArgumentException => CreateBadRequestProblemDetails(context, exception),
                InvalidOperationException => CreateBadRequestProblemDetails(context, exception),
                NotImplementedException => CreateNotImplementedProblemDetails(context),
                TimeoutException => CreateTimeoutProblemDetails(context),
                OperationCanceledException => CreateRequestCancelledProblemDetails(context),
                _ => CreateInternalServerErrorProblemDetails(context)
            };

            context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(problemDetails, options);
            await context.Response.WriteAsync(json);
        }

        private static ProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationException exception)
        {
            var errors = exception.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );

            return new ProblemDetails
            {
                Title = "Doğrulama Hatası",
                Detail = "Bir veya daha fazla doğrulama hatası oluştu",
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path,
                Extensions = { ["errors"] = errors }
            };
        }

        private static ProblemDetails CreateUnauthorizedProblemDetails(HttpContext context)
        {
            return new ProblemDetails
            {
                Title = "Yetkisiz Erişim",
                Detail = "Bu kaynağa erişim yetkiniz yok",
                Status = (int)HttpStatusCode.Unauthorized,
                Instance = context.Request.Path
            };
        }

        private static ProblemDetails CreateNotFoundProblemDetails(HttpContext context, Exception exception)
        {
            return new ProblemDetails
            {
                Title = "Kayıt Bulunamadı",
                Detail = exception.Message,
                Status = (int)HttpStatusCode.NotFound,
                Instance = context.Request.Path
            };
        }

        private static ProblemDetails CreateBadRequestProblemDetails(HttpContext context, Exception exception)
        {
            return new ProblemDetails
            {
                Title = "Geçersiz İstek",
                Detail = exception.Message,
                Status = (int)HttpStatusCode.BadRequest,
                Instance = context.Request.Path
            };
        }

        private static ProblemDetails CreateNotImplementedProblemDetails(HttpContext context)
        {
            return new ProblemDetails
            {
                Title = "Özellik Uygulanmadı",
                Detail = "Bu özellik henüz uygulanmamıştır",
                Status = (int)HttpStatusCode.NotImplemented,
                Instance = context.Request.Path
            };
        }

        private static ProblemDetails CreateTimeoutProblemDetails(HttpContext context)
        {
            return new ProblemDetails
            {
                Title = "İstek Zaman Aşımına Uğradı",
                Detail = "İşlem çok uzun sürdü ve zaman aşımına uğradı",
                Status = (int)HttpStatusCode.RequestTimeout,
                Instance = context.Request.Path
            };
        }

        private static ProblemDetails CreateRequestCancelledProblemDetails(HttpContext context)
        {
            return new ProblemDetails
            {
                Title = "İstek İptal Edildi",
                Detail = "İstek kullanıcı tarafından iptal edildi",
                Status = 499,
                Instance = context.Request.Path
            };
        }

        private static ProblemDetails CreateInternalServerErrorProblemDetails(HttpContext context)
        {
            return new ProblemDetails
            {
                Title = "Sunucu Hatası",
                Detail = "Beklenmeyen bir hata oluştu",
                Status = (int)HttpStatusCode.InternalServerError,
                Instance = context.Request.Path
            };
        }
    }
}