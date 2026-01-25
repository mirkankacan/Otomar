using Otomar.WebApp.Dtos.Auth;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IAuthApi
    {
        [Post("/api/auth/login")]
        Task<TokenDto> LoginAsync([Body] LoginDto dto, CancellationToken cancellationToken = default);

        [Post("/api/auth/register")]
        Task<TokenDto> RegisterAsync([Body] RegisterDto dto, CancellationToken cancellationToken = default);

        [Post("/api/auth/logout")]
        Task LogoutAsync(CancellationToken cancellationToken = default);

        [Post("/api/auth/refresh-token")]
        Task<TokenDto> RefreshTokenAsync([Body] CreateTokenByRefreshTokenDto dto, CancellationToken cancellationToken = default);

        [Post("/api/auth/reset-password")]
        Task ResetPasswordAsync([Body] ResetPasswordDto dto, CancellationToken cancellationToken = default);
    }
}