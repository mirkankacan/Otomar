using Microsoft.Extensions.Logging;
using Otomar.Shared.Common;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Dtos.Client;
using Otomar.Application.Interfaces.Repositories;
using System.Net;

namespace Otomar.Application.Services
{
    public class ClientService(IClientRepository clientRepository, ILogger<ClientService> logger) : IClientService
    {
        public async Task<ServiceResult<ClientDto>> GetClientByCodeAsync(string clientCode)
        {
            if (string.IsNullOrEmpty(clientCode))
            {
                return ServiceResult<ClientDto>.Error("Geçersiz Cari Kodu", "Cari kodu boş geçilemez", HttpStatusCode.BadRequest);
            }

            var result = await clientRepository.GetByCodeAsync(clientCode.Trim());
            if (result == null)
            {
                logger.LogWarning($"{clientCode} kodlu cari bulunamadı");
                return ServiceResult<ClientDto>.Error("Cari Bulunamadı", $"{clientCode} kodlu cari bulunamadı", HttpStatusCode.NotFound);
            }
            return ServiceResult<ClientDto>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<ClientDto>> GetClientByTaxTcNumberAsync(string taxNumber)
        {
            if (string.IsNullOrEmpty(taxNumber))
            {
                return ServiceResult<ClientDto>.Error("Geçersiz Vergi/TC Numarası", "Vergi numarası/TC kimlik numarası boş geçilemez", HttpStatusCode.BadRequest);
            }

            ClientDto? result = null;
            if (taxNumber.Length == 10)
            {
                result = await clientRepository.GetByTaxNumberAsync(taxNumber.Trim());
            }
            else if (taxNumber.Length == 11)
            {
                result = await clientRepository.GetByTcNumberAsync(taxNumber.Trim());
            }

            if (result == null)
            {
                logger.LogWarning($"{taxNumber} vergi numaralı/TC kimlik numaralı cari bulunamadı");
                return ServiceResult<ClientDto>.Error("Cari Bulunamadı", $"{taxNumber} vergi numaralı/TC kimlik numaralı cari bulunamadı", HttpStatusCode.NotFound);
            }
            return ServiceResult<ClientDto>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<IEnumerable<TransactionDto>>> GetClientTransactionsByCodeAsync(string clientCode)
        {
            if (string.IsNullOrEmpty(clientCode))
            {
                return ServiceResult<IEnumerable<TransactionDto>>.Error("Geçersiz Cari Kodu", "Cari kodu boş geçilemez", HttpStatusCode.BadRequest);
            }

            var result = await clientRepository.GetTransactionsByCodeAsync(clientCode.Trim());
            if (!result.Any())
            {
                logger.LogWarning($"{clientCode} kodlu carinin hareketleri bulunamadı");
                return ServiceResult<IEnumerable<TransactionDto>>.Error("Cari Hareket Bulunamadı", $"{clientCode} kodlu carinin hareketleri bulunamadı", HttpStatusCode.NotFound);
            }
            return ServiceResult<IEnumerable<TransactionDto>>.SuccessAsOk(result);
        }

        public async Task<ServiceResult<IEnumerable<TransactionDto>>> GetClientTransactionsByCodeAsync(string clientCode, int pageNumber, int pageSize)
        {
            if (string.IsNullOrEmpty(clientCode))
            {
                return ServiceResult<IEnumerable<TransactionDto>>.Error("Geçersiz Cari Kodu", "Cari kodu boş geçilemez", HttpStatusCode.BadRequest);
            }
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var result = await clientRepository.GetTransactionsByCodeAsync(clientCode.Trim(), pageNumber, pageSize);
            if (!result.Any())
            {
                logger.LogWarning($"{clientCode} kodlu carinin hareketleri bulunamadı");
                return ServiceResult<IEnumerable<TransactionDto>>.Error("Cari Hareket Bulunamadı", $"{clientCode} kodlu carinin hareketleri bulunamadı", HttpStatusCode.NotFound);
            }
            return ServiceResult<IEnumerable<TransactionDto>>.SuccessAsOk(result);
        }
    }
}
