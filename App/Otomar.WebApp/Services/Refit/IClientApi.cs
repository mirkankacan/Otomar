using Otomar.WebApp.Dtos.Client;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IClientApi
    {
        [Get("/api/clients/code/{clientCode}")]
        Task<ClientDto> GetClientByCodeAsync(string clientCode, CancellationToken cancellationToken = default);

        [Get("/api/clients/taxNumber/{taxNumber}")]
        Task<ClientDto> GetClientByTaxTcNumberAsync(string taxNumber, CancellationToken cancellationToken = default);

        [Get("/api/clients/{clientCode}/transactions")]
        Task<IEnumerable<TransactionDto>> GetClientTransactionsByCodeAsync(string clientCode, CancellationToken cancellationToken = default);
    }
}
