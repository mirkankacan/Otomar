namespace Otomar.Application.Contracts.Services
{
    public interface IIdentityService
    {
        string? GetUserId();

        string? GetUserEmail();

        string? GetUserNameSurname();

        string? GetClientCode();
    }
}