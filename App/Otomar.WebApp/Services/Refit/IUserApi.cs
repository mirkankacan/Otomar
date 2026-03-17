using Otomar.Shared.Dtos.User;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IUserApi
    {
        [Get("/api/user/profile")]
        Task<UserProfileDto> GetProfileAsync(CancellationToken cancellationToken = default);

        [Put("/api/user/profile")]
        Task UpdateProfileAsync([Body] UpdateUserProfileDto dto, CancellationToken cancellationToken = default);

        [Post("/api/user/change-password")]
        Task ChangePasswordAsync([Body] ChangePasswordDto dto, CancellationToken cancellationToken = default);
    }
}
