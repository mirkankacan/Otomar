using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Common;
using Otomar.WebApp.Responses;
using Refit;
using System.Net;
using System.Net.Http;

namespace Otomar.WebApp.Extensions
{
    public static class ServiceResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ServiceResult<T> serviceResult)
        {
            if (serviceResult.IsSuccess)
            {
                if (serviceResult.StatusCode == HttpStatusCode.NoContent)
                {
                    return new NoContentResult();
                }

                return new OkObjectResult(serviceResult.Data);
            }

            var title = serviceResult.Fail?.Title ?? "Bir hata oluştu";
            var detail = serviceResult.Fail?.Detail ?? string.Empty;

            return new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = title,
                status = (int)serviceResult.StatusCode,
                detail = detail
            })
            {
                StatusCode = (int)serviceResult.StatusCode
            };
        }

        public static IActionResult ToActionResult(this ServiceResult serviceResult)
        {
            if (serviceResult.IsSuccess)
            {
                if (serviceResult.StatusCode == HttpStatusCode.NoContent)
                {
                    return new NoContentResult();
                }

                return new OkResult();
            }

            var title = serviceResult.Fail?.Title ?? "Bir hata oluştu";
            var detail = serviceResult.Fail?.Detail ?? string.Empty;

            return new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = title,
                status = (int)serviceResult.StatusCode,
                detail = detail
            })
            {
                StatusCode = (int)serviceResult.StatusCode
            };
        }

        public static async Task<IActionResult> ToActionResultAsync<T>(this Task<T> task)
        {
            try
            {
                var data = await task;
                return new OkObjectResult(data);
            }
            catch (ApiException ex)
            {
                var statusCode = (HttpStatusCode)ex.StatusCode;
                var problemDetails = ex.Content;
                
                return new ObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = ex.ReasonPhrase ?? "Bir hata oluştu",
                    status = (int)statusCode,
                    detail = problemDetails ?? ex.Message
                })
                {
                    StatusCode = (int)statusCode
                };
            }
        }

        public static async Task<IActionResult> ToActionResultAsync(this Task task)
        {
            try
            {
                await task;
                return new OkResult();
            }
            catch (ApiException ex)
            {
                var statusCode = (HttpStatusCode)ex.StatusCode;
                var problemDetails = ex.Content;
                
                return new ObjectResult(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = ex.ReasonPhrase ?? "Bir hata oluştu",
                    status = (int)statusCode,
                    detail = problemDetails ?? ex.Message
                })
                {
                    StatusCode = (int)statusCode
                };
            }
        }
    }
}
