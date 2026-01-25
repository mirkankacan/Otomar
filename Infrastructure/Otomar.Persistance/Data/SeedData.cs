using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Otomar.Domain.Entities;

namespace Otomar.Persistance.Data
{
    public static class SeedData
    {
        public static async Task SeedAsync(
            IdentityDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            await SeedRolesAsync(roleManager);
            await SeedUsersAsync(userManager);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
        {
            var roles = new[]
            {
                new ApplicationRole { Name = "Admin", NormalizedName = "ADMIN" },
                new ApplicationRole { Name = "User", NormalizedName = "USER" },
                new ApplicationRole { Name = "Manager", NormalizedName = "MANAGER" }
            };

            foreach (var role in roles)
            {
                var existingRole = await roleManager.FindByNameAsync(role.Name!);
                if (existingRole == null)
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
        {
            // Admin User
            var adminUser = new ApplicationUser
            {
                UserName = "admin@otomar.com",
                Email = "admin@otomar.com",
                EmailConfirmed = true,
                Name = "Admin",
                Surname = "User",
                PhoneNumber = "+905551234567",
                IsActive = true,
                IsPaymentExempt = true,
                CommerceMail = true,
                ClarificationAgreement = true,
                MembershipAgreement = true,
                ClientCode = "ADMIN001"
            };

            var existingAdmin = await userManager.FindByEmailAsync(adminUser.Email!);
            if (existingAdmin == null)
            {
                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Test User 1
            var testUser1 = new ApplicationUser
            {
                UserName = "test@otomar.com",
                Email = "test@otomar.com",
                EmailConfirmed = true,
                Name = "Test",
                Surname = "User",
                PhoneNumber = "+905559876543",
                IsActive = true,
                IsPaymentExempt = false,
                CommerceMail = true,
                ClarificationAgreement = true,
                MembershipAgreement = true,
                ClientCode = "CLIENT001"
            };

            var existingTestUser1 = await userManager.FindByEmailAsync(testUser1.Email!);
            if (existingTestUser1 == null)
            {
                var result = await userManager.CreateAsync(testUser1, "Test123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser1, "User");
                }
            }

            // Test User 2
            var testUser2 = new ApplicationUser
            {
                UserName = "manager@otomar.com",
                Email = "manager@otomar.com",
                EmailConfirmed = true,
                Name = "Manager",
                Surname = "User",
                PhoneNumber = "+905551112233",
                IsActive = true,
                IsPaymentExempt = false,
                CommerceMail = true,
                ClarificationAgreement = true,
                MembershipAgreement = true,
                ClientCode = "CLIENT002"
            };

            var existingTestUser2 = await userManager.FindByEmailAsync(testUser2.Email!);
            if (existingTestUser2 == null)
            {
                var result = await userManager.CreateAsync(testUser2, "Manager123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser2, "Manager");
                }
            }

            // Inactive User (for testing)
            var inactiveUser = new ApplicationUser
            {
                UserName = "inactive@otomar.com",
                Email = "inactive@otomar.com",
                EmailConfirmed = true,
                Name = "Inactive",
                Surname = "User",
                PhoneNumber = "+905554445566",
                IsActive = false,
                IsPaymentExempt = false,
                CommerceMail = false,
                ClarificationAgreement = false,
                MembershipAgreement = false,
                ClientCode = "CLIENT003"
            };

            var existingInactiveUser = await userManager.FindByEmailAsync(inactiveUser.Email!);
            if (existingInactiveUser == null)
            {
                var result = await userManager.CreateAsync(inactiveUser, "Inactive123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(inactiveUser, "User");
                }
            }
        }
    }
}
