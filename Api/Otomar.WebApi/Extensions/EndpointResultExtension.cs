using Otomar.Contracts.Common;
using System.Net;

namespace Otomar.WebApi.Extensions
{
    public static class EndpointResultExtension
    {
        public static IResult ToGenericResult<T>(this ServiceResult<T> serviceResult)
        {
            return serviceResult.StatusCode switch
            {
                HttpStatusCode.OK => Results.Ok(serviceResult.Data),
                HttpStatusCode.Created => Results.Created(serviceResult.UrlAsCreated, serviceResult.Data),
                HttpStatusCode.NotFound => Results.NotFound(serviceResult.Fail!),
                HttpStatusCode.BadRequest => Results.BadRequest(serviceResult.Fail!),
                HttpStatusCode.Unauthorized => Results.Unauthorized(),
                HttpStatusCode.Conflict => Results.Conflict(serviceResult.Fail!),
                _ => Results.Json(
                    serviceResult.Fail!,
                    statusCode: (int)serviceResult.StatusCode,
                    contentType: "application/problem+json"
                )
            };
        }

        public static IResult ToResult(this ServiceResult serviceResult)
        {
            return serviceResult.StatusCode switch
            {
                HttpStatusCode.NoContent => Results.NoContent(),
                HttpStatusCode.NotFound => Results.NotFound(serviceResult.Fail!),
                HttpStatusCode.BadRequest => Results.BadRequest(serviceResult.Fail!),
                HttpStatusCode.Unauthorized => Results.Unauthorized(),
                _ => Results.Json(
                    serviceResult.Fail!,
                    statusCode: (int)serviceResult.StatusCode,
                    contentType: "application/problem+json"
                )
            };
        }
    }
}