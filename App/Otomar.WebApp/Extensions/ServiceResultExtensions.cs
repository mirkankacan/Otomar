using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Common;
using Refit;
using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace Otomar.WebApp.Extensions
{
    public static class ServiceResultExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// HttpClient yanıtını ServiceResult ile aynı formatta IActionResult'a çevirir (API: Ok/Created/ProblemDetails).
        /// </summary>
        public static async Task<IActionResult> ToActionResultAsync(this HttpResponseMessage response, CancellationToken cancellationToken = default)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NoContent || string.IsNullOrWhiteSpace(body))
                    return new NoContentResult();

                object? data = null;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    try { data = JsonSerializer.Deserialize<JsonElement>(body); }
                    catch { data = body; }
                }

                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var location = response.Headers.Location?.ToString();
                    return new CreatedResult(location ?? "", data);
                }

                return new OkObjectResult(data ?? body);
            }

            var title = response.ReasonPhrase ?? "Bir hata oluştu";
            var detail = body;
            var status = (int)response.StatusCode;
            try
            {
                var problem = JsonSerializer.Deserialize<JsonElement>(body);
                if (problem.TryGetProperty("title", out var t)) title = t.GetString() ?? title;
                if (problem.TryGetProperty("detail", out var d)) detail = d.GetString() ?? detail;
                if (problem.TryGetProperty("status", out var s) && s.TryGetInt32(out var st)) status = st;
            }
            catch { /* body olduğu gibi detail olarak kullanılır */ }

            return new ObjectResult(new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title = title,
                status = status,
                detail = detail
            })
            {
                StatusCode = status
            };
        }
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
            catch (OperationCanceledException)
            {
                return new StatusCodeResult(499);
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
            catch (OperationCanceledException)
            {
                return new StatusCodeResult(499);
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
