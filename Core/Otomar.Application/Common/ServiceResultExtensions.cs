using Otomar.Contracts.Common;
using Refit;
using System.Net;
using System.Text.Json;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Otomar.Application.Common
{
    public static class ServiceResultExtensions
    {
        public static ServiceResult ErrorFromProblemDetails(ApiException exception)
        {
            if (string.IsNullOrEmpty(exception.Content))
            {
                return new ServiceResult()
                {
                    Fail = new ProblemDetails()
                    {
                        Detail = exception.Message
                    },
                    StatusCode = exception.StatusCode
                };
            }
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(exception.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return new ServiceResult()
            {
                Fail = problemDetails,
                StatusCode = exception.StatusCode
            };
        }

        public static ServiceResult<T> ErrorFromProblemDetails<T>(ApiException exception)
        {
            if (string.IsNullOrEmpty(exception.Content))
            {
                return new ServiceResult<T>()
                {
                    Fail = new ProblemDetails()
                    {
                        Detail = exception.Message
                    },
                    StatusCode = exception.StatusCode
                };
            }
            var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(exception.Content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return new ServiceResult<T>()
            {
                Fail = problemDetails,
                StatusCode = exception.StatusCode
            };
        }
    }
}
