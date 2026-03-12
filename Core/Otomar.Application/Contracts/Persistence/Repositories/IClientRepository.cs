using Otomar.Shared.Dtos.Client;

namespace Otomar.Application.Contracts.Persistence.Repositories
{
    public interface IClientRepository
    {
        Task<ClientDto?> GetByCodeAsync(string clientCode);
        Task<ClientDto?> GetByTaxNumberAsync(string taxNumber);
        Task<ClientDto?> GetByTcNumberAsync(string tcNumber);
        Task<IEnumerable<TransactionDto>> GetTransactionsByCodeAsync(string clientCode);
        Task<IEnumerable<TransactionDto>> GetTransactionsByCodeAsync(string clientCode, int pageNumber, int pageSize);
    }
}
