using Otomar.Shared.Dtos.ListSearch;
using Otomar.Shared.Dtos.Order;
using Otomar.Shared.Dtos.Payment;

namespace Otomar.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendPaymentSuccessMailAsync(OrderDto order, CancellationToken cancellationToken);

        Task SendListSearchMailAsync(ListSearchDto listSearch, CancellationToken cancellationToken);

        Task SendListSearchAnsweredMailAsync(ListSearchDto listSearch, CancellationToken cancellationToken);

        Task SendVirtualPosPaymentSuccessMailAsync(OrderDto order, PaymentDto payment, CancellationToken cancellationToken);

        Task SendClientOrderMailAsync(ClientOrderDto order, CancellationToken cancellationToken);

        Task SendPaymentFailedMailAsync(OrderDto order, PaymentDto payment, CancellationToken cancellationToken);

        Task SendHealthAlertAsync(string checkName, string status, string? description, string? errorMessage, CancellationToken cancellationToken);
    }
}