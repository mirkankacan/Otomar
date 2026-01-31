using Otomar.Application.Common;
using Otomar.Application.Dtos.Client;

namespace Otomar.Application.Contracts.Services
{
    public interface IClientService
    {
        Task<ServiceResult<IEnumerable<TransactionDto>>> GetClientTransactionsByCodeAsync(string clientCode);

        Task<ServiceResult<ClientDto>> GetClientByTaxTcNumberAsync(string taxNumber);

        Task<ServiceResult<ClientDto>> GetClientByCodeAsync(string clientCode);
    }
}