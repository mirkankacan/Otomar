using Dapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.ListSearch;
using Otomar.Application.Enums;
using Otomar.Persistance.Data;

namespace Otomar.Persistance.Services
{
    public class ListSearchService(ILogger<ListSearchService> logger, IAppDbContext context, IIdentityService identityService) : IListSearchService
    {
        public Task<ServiceResult<int>> CreateListSearchAnswerAsync(List<CreateListSearchAnswerDto> createListSearchAnswerDtos)
        {
            throw new NotImplementedException();
        }

        public async Task<ServiceResult<string>> CreateListSearchAsync(CreateListSearchDto createListSearchDto)
        {
            try
            {
                var requestNo = $"OTOMAR-{NewId.NextGuid().ToString().Substring(0, 8).ToUpper()}";

                var userId = identityService.GetUserId() ?? null;
                var query = @"
                INSERT INTO IdtListSearches (RequestNo, PhoneNumber, ChassisNumber, Email, Brand, Model, Year, Engine, License, Annotation, CreatedAt, UserId, Status)
                VALUES (@RequestNo, @NameSurname, @CompanyName, @PhoneNumber, @ChassisNumber, @Email, @Brand, @Model, @Year, @Engine, @License, @Annotation, @CreatedAt, @UserId, @Status);";

                var parameters = new DynamicParameters();
                parameters.Add("RequestNo", requestNo);
                parameters.Add("PhoneNumber", createListSearchDto.PhoneNumber);
                parameters.Add("ChassisNumber", createListSearchDto.ChassisNumber);
                parameters.Add("Email", createListSearchDto.Email);
                parameters.Add("Brand", createListSearchDto.Brand);
                parameters.Add("Model", createListSearchDto.Model);
                parameters.Add("Year", createListSearchDto.Year);
                parameters.Add("Engine", createListSearchDto.Engine);
                parameters.Add("License", createListSearchDto.License);
                parameters.Add("Annotation", createListSearchDto.Annotation);
                parameters.Add("CreatedAt", DateTime.Now);
                parameters.Add("UserId", userId);
                parameters.Add("Status", ListSearchStatus.NotAnswered);

                await context.Connection.ExecuteAsync(query, parameters);

                logger.LogInformation($"{requestNo} istek numaralı liste araması oluşturuldu");

                return ServiceResult<string>.SuccessAsCreated(requestNo, $"/api/listsearches/{requestNo}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, message: "CreateListSearchAsync işleminde hata");
                throw;
            }
        }

        public Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByRequestNoAsync(string requestNo)
        {
            throw new NotImplementedException();
        }

        public Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByUserAsync(string userId)
        {
            throw new NotImplementedException();
        }
    }
}