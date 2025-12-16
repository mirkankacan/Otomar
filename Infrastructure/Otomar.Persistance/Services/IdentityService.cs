using Microsoft.AspNetCore.Http;
using Otomar.Application.Contracts.Services;
using System.Security.Claims;

namespace Otomar.Persistance.Services
{
    public class IdentityService(IHttpContextAccessor accessor) : IIdentityService
    {
        public string? GetClientCode()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "client_code")?.Value ?? null;
        }

        public string? GetUserEmail()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value ?? null;
        }

        public string? GetUserId()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? null;
        }

        public string? GetUserNameSurname()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value ?? null;
        }
    }
}