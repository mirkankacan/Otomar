using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.User;
using Otomar.Shared.Interfaces;
using System.Net;

namespace Otomar.Application.Services
{
    public class UserService(IUserRepository userRepository, IIdentityService identityService) : IUserService
    {
        public async Task<ServiceResult<UserProfileDto>> GetProfileAsync(CancellationToken cancellationToken = default)
        {
            var userId = identityService.GetUserId();
            var user = await userRepository.GetByIdAsync(userId);

            if (user == null)
                return ServiceResult<UserProfileDto>.Error("Kullanıcı Bulunamadı", HttpStatusCode.NotFound);

            return ServiceResult<UserProfileDto>.SuccessAsOk(new UserProfileDto
            {
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber
            });
        }

        public async Task<ServiceResult> UpdateProfileAsync(UpdateUserProfileDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Email))
            {
                return ServiceResult.Error(
                    "Geçersiz Bilgi",
                    "Ad ve e-posta alanları zorunludur.",
                    HttpStatusCode.BadRequest);
            }

            var userId = identityService.GetUserId();

            if (await userRepository.IsEmailInUseAsync(dto.Email, userId))
            {
                return ServiceResult.Error(
                    "E-posta Kullanımda",
                    "Bu e-posta adresi başka bir hesap tarafından kullanılıyor.",
                    HttpStatusCode.Conflict);
            }

            return await userRepository.UpdateProfileAsync(userId, dto);
        }

        public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return ServiceResult.Error(
                    "Geçersiz Bilgi",
                    "Mevcut şifre ve yeni şifre zorunludur.",
                    HttpStatusCode.BadRequest);
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return ServiceResult.Error(
                    "Şifre Uyuşmazlığı",
                    "Yeni şifre ve şifre onayı uyuşmuyor.",
                    HttpStatusCode.BadRequest);
            }

            var userId = identityService.GetUserId();
            return await userRepository.ChangePasswordAsync(userId, dto);
        }
    }
}
