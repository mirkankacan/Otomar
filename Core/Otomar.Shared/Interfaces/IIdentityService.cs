namespace Otomar.Shared.Interfaces
{
    /// <summary>
    /// Current user identity information from HttpContext claims.
    /// Implemented separately by WebApi (JWT) and WebApp (Cookie) layers.
    /// </summary>
    public interface IIdentityService
    {
        string GetUserId();

        string GetUserEmail();

        string GetUserFullName();

        bool IsUserPaymentExempt();

        string GetUserPhoneNumber();

        string? GetClientCode();

        string GetUserRole();
    }
}
