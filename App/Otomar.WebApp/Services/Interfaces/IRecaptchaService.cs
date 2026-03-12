namespace Otomar.WebApp.Services.Interfaces
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(
            string? token,
            string expectedAction,
            string? remoteIp = null,
            CancellationToken cancellationToken = default);
    }
}
