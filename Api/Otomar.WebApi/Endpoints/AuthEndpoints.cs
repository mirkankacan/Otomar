using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Dtos.Auth;
using Otomar.WebApi.Extensions;
using static Otomar.WebApi.Extensions.RateLimitingRegistration;

namespace Otomar.WebApi.Endpoints
{
    public class AuthEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/auth")
                .WithTags("Auth");

            group.MapPost("/login", async ([FromBody] LoginDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.LoginAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("Login")
            .AllowAnonymous()
            .RequireRateLimiting(Policies.AuthStrict);

            group.MapPost("/register", async ([FromBody] RegisterDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.RegisterAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("Register")
            .AllowAnonymous()
            .RequireRateLimiting(Policies.AuthStrict);

            group.MapPost("/logout", async ([FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.LogoutAsync(cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("Logout")
            .RequireAuthorization();

            group.MapPost("/refresh-token", async ([FromBody] CreateTokenByRefreshTokenDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.RefreshTokenAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("RefreshToken")
            .AllowAnonymous()
            .RequireRateLimiting(Policies.AuthModerate);

            group.MapPost("/forgot-password", async ([FromBody] ForgotPasswordDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.ForgotPasswordAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("ForgotPassword")
            .AllowAnonymous()
            .RequireRateLimiting(Policies.AuthStrict);

            group.MapPost("/reset-password", async ([FromBody] ResetPasswordDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.ResetPasswordAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("ResetPassword")
            .AllowAnonymous()
            .RequireRateLimiting(Policies.AuthModerate);
        }
    }
}