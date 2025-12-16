using System.Net;

namespace Otomar.WebApp.Responses
{
    public class ApiResponse<T>
    {
        public bool IsSuccess { get; set; }
        public T Data { get; set; }
        public string ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public static ApiResponse<T> Success(T data, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ApiResponse<T>
            {
                IsSuccess = true,
                Data = data,
                StatusCode = statusCode
            };
        }

        public static ApiResponse<T> Failure(string errorMessage, HttpStatusCode statusCode)
        {
            return new ApiResponse<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                StatusCode = statusCode
            };
        }
    }

    public class ApiResponse
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public static ApiResponse Success(HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new ApiResponse
            {
                IsSuccess = true,
                StatusCode = statusCode
            };
        }

        public static ApiResponse Failure(string errorMessage, HttpStatusCode statusCode)
        {
            return new ApiResponse
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                StatusCode = statusCode
            };
        }
    }
}