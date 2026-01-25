using Otomar.Application.Common;
using Otomar.Application.Dtos.Auth;
using Otomar.Domain.Entities;

namespace Otomar.Application.Contracts.Providers
{
    public interface IJwtProvider
    {
        Task<ServiceResult<TokenDto>> CreateTokenAsync(ApplicationUser applicationUser);
    }
}