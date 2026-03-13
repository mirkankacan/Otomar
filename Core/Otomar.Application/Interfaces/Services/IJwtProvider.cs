using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Auth;
using Otomar.Domain.Entities;

namespace Otomar.Application.Interfaces.Services
{
    public interface IJwtProvider
    {
        Task<ServiceResult<TokenDto>> CreateTokenAsync(ApplicationUser applicationUser);
    }
}