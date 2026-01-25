using Microsoft.AspNetCore.Http;
using Otomar.Application.Contracts.Services;
using System.Security.Claims;

namespace Otomar.Persistance.Services
{
    public class IdentityService(IHttpContextAccessor accessor) : IIdentityService
    {
        public string? GetClientCode()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "ClientCode")?.Value ?? null;
        }

        public string GetUserEmail()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value!;
        }

        public string GetUserId()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value!;
        }

        public string GetUserFullName()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value!;
        }

        public bool IsUserPaymentExempt()
        {
            return bool.Parse(accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == "IsPaymentExempt")?.Value!);
        }

        public string GetUserPhoneNumber()
        {
            return accessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.MobilePhone)?.Value!;
        }
    }
}