using Dapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Application.Common;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.ListSearch;
using Otomar.Domain.Enums;
using Otomar.Persistance.Data;
using System.Net;

namespace Otomar.Persistance.Services
{
    public class ListSearchService(ILogger<ListSearchService> logger, IAppDbContext context, IIdentityService identityService, IFileService fileService) : IListSearchService
    {
        public async Task<ServiceResult<int>> CreateListSearchAnswerAsync(List<CreateListSearchAnswerDto> createListSearchAnswerDtos)
        {
            using var transaction = context.Connection.BeginTransaction();
            try
            {
                if (createListSearchAnswerDtos == null || !createListSearchAnswerDtos.Any())
                {
                    return ServiceResult<int>.Error("Cevap Listesi Boş", "Cevap listesi boş geçilemez", HttpStatusCode.BadRequest);
                }

                var userId = identityService.GetUserId();
                var updatedCount = 0;

                // Bu metod şu an için placeholder olarak bırakıldı
                // CreateListSearchAnswerDto'nun yapısı belirlendiğinde implementasyon tamamlanacak
                // Muhtemelen ListSearch'ün Status'unu Answered olarak güncellemek için kullanılacak

                transaction.Commit();
                logger.LogInformation($"{updatedCount} adet liste sorgu cevabı oluşturuldu");
                return ServiceResult<int>.SuccessAsCreated(updatedCount, $"/api/listsearches");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "CreateListSearchAnswerAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<string>> CreateListSearchAsync(CreateListSearchDto createListSearchDto, CancellationToken cancellationToken)
        {
            using var transaction = context.Connection.BeginTransaction();
            try
            {
                var requestNo = $"OTOMAR-{NewId.NextGuid().ToString().Substring(0, 8).ToUpper()}";
                var userId = identityService.GetUserId();

                var query = @"
            INSERT INTO IdtListSearches (Id, RequestNo,NameSurname, CompanyName, PhoneNumber, ChassisNumber, Email, Brand, Model, Year, Engine, LicensePlate, Note, CreatedAt, CreatedBy, Status)
            VALUES (@Id, @RequestNo, @NameSurname, @CompanyName,@PhoneNumber, @ChassisNumber, @Email, @Brand, @Model, @Year, @Engine, @LicensePlate, @Note, @CreatedAt, @CreatedBy, @Status);";

                var parameters = new DynamicParameters();
                var id = NewId.NextGuid();
                parameters.Add("Id", id);
                parameters.Add("RequestNo", requestNo);
                parameters.Add("NameSurname", createListSearchDto.NameSurname);
                parameters.Add("CompanyName", createListSearchDto.CompanyName);
                parameters.Add("PhoneNumber", createListSearchDto.PhoneNumber);
                parameters.Add("ChassisNumber", createListSearchDto.ChassisNumber);
                parameters.Add("Email", createListSearchDto.Email);
                parameters.Add("Brand", createListSearchDto.Brand);
                parameters.Add("Model", createListSearchDto.Model);
                parameters.Add("Year", createListSearchDto.Year);
                parameters.Add("Engine", createListSearchDto.Engine);
                parameters.Add("LicensePlate", createListSearchDto.LicensePlate);
                parameters.Add("Note", createListSearchDto.Note ?? null);
                parameters.Add("CreatedAt", DateTime.Now);
                parameters.Add("CreatedBy", userId);
                parameters.Add("Status", ListSearchStatus.NotAnswered);

                await context.Connection.ExecuteAsync(query, parameters, transaction);

                // Sadece parça tanımı ve adet dolu olan satırlar dataya eklenir
                var partsToAdd = (createListSearchDto.Parts ?? new List<CreateListSearchPartDto>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.Definition) && p.Quantity > 0)
                    .ToList();
                foreach (var part in partsToAdd)
                {
                    var partQuery = @"
                INSERT INTO IdtListSearchParts(ListSearchId, Definition, Quantity, PartImages)
                VALUES (@ListSearchId, @Definition, @Quantity, @PartImages);";

                    var partParameters = new DynamicParameters();
                    partParameters.Add("ListSearchId", id);
                    partParameters.Add("Definition", part.Definition);
                    partParameters.Add("Quantity", part.Quantity);
                    partParameters.Add("Note", part.Note ?? null);
                    string? imagePathsAsString = null;
                    if (part.PartImages != null)
                    {
                        var fileServiceResult = part.PartImages != null ? await fileService.UploadFileAsync(part.PartImages, FileType.ListSearch, requestNo, cancellationToken) : null;
                        imagePathsAsString = string.Join(",", fileServiceResult.Data);
                    }

                    partParameters.Add("PartImages", imagePathsAsString);

                    await context.Connection.ExecuteAsync(partQuery, partParameters, transaction);
                }

                transaction.Commit();
                logger.LogInformation($"{requestNo} istek numaralı liste araması oluşturuldu");
                return ServiceResult<string>.SuccessAsCreated(requestNo, $"/api/listsearches/{requestNo}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                logger.LogError(ex, "CreateListSearchAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ListSearchDto>> GetListSearchByIdAsync(Guid id)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("id", id);
                string query = @"
                SELECT TOP 1
                   ILS.*,
                    ILSP.Id as PartId,
                    ILSP.ListSearchId,
                    ILSP.Definition,
                    ILSP.Quantity,
                    ILSP.PartImages,
                    ILSP.Note as ItemNote
                FROM IdtListSearches ILS WITH (NOLOCK)
                INNER JOIN IdtListSearchParts ILSP ON ILS.Id = ILSP.ListSearchId
                WHERE ILS.Id = @id;";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{id} ID'li liste sorgusu bulunamadı");
                    return ServiceResult<ListSearchDto>.Error("Liste Sorgusu Bulunamadı", $"{id} ID'li liste sorgusu bulunamadı", HttpStatusCode.NotFound);
                }

                var firstRow = rowList.First();
                var listSearch = new ListSearchDto
                {
                    Id = firstRow.Id,
                    RequestNo = firstRow.RequestNo,
                    NameSurname = firstRow.NameSurname,
                    CompanyName = firstRow.CompanyName,
                    PhoneNumber = firstRow.PhoneNumber,
                    ChassisNumber = firstRow.ChassisNumber,
                    Email = firstRow.Email,
                    Brand = firstRow.Brand,
                    Model = firstRow.Model,
                    Year = firstRow.Year,
                    Engine = firstRow.Engine,
                    LicensePlate = firstRow.LicensePlate,
                    Note = firstRow.Note,
                    CreatedAt = firstRow.CreatedAt,
                    Status = (ListSearchStatus)firstRow.Status,
                    UpdatedAt = firstRow.UpdatedAt,
                    Parts = rowList
                        .Where(r => !Convert.IsDBNull(r.PartId) && r.PartId != null)
                        .Select(r => new ListSearchPartDto
                        {
                            Id = r.PartId,
                            ListSearchId = r.ListSearchId,
                            Definition = r.Definition,
                            Quantity = r.Quantity,
                            Note = r.ItemNote,
                            PartImages = string.IsNullOrEmpty(r.PartImages) ? new List<string>() : r.PartImages.Split(',').ToList()
                        })
                        .ToList()
                };
                return ServiceResult<ListSearchDto>.SuccessAsOk(listSearch);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetListSearchByIdAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ListSearchDto>> GetListSearchByRequestNoAsync(string requestNo)
        {
            try
            {
                if (string.IsNullOrEmpty(requestNo))
                {
                    return ServiceResult<ListSearchDto>.Error("İstek No Boş", "İstek numarası boş geçilemez", HttpStatusCode.BadRequest);
                }
                var parameters = new DynamicParameters();
                parameters.Add("requestNo", requestNo);
                string query = @"
                SELECT TOP 1
                   ILS.*,
                    ILSP.Id as PartId,
                    ILSP.ListSearchId,
                    ILSP.Definition,
                    ILSP.Quantity,
                    ILSP.PartImages,
                    ILSP.Note as ItemNote
                FROM IdtListSearches ILS WITH (NOLOCK)
                INNER JOIN IdtListSearchParts ILSP ON ILS.Id = ILSP.ListSearchId
                WHERE ILS.RequestNo = @requestNo;";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{requestNo} istek numaralı liste sorgusu bulunamadı");
                    return ServiceResult<ListSearchDto>.Error("Liste Sorgusu Bulunamadı", $"{requestNo} istek numaralı liste sorgusu bulunamadı", HttpStatusCode.NotFound);
                }

                var firstRow = rowList.First();
                var listSearch = new ListSearchDto
                {
                    Id = firstRow.Id,
                    RequestNo = firstRow.RequestNo,
                    NameSurname = firstRow.NameSurname,
                    CompanyName = firstRow.CompanyName,
                    PhoneNumber = firstRow.PhoneNumber,
                    ChassisNumber = firstRow.ChassisNumber,
                    Email = firstRow.Email,
                    Brand = firstRow.Brand,
                    Model = firstRow.Model,
                    Year = firstRow.Year,
                    Engine = firstRow.Engine,
                    LicensePlate = firstRow.LicensePlate,
                    Note = firstRow.Note,
                    CreatedAt = firstRow.CreatedAt,
                    Status = (ListSearchStatus)firstRow.Status,
                    UpdatedAt = firstRow.UpdatedAt,
                    Parts = rowList
                        .Where(r => !Convert.IsDBNull(r.PartId) && r.PartId != null)
                        .Select(r => new ListSearchPartDto
                        {
                            Id = r.PartId,
                            ListSearchId = r.ListSearchId,
                            Definition = r.Definition,
                            Quantity = r.Quantity,
                            PartImages = string.IsNullOrEmpty(r.PartImages) ? new List<string>() : r.PartImages.Split(',').ToList()
                        })
                        .ToList()
                };
                return ServiceResult<ListSearchDto>.SuccessAsOk(listSearch);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetListSearchByRequestNoAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync()
        {
            try
            {
                string query = @"
                SELECT
                    ILS.*,
                    ILSP.Id as PartId,
                    ILSP.ListSearchId,
                    ILSP.Definition,
                    ILSP.Quantity,
                    ILSP.PartImages,
                    ILSP.Note as ItemNote
                FROM IdtListSearches ILS WITH (NOLOCK)
                INNER JOIN IdtListSearchParts ILSP ON ILS.Id = ILSP.ListSearchId
                ORDER BY ILS.CreatedAt DESC;";

                var rows = await context.Connection.QueryAsync(query);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"Liste sorguları bulunamadı");
                    return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(Enumerable.Empty<ListSearchDto>());
                }

                var listSearchesDict = new Dictionary<Guid, ListSearchDto>();
                foreach (var row in rowList)
                {
                    if (!listSearchesDict.ContainsKey(row.Id))
                    {
                        var listSearch = new ListSearchDto
                        {
                            Id = row.Id,
                            RequestNo = row.RequestNo,
                            NameSurname = row.NameSurname,
                            CompanyName = row.CompanyName,
                            PhoneNumber = row.PhoneNumber,
                            ChassisNumber = row.ChassisNumber,
                            Email = row.Email,
                            Brand = row.Brand,
                            Model = row.Model,
                            Year = row.Year,
                            Engine = row.Engine,
                            LicensePlate = row.LicensePlate,
                            Note = row.Note,
                            CreatedAt = row.CreatedAt,
                            Status = (ListSearchStatus)row.Status,
                            UpdatedAt = row.UpdatedAt,
                            Parts = new List<ListSearchPartDto>()
                        };

                        listSearchesDict[row.Id] = listSearch;
                    }

                    if (!Convert.IsDBNull(row.PartId) && row.PartId != null)
                    {
                        ((List<ListSearchPartDto>)listSearchesDict[row.Id].Parts).Add(new ListSearchPartDto
                        {
                            Id = row.PartId,
                            ListSearchId = row.ListSearchId,
                            Definition = row.Definition,
                            Quantity = row.Quantity,
                            PartImages = string.IsNullOrEmpty(row.PartImages) ? new List<string>() : row.PartImages.Split(',').ToList()
                        });
                    }
                }

                var result = listSearchesDict.Values.ToList();
                return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetListSearchesAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return ServiceResult<IEnumerable<ListSearchDto>>.Error("Geçersiz Kullanıcı ID'si", "Kullanıcı ID'si boş geçilemez", HttpStatusCode.BadRequest);
                }

                var parameters = new DynamicParameters();
                parameters.Add("userId", userId);

                string query = @"
                SELECT
                    ILS.*,
                    ILSP.Id as PartId,
                    ILSP.ListSearchId,
                    ILSP.Definition,
                    ILSP.Quantity,
                    ILSP.PartImages,
                    ILSP.Note as ItemNote
                FROM IdtListSearches ILS WITH (NOLOCK)
                INNER JOIN IdtListSearchParts ILSP ON ILS.Id = ILSP.ListSearchId
                WHERE ILS.CreatedBy = @userId
                ORDER BY ILS.CreatedAt DESC;";

                var rows = await context.Connection.QueryAsync(query, parameters);
                var rowList = rows.ToList();

                if (!rowList.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının liste sorguları bulunamadı");
                    return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(Enumerable.Empty<ListSearchDto>());
                }

                var listSearchesDict = new Dictionary<Guid, ListSearchDto>();
                foreach (var row in rowList)
                {
                    if (!listSearchesDict.ContainsKey(row.Id))
                    {
                        var listSearch = new ListSearchDto
                        {
                            Id = row.Id,
                            RequestNo = row.RequestNo,
                            NameSurname = row.NameSurname,
                            CompanyName = row.CompanyName,
                            PhoneNumber = row.PhoneNumber,
                            ChassisNumber = row.ChassisNumber,
                            Email = row.Email,
                            Brand = row.Brand,
                            Model = row.Model,
                            Year = row.Year,
                            Engine = row.Engine,
                            LicensePlate = row.LicensePlate,
                            Note = row.Note,
                            CreatedAt = row.CreatedAt,
                            Status = (ListSearchStatus)row.Status,
                            UpdatedAt = row.UpdatedAt,
                            Parts = new List<ListSearchPartDto>()
                        };

                        listSearchesDict[row.Id] = listSearch;
                    }

                    if (!Convert.IsDBNull(row.PartId) && row.PartId != null)
                    {
                        ((List<ListSearchPartDto>)listSearchesDict[row.Id].Parts).Add(new ListSearchPartDto
                        {
                            Id = row.PartId,
                            ListSearchId = row.ListSearchId,
                            Definition = row.Definition,
                            Quantity = row.Quantity,
                            PartImages = string.IsNullOrEmpty(row.PartImages) ? new List<string>() : row.PartImages.Split(',').ToList()
                        });
                    }
                }

                var result = listSearchesDict.Values.ToList();
                return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetListSearchesByUserAsync işleminde hata");
                throw;
            }
        }
    }
}