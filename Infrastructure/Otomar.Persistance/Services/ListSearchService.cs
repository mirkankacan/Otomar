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
                logger.LogWarning(ex, "CreateListSearchAnswerAsync işleminde hata");
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
                INSERT INTO IdtListSearchParts(ListSearchId, Definition, Quantity, Note, PartImages)
                VALUES (@ListSearchId, @Definition, @Quantity,@Note, @PartImages);";

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
                logger.LogWarning(ex, "CreateListSearchAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ListSearchDto>> GetListSearchByIdAsync(Guid id)
        {
            try
            {
                var listSearchQuery = @"
                    SELECT *
                    FROM IdtListSearches WITH (NOLOCK)
                    WHERE Id = @id;";
                var listSearchRow = await context.Connection.QueryFirstOrDefaultAsync<dynamic>(listSearchQuery, new { id });

                if (listSearchRow == null)
                {
                    logger.LogWarning($"{id} ID'li liste sorgusu bulunamadı");
                    return ServiceResult<ListSearchDto>.Error("Liste Sorgusu Bulunamadı", $"{id} ID'li liste sorgusu bulunamadı", HttpStatusCode.NotFound);
                }

                var partsQuery = @"
                    SELECT Id as PartId, ListSearchId, Definition, Quantity, PartImages, Note as ItemNote
                    FROM IdtListSearchParts WITH (NOLOCK)
                    WHERE ListSearchId = @ListSearchId;";
                var parts = await context.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchId = id });
                var partsList = parts.ToList();

                var listSearch = new ListSearchDto
                {
                    Id = listSearchRow.Id,
                    RequestNo = listSearchRow.RequestNo,
                    NameSurname = listSearchRow.NameSurname,
                    CompanyName = listSearchRow.CompanyName,
                    PhoneNumber = listSearchRow.PhoneNumber,
                    ChassisNumber = listSearchRow.ChassisNumber,
                    Email = listSearchRow.Email,
                    Brand = listSearchRow.Brand,
                    Model = listSearchRow.Model,
                    Year = listSearchRow.Year,
                    Engine = listSearchRow.Engine,
                    LicensePlate = listSearchRow.LicensePlate,
                    Note = listSearchRow.Note,
                    CreatedAt = listSearchRow.CreatedAt,
                    Status = (ListSearchStatus)listSearchRow.Status,
                    UpdatedAt = listSearchRow.UpdatedAt,
                    Parts = partsList
                        .Select(p => new ListSearchPartDto
                        {
                            Id = p.PartId,
                            ListSearchId = p.ListSearchId,
                            Definition = p.Definition,
                            Quantity = p.Quantity,
                            Note = p.ItemNote,
                            PartImages = string.IsNullOrEmpty(p.PartImages as string)
                                ? new List<string>()
                                : new List<string>((p.PartImages as string)!.Split(',', StringSplitOptions.RemoveEmptyEntries))
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

                var listSearchQuery = @"
                    SELECT *
                    FROM IdtListSearches WITH (NOLOCK)
                    WHERE RequestNo = @requestNo;";
                var listSearchRow = await context.Connection.QueryFirstOrDefaultAsync<dynamic>(listSearchQuery, new { requestNo });

                if (listSearchRow == null)
                {
                    logger.LogWarning($"{requestNo} istek numaralı liste sorgusu bulunamadı");
                    return ServiceResult<ListSearchDto>.Error("Liste Sorgusu Bulunamadı", $"{requestNo} istek numaralı liste sorgusu bulunamadı", HttpStatusCode.NotFound);
                }

                var listSearchId = (Guid)listSearchRow.Id;
                var partsQuery = @"
                    SELECT Id as PartId, ListSearchId, Definition, Quantity, PartImages, Note as ItemNote
                    FROM IdtListSearchParts WITH (NOLOCK)
                    WHERE ListSearchId = @ListSearchId;";
                var parts = await context.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchId = listSearchId });
                var partsList = parts.ToList();

                var listSearch = new ListSearchDto
                {
                    Id = listSearchRow.Id,
                    RequestNo = listSearchRow.RequestNo,
                    NameSurname = listSearchRow.NameSurname,
                    CompanyName = listSearchRow.CompanyName,
                    PhoneNumber = listSearchRow.PhoneNumber,
                    ChassisNumber = listSearchRow.ChassisNumber,
                    Email = listSearchRow.Email,
                    Brand = listSearchRow.Brand,
                    Model = listSearchRow.Model,
                    Year = listSearchRow.Year,
                    Engine = listSearchRow.Engine,
                    LicensePlate = listSearchRow.LicensePlate,
                    Note = listSearchRow.Note,
                    CreatedAt = listSearchRow.CreatedAt,
                    Status = (ListSearchStatus)listSearchRow.Status,
                    UpdatedAt = listSearchRow.UpdatedAt,
                    Parts = partsList
                        .Select(p => new ListSearchPartDto
                        {
                            Id = p.PartId,
                            ListSearchId = p.ListSearchId,
                            Definition = p.Definition,
                            Quantity = p.Quantity,
                            Note = p.ItemNote,
                            PartImages = string.IsNullOrEmpty(p.PartImages as string)
                                ? new List<string>()
                                : new List<string>((p.PartImages as string)!.Split(',', StringSplitOptions.RemoveEmptyEntries))
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

        public async Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync(int pageNumber, int pageSize)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var parameters = new DynamicParameters();
                parameters.Add("offset", (pageNumber - 1) * pageSize);
                parameters.Add("pageSize", pageSize);

                // 1. Önce ListSearches kayıtlarını çek
                string listSearchQuery = @"
            SELECT *
            FROM IdtListSearches WITH (NOLOCK)
            ORDER BY CreatedAt DESC
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;";

                var listSearches = await context.Connection.QueryAsync<dynamic>(listSearchQuery, parameters);
                var listSearchList = listSearches.ToList();

                if (!listSearchList.Any())
                {
                    logger.LogWarning($"Liste sorguları bulunamadı. PageNumber: {pageNumber}, PageSize: {pageSize}");
                    return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(Enumerable.Empty<ListSearchDto>());
                }

                // 2. ListSearch Id'lerini topla
                var listSearchIds = listSearchList.Select(ls => (Guid)ls.Id).ToList();

                // 3. Bu Id'lere ait Part'ları çek
                string partsQuery = @"
            SELECT 
                Id as PartId,
                ListSearchId,
                Definition,
                Quantity,
                PartImages,
                Note as ItemNote
            FROM IdtListSearchParts WITH (NOLOCK)
            WHERE ListSearchId IN @ListSearchIds
            ORDER BY ListSearchId;";

                var parts = await context.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchIds = listSearchIds });
                var partsList = parts.ToList();

                // 4. Part'ları ListSearchId'ye göre grupla
                var partsGrouped = partsList
                    .GroupBy(p => (Guid)p.ListSearchId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 5. DTO'ları oluştur
                var result = new List<ListSearchDto>();

                foreach (var row in listSearchList)
                {
                    var listSearchDto = new ListSearchDto
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

                    // İlgili Part'ları ekle
                    if (partsGrouped.ContainsKey(row.Id))
                    {
                        foreach (var part in partsGrouped[row.Id])
                        {
                            listSearchDto.Parts.Add(new ListSearchPartDto
                            {
                                Id = part.PartId,
                                ListSearchId = part.ListSearchId,
                                Definition = part.Definition,
                                Quantity = part.Quantity,
                                Note = part.ItemNote,
                                PartImages = string.IsNullOrEmpty(part.PartImages as string)
                                    ? new List<string>()
                                    : new List<string>((part.PartImages as string)!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            });
                        }
                    }

                    result.Add(listSearchDto);
                }

                logger.LogInformation($"Liste sorguları başarıyla getirildi. Count: {result.Count}, PageNumber: {pageNumber}");
                return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetListSearchesAsync işleminde hata. PageNumber: {PageNumber}, PageSize: {PageSize}", pageNumber, pageSize);
                throw;
            }
        }

        public async Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync()
        {
            try
            {
                var listSearchQuery = @"
                    SELECT *
                    FROM IdtListSearches WITH (NOLOCK)
                    ORDER BY CreatedAt DESC;";
                var listSearches = await context.Connection.QueryAsync<dynamic>(listSearchQuery);
                var listSearchList = listSearches.ToList();

                if (!listSearchList.Any())
                {
                    logger.LogWarning($"Liste sorguları bulunamadı");
                    return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(Enumerable.Empty<ListSearchDto>());
                }

                var listSearchIds = listSearchList.Select(ls => (Guid)ls.Id).ToList();
                var partsQuery = @"
                    SELECT Id as PartId, ListSearchId, Definition, Quantity, PartImages, Note as ItemNote
                    FROM IdtListSearchParts WITH (NOLOCK)
                    WHERE ListSearchId IN @ListSearchIds
                    ORDER BY ListSearchId;";
                var parts = await context.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchIds = listSearchIds });
                var partsList = parts.ToList();
                var partsGrouped = partsList
                    .GroupBy(p => (Guid)p.ListSearchId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<ListSearchDto>();
                foreach (var row in listSearchList)
                {
                    var listSearchDto = new ListSearchDto
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
                    if (partsGrouped.ContainsKey(row.Id))
                    {
                        foreach (var part in partsGrouped[row.Id])
                        {
                            listSearchDto.Parts.Add(new ListSearchPartDto
                            {
                                Id = part.PartId,
                                ListSearchId = part.ListSearchId,
                                Definition = part.Definition,
                                Quantity = part.Quantity,
                                Note = part.ItemNote,
                                PartImages = string.IsNullOrEmpty(part.PartImages as string)
                                    ? new List<string>()
                                    : new List<string>((part.PartImages as string)!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            });
                        }
                    }
                    result.Add(listSearchDto);
                }
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

                var listSearchQuery = @"
                    SELECT *
                    FROM IdtListSearches WITH (NOLOCK)
                    WHERE CreatedBy = @userId
                    ORDER BY CreatedAt DESC;";
                var listSearches = await context.Connection.QueryAsync<dynamic>(listSearchQuery, new { userId });
                var listSearchList = listSearches.ToList();

                if (!listSearchList.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının liste sorguları bulunamadı");
                    return ServiceResult<IEnumerable<ListSearchDto>>.SuccessAsOk(Enumerable.Empty<ListSearchDto>());
                }

                var listSearchIds = listSearchList.Select(ls => (Guid)ls.Id).ToList();
                var partsQuery = @"
                    SELECT Id as PartId, ListSearchId, Definition, Quantity, PartImages, Note as ItemNote
                    FROM IdtListSearchParts WITH (NOLOCK)
                    WHERE ListSearchId IN @ListSearchIds
                    ORDER BY ListSearchId;";
                var parts = await context.Connection.QueryAsync<dynamic>(partsQuery, new { ListSearchIds = listSearchIds });
                var partsList = parts.ToList();
                var partsGrouped = partsList
                    .GroupBy(p => (Guid)p.ListSearchId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var result = new List<ListSearchDto>();
                foreach (var row in listSearchList)
                {
                    var listSearchDto = new ListSearchDto
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
                    if (partsGrouped.ContainsKey(row.Id))
                    {
                        foreach (var part in partsGrouped[row.Id])
                        {
                            listSearchDto.Parts.Add(new ListSearchPartDto
                            {
                                Id = part.PartId,
                                ListSearchId = part.ListSearchId,
                                Definition = part.Definition,
                                Quantity = part.Quantity,
                                Note = part.ItemNote,
                                PartImages = string.IsNullOrEmpty(part.PartImages as string)
                                    ? new List<string>()
                                    : new List<string>((part.PartImages as string)!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            });
                        }
                    }
                    result.Add(listSearchDto);
                }
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