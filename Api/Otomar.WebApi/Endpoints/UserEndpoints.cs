using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Dtos.User;
using Otomar.WebApi.Extensions;
using static Otomar.WebApi.Extensions.RateLimitingRegistration;

namespace Otomar.WebApi.Endpoints
{
    public class UserEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/user")
                .WithTags("User")
                .RequireAuthorization();

            group.MapGet("/profile", async ([FromServices] IUserService userService, CancellationToken cancellationToken) =>
            {
                var result = await userService.GetProfileAsync(cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("GetUserProfile");

            group.MapPut("/profile", async ([FromBody] UpdateUserProfileDto dto, [FromServices] IUserService userService, CancellationToken cancellationToken) =>
            {
                var result = await userService.UpdateProfileAsync(dto, cancellationToken);
                return result.ToResult();
            })
            .WithName("UpdateUserProfile");

            group.MapPost("/change-password", async ([FromBody] ChangePasswordDto dto, [FromServices] IUserService userService, CancellationToken cancellationToken) =>
            {
                var result = await userService.ChangePasswordAsync(dto, cancellationToken);
                return result.ToResult();
            })
            .WithName("ChangeUserPassword")
            .RequireRateLimiting(Policies.ChangePassword);
        }
    }
}
