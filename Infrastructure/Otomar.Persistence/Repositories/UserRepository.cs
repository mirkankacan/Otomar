using Dapper;
using Microsoft.AspNetCore.Identity;
using Otomar.Application.Interfaces;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Domain.Entities;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.User;
using System.Net;

namespace Otomar.Persistence.Repositories
{
    public class UserRepository(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager) : IUserRepository
    {
        public async Task<PanelKullanici?> GetPanelUserByUsernameAsync(string username)
        {
            return await unitOfWork.Connection.QueryFirstOrDefaultAsync<PanelKullanici>("""
                SELECT TOP 1
                    LTRIM(RTRIM(KULLANICI_ADI)) AS KullaniciAdi,
                    SIFRE AS Sifre,
                    CARI_ISIM AS CariIsim,
                    CARI_KOD AS CariKod
                FROM IDV_WEB_PANEL_KULLANICI
                WHERE LTRIM(RTRIM(KULLANICI_ADI)) = @Username
                """, new { Username = username.Trim() });
        }

        public async Task<ApplicationUser?> GetByIdAsync(string userId)
        {
            return await userManager.FindByIdAsync(userId);
        }

        public async Task<ApplicationUser?> GetByEmailAsync(string email)
        {
            return await userManager.FindByEmailAsync(email);
        }

        public async Task<bool> IsEmailInUseAsync(string email, string excludeUserId)
        {
            var existing = await userManager.FindByEmailAsync(email);
            return existing != null && existing.Id != excludeUserId;
        }

        public async Task<ServiceResult> UpdateProfileAsync(string userId, UpdateUserProfileDto dto)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.ErrorAsNotFound();

            user.Name = dto.Name;
            user.Surname = dto.Surname;
            user.PhoneNumber = dto.PhoneNumber;

            if (!string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase))
            {
                var setEmailResult = await userManager.SetEmailAsync(user, dto.Email);
                if (!setEmailResult.Succeeded)
                {
                    return ServiceResult.Error(
                        "E-posta Güncellenemedi",
                        string.Join(", ", setEmailResult.Errors.Select(e => e.Description)),
                        HttpStatusCode.BadRequest);
                }

                await userManager.SetUserNameAsync(user, dto.Email);
            }

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult.Error(
                    "Profil Güncellenemedi",
                    string.Join(", ", updateResult.Errors.Select(e => e.Description)),
                    HttpStatusCode.BadRequest);
            }

            return ServiceResult.SuccessAsNoContent();
        }

        public async Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return ServiceResult.ErrorAsNotFound();

            var result = await userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            if (!result.Succeeded)
            {
                return ServiceResult.Error(
                    "Şifre Değiştirilemedi",
                    string.Join(", ", result.Errors.Select(e => e.Description)),
                    HttpStatusCode.BadRequest);
            }

            return ServiceResult.SuccessAsNoContent();
        }
    }
}
