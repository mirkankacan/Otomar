namespace Otomar.Application.Contracts.Services
{
    public interface IEmailService
    {
        Task SendPaymentSuccessMailAsync(Guid orderId, CancellationToken cancellationToken);

        Task SendVirtualPosPaymentSuccessMailAsync(Guid orderId, CancellationToken cancellationToken);

        Task SendPaymentFailedMailAsync(CancellationToken cancellationToken);
    }
}