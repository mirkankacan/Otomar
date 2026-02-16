namespace Otomar.WebApp.Services.Interfaces
{
    public interface IIdentityService
    {
        string GetUserId();

        string GetUserEmail();

        string GetUserFullName();

        bool IsUserPaymentExempt();

        string GetUserPhoneNumber();

        string? GetClientCode();
    }
}
