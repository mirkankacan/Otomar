using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Responses;
using System.Net;

namespace Otomar.WebApp.Extensions
{
    public static class ApiResponseExtensions
    {
        public static IActionResult ToActionResult<T>(this ApiResponse<T> response)
        {
            if (response.IsSuccess)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new NoContentResult();
                }

                return new OkObjectResult(response.Data);
            }

            // ErrorTitle ve ErrorDetail varsa direkt kullan, yoksa ErrorMessage'ı parse et
            string? title = response.ErrorTitle;
            string? detail = response.ErrorDetail;

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(detail) && !string.IsNullOrEmpty(response.ErrorMessage))
            {
                // ErrorMessage'ı parse et (format: "Title: Detail" veya sadece "Title" veya sadece "Detail")
                var parts = response.ErrorMessage.Split(new[] { ':' }, 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    title = parts[0];
                    detail = parts[1];
                }
                else
                {
                    // Sadece title veya detail var
                    title = response.ErrorMessage;
                }
            }

            return new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = title ?? "Bir hata oluştu",
                status = (int)response.StatusCode,
                detail = detail ?? response.ErrorMessage ?? "İstek başarısız oldu"
            })
            {
                StatusCode = (int)response.StatusCode
            };
        }

        public static IActionResult ToActionResult(this ApiResponse response)
        {
            if (response.IsSuccess)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                {
                    return new NoContentResult();
                }

                return new OkResult();
            }

            // ErrorTitle ve ErrorDetail varsa direkt kullan, yoksa ErrorMessage'ı parse et
            string? title = response.ErrorTitle;
            string? detail = response.ErrorDetail;

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(detail) && !string.IsNullOrEmpty(response.ErrorMessage))
            {
                // ErrorMessage'ı parse et (format: "Title: Detail" veya sadece "Title" veya sadece "Detail")
                var parts = response.ErrorMessage.Split(new[] { ':' }, 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    title = parts[0];
                    detail = parts[1];
                }
                else
                {
                    // Sadece title veya detail var
                    title = response.ErrorMessage;
                }
            }

            return new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = title ?? "Bir hata oluştu",
                status = (int)response.StatusCode,
                detail = detail ?? response.ErrorMessage ?? "İstek başarısız oldu"
            })
            {
                StatusCode = (int)response.StatusCode
            };
        }
    }
}