using Otomar.Shared.Common;
using Otomar.Shared.Dtos.User;

namespace Otomar.Application.Interfaces.Services
{
    public interface IUserService
    {
        Task<ServiceResult<UserProfileDto>> GetProfileAsync(CancellationToken cancellationToken = default);
        Task<ServiceResult> UpdateProfileAsync(UpdateUserProfileDto dto, CancellationToken cancellationToken = default);
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default);
    }
}
