using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Contracts.Dtos.Auth;
using Otomar.WebApi.Extensions;

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
            .AllowAnonymous();

            group.MapPost("/register", async ([FromBody] RegisterDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.RegisterAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("Register")
            .AllowAnonymous();

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
            .AllowAnonymous();

            group.MapPost("/reset-password", async ([FromBody] ResetPasswordDto dto, [FromServices] IAuthService authService, CancellationToken cancellationToken) =>
            {
                var result = await authService.ResetPasswordAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("ResetPassword")
            .AllowAnonymous();
        }
    }
}