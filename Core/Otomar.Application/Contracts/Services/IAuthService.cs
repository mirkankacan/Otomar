using Otomar.Application.Common;
using Otomar.Application.Dtos.Auth;

namespace Otomar.Application.Contracts.Services
{
    public interface IAuthService
    {
        Task<ServiceResult<TokenDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default);

        Task<ServiceResult<TokenDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> LogoutAsync(CancellationToken cancellationToken = default);

        Task<ServiceResult<TokenDto>> RefreshTokenAsync(CreateTokenByRefreshTokenDto createTokenByRefreshTokenDto, CancellationToken cancellationToken = default);

        Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken = default);
    }
}