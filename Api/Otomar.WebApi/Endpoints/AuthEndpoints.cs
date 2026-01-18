using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Auth;
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
        }
    }
}