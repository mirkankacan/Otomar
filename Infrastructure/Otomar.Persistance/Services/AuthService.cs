using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.Auth;

namespace Otomar.Persistance.Services
{
    public class AuthService : IAuthService
    {
        public Task<ServiceResult<TokenDto>> LoginAsync(LoginDto loginDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<bool>> LogoutAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<TokenDto>> RefreshTokenAsync(CreateTokenByRefreshTokenDto createTokenByRefreshTokenDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<TokenDto>> RegisterAsync(RegisterDto registerDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}