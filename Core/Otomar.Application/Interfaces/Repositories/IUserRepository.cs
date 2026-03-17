using Otomar.Domain.Entities;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.User;

namespace Otomar.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task<PanelKullanici?> GetPanelUserByUsernameAsync(string username);
        Task<ApplicationUser?> GetByIdAsync(string userId);
        Task<ApplicationUser?> GetByEmailAsync(string email);
        Task<bool> IsEmailInUseAsync(string email, string excludeUserId);
        Task<ServiceResult> UpdateProfileAsync(string userId, UpdateUserProfileDto dto);
        Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto);
    }
}
