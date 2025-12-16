using Refit;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProblemDetails = Microsoft.AspNetCore.Mvc.ProblemDetails;

namespace Otomar.Application.Common
{
    public class ServiceResult
    {
        [JsonIgnore]
        public HttpStatusCode StatusCode { get; set; }

        public ProblemDetails? Fail { get; set; }

        [JsonIgnore]
        public bool IsSuccess => Fail is null;

        [JsonIgnore]
        public bool IsFail => !IsSuccess;

        public static ServiceResult SuccessAsNoContent()
        {
            return new ServiceResult { StatusCode = HttpStatusCode.NoContent };
        }

        public static ServiceResult ErrorAsNotFound()
        {
            return new ServiceResult
            {
                StatusCode = HttpStatusCode.NotFound,
                Fail = new ProblemDetails
                {
                    Title = "Not Found",
                    Detail = "The requested resource was not found"
                }
            };
        }

        public static ServiceResult ErrorFromProblemDetails(Refit.ApiException exception)
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

        public static ServiceResult Error(ProblemDetails problemDetails, HttpStatusCode statusCode)
        {
            return new ServiceResult
            {
                StatusCode = statusCode,
                Fail = problemDetails
            };
        }

        public static ServiceResult Error(string title, string description, HttpStatusCode statusCode)
        {
            return new ServiceResult
            {
                StatusCode = statusCode,
                Fail = new ProblemDetails
                {
                    Title = title,
                    Detail = description,
                    Status = (int)statusCode
                }
            };
        }

        public static ServiceResult Error(string title, HttpStatusCode statusCode)
        {
            return new ServiceResult
            {
                StatusCode = statusCode,
                Fail = new ProblemDetails
                {
                    Title = title,
                    Status = (int)statusCode
                }
            };
        }

        public static ServiceResult ErrorFromValidation(IDictionary<string, object> validationErrors)
        {
            return new ServiceResult
            {
                StatusCode = HttpStatusCode.BadRequest,
                Fail = new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = "Please check the errors property for more details",
                    Extensions = validationErrors,
                    Status = (int)HttpStatusCode.BadRequest
                }
            };
        }
    }

    public class ServiceResult<T> : ServiceResult
    {
        public T? Data { get; set; }

        [JsonIgnore]
        public string? UrlAsCreated { get; set; }

        public static ServiceResult<T> SuccessAsOk(T data)
        {
            return new ServiceResult<T> { StatusCode = HttpStatusCode.OK, Data = data };
        }

        public static ServiceResult<T> SuccessAsCreated(T data, string url)
        {
            return new ServiceResult<T> { StatusCode = HttpStatusCode.Created, Data = data, UrlAsCreated = url };
        }

        public new static ServiceResult<T> ErrorFromProblemDetails(ApiException exception)
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

        public new static ServiceResult<T> Error(ProblemDetails problemDetails, HttpStatusCode statusCode)
        {
            return new ServiceResult<T>
            {
                StatusCode = statusCode,
                Fail = problemDetails
            };
        }

        public new static ServiceResult<T> Error(string title, string description, HttpStatusCode statusCode)
        {
            return new ServiceResult<T>
            {
                StatusCode = statusCode,
                Fail = new ProblemDetails
                {
                    Title = title,
                    Detail = description,
                    Status = (int)statusCode
                }
            };
        }

        public new static ServiceResult<T> Error(string title, HttpStatusCode statusCode)
        {
            return new ServiceResult<T>
            {
                StatusCode = statusCode,
                Fail = new ProblemDetails
                {
                    Title = title,
                    Status = (int)statusCode
                }
            };
        }

        public new static ServiceResult<T> ErrorFromValidation(IDictionary<string, object> validationErrors)
        {
            return new ServiceResult<T>
            {
                StatusCode = HttpStatusCode.BadRequest,
                Fail = new ProblemDetails
                {
                    Title = "Validation Error",
                    Detail = "Please check the errors property for more details",
                    Extensions = validationErrors,
                    Status = (int)HttpStatusCode.BadRequest
                }
            };
        }
    }
}