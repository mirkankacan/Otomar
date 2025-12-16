using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Serilog.Context;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Otomar.WebApi.Middlewares
{
    public class GlobalExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(ILogger<GlobalExceptionMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await LogAndHandleExceptionAsync(context, ex);
            }
        }

        private async Task LogAndHandleExceptionAsync(HttpContext context, Exception exception)
        {
            var request = context.Request;

            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous";
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var userAgent = request.Headers["User-Agent"].ToString();
            var requestId = context.TraceIdentifier;
            var action = $"{request.Method} {request.Path}";
            var endpoint = context.GetEndpoint();
            var controllerName = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerName ?? "Unknown";
            var actionName = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>()?.ActionName ?? "Unknown";
            var module = $"{controllerName}Controller";

            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("Action", action))
            using (LogContext.PushProperty("Module", module))
            using (LogContext.PushProperty("ClientIP", clientIp))
            using (LogContext.PushProperty("UserAgent", userAgent))
            using (LogContext.PushProperty("RequestId", requestId))
            {
                _logger.LogError(exception,
                    "HTTP {Method} {Path} başarısız oldu. QueryString: {QueryString}",
                    request.Method,
                    request.Path,
                    request.QueryString.ToString());
            }

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