namespace Otomar.Application.Contracts.Services
{
    public interface IEmailService
    {
        Task SendPaymentSuccessMailAsync(Guid orderId, CancellationToken cancellationToken);

        Task SendListSearchMailAsync(Guid listSearchId, CancellationToken cancellationToken);

        Task SendVirtualPosPaymentSuccessMailAsync(Guid orderId, CancellationToken cancellationToken);

        Task SendClientOrderMailAsync(Guid orderId, CancellationToken cancellationToken);

        Task SendPaymentFailedMailAsync(CancellationToken cancellationToken);
    }
}