using MassTransit;
using Microsoft.Extensions.Logging;
using Otomar.Application.Contracts.Persistence;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Application.Contracts.Services;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.ListSearch;
using Otomar.Shared.Dtos.Notification;
using Otomar.Shared.Enums;
using System.Net;

namespace Otomar.Application.Services
{
    public class ListSearchService(ILogger<ListSearchService> logger, IListSearchRepository listSearchRepository, IUnitOfWork unitOfWork, IIdentityService identityService, IFileService fileService, INotificationService notificationService, IEmailService emailService) : IListSearchService
    {
        public async Task<ServiceResult<int>> CreateListSearchAnswerAsync(List<CreateListSearchAnswerDto> createListSearchAnswerDtos)
        {
            try
            {
                if (createListSearchAnswerDtos == null || !createListSearchAnswerDtos.Any())
                {
                    return ServiceResult<int>.Error("Cevap Listesi Boş", "Cevap listesi boş geçilemez", HttpStatusCode.BadRequest);
                }

                var userId = identityService.GetUserId();
                var listSearchId = createListSearchAnswerDtos.First().ListSearchId;

                unitOfWork.BeginTransaction();

                foreach (var dto in createListSearchAnswerDtos)
                {
                    await listSearchRepository.InsertAnswerAsync(NewId.NextGuid(), dto, userId, DateTime.Now, unitOfWork);
                }

                // Tüm parçalar cevaplandıysa ListSearch'i Answered olarak işaretle
                var totalParts = await listSearchRepository.GetTotalPartsCountAsync(listSearchId, unitOfWork);
                var answeredParts = await listSearchRepository.GetAnsweredPartsCountAsync(listSearchId, unitOfWork);

                if (totalParts > 0 && answeredParts >= totalParts)
                {
                    await listSearchRepository.UpdateStatusAsync(listSearchId, ListSearchStatus.Answered, userId, DateTime.Now, unitOfWork);
                }

                unitOfWork.Commit();
                logger.LogInformation("{Count} adet liste sorgu cevabı oluşturuldu. ListSearchId: {ListSearchId}", createListSearchAnswerDtos.Count, listSearchId);

                // Liste sorguyu oluşturan kullanıcıya bildirim gönder
                try
                {
                    var creatorInfo = await listSearchRepository.GetListSearchCreatorInfoAsync(listSearchId);

                    if (creatorInfo != null && !string.IsNullOrEmpty(creatorInfo.Value.CreatedBy))
                    {
                        await notificationService.CreateNotificationAsync(new CreateNotificationDto
                        {
                            Title = "Liste Sorgunuz Cevaplandı",
                            Message = $"Liste sorgunuz ({creatorInfo.Value.RequestNo}) cevaplandı. Bildirime tıklayarak cevapları inceleyebilirsiniz.",
                            Type = NotificationType.ListSearchAnswered,
                            RedirectUrl = $"/liste-sorgu/talep-no/{creatorInfo.Value.RequestNo}",
                            TargetUserId = creatorInfo.Value.CreatedBy
                        });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Liste sorgu cevap bildirimi gönderilemedi. ListSearchId: {ListSearchId}", listSearchId);
                }

                // Müşteriye cevap e-postası gönder
                try
                {
                    var answeredListSearch = await GetListSearchByIdAsync(listSearchId);
                    if (answeredListSearch.Data != null)
                    {
                        await emailService.SendListSearchAnsweredMailAsync(answeredListSearch.Data, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Liste sorgu cevap e-postası gönderilemedi. ListSearchId: {ListSearchId}", listSearchId);
                }

                return ServiceResult<int>.SuccessAsCreated(createListSearchAnswerDtos.Count, $"/api/listsearches/{listSearchId}");
            }
            catch (Exception ex)
            {
                unitOfWork.Rollback();
                logger.LogWarning(ex, "CreateListSearchAnswerAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<string>> CreateListSearchAsync(CreateListSearchDto createListSearchDto, CancellationToken cancellationToken)
        {
            try
            {
                var requestNo = $"OTOMAR-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
                var userId = identityService.GetUserId();
                var id = NewId.NextGuid();

                unitOfWork.BeginTransaction();

                await listSearchRepository.InsertListSearchAsync(id, requestNo, createListSearchDto, userId, unitOfWork);

                // Sadece parça tanımı ve adet dolu olan satırlar dataya eklenir
                var partsToAdd = (createListSearchDto.Parts ?? new List<CreateListSearchPartDto>())
                    .Where(p => !string.IsNullOrWhiteSpace(p.Definition) && p.Quantity > 0)
                    .ToList();

                foreach (var part in partsToAdd)
                {
                    string? imagePathsAsString = null;
                    if (part.PartImages != null)
                    {
                        var fileServiceResult = await fileService.UploadFileAsync(part.PartImages, FileType.ListSearch, requestNo, cancellationToken);
                        imagePathsAsString = string.Join(",", fileServiceResult.Data);
                    }

                    await listSearchRepository.InsertListSearchPartAsync(id, part.Definition, part.Quantity, part.Note, imagePathsAsString, unitOfWork);
                }

                unitOfWork.Commit();
                logger.LogInformation($"{requestNo} istek numaralı liste araması oluşturuldu");

                // Admin'lere bildirim gönder
                try
                {
                    await notificationService.CreateNotificationAsync(new CreateNotificationDto
                    {
                        Title = "Yeni Liste Sorgu",
                        Message = $"{createListSearchDto.NameSurname} tarafından yeni bir liste sorgu ({requestNo}) oluşturuldu. Cevaplamak için bildirime tıklayınız.",
                        Type = NotificationType.ListSearchCreated,
                        RedirectUrl = $"/liste-sorgu/cevapla/{requestNo}",
                        TargetRoleName = "Admin"
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Liste sorgu oluşturma bildirimi gönderilemedi. RequestNo: {RequestNo}", requestNo);
                }

                // Müşteriye bilgilendirme e-postası gönder
                try
                {
                    var createdListSearch = await GetListSearchByRequestNoAsync(requestNo);
                    if (createdListSearch.Data != null)
                    {
                        await emailService.SendListSearchMailAsync(createdListSearch.Data, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Liste sorgu oluşturma e-postası gönderilemedi. RequestNo: {RequestNo}", requestNo);
                }

                return ServiceResult<string>.SuccessAsCreated(requestNo, $"/api/listsearches/{requestNo}");
            }
            catch (Exception ex)
            {
                unitOfWork.Rollback();
                logger.LogWarning(ex, "CreateListSearchAsync işleminde hata");
                throw;
            }
        }

        public async Task<ServiceResult<ListSearchDto>> GetListSearchByIdAsync(Guid id)
        {
            try
            {
                var listSearch = await listSearchRepository.GetByIdAsync(id);

                if (listSearch == null)
                {
                    logger.LogWarning($"{id} ID'li liste sorgusu bulunamadı");
                    return ServiceResult<ListSearchDto>.Error("Liste Sorgusu Bulunamadı", $"{id} ID'li liste sorgusu bulunamadı", HttpStatusCode.NotFound);
                }

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

                var listSearch = await listSearchRepository.GetByRequestNoAsync(requestNo);

                if (listSearch == null)
                {
                    logger.LogWarning($"{requestNo} istek numaralı liste sorgusu bulunamadı");
                    return ServiceResult<ListSearchDto>.Error("Liste Sorgusu Bulunamadı", $"{requestNo} istek numaralı liste sorgusu bulunamadı", HttpStatusCode.NotFound);
                }

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

                var offset = (pageNumber - 1) * pageSize;
                var result = await listSearchRepository.GetPagedAsync(offset, pageSize);

                if (!result.Any())
                {
                    logger.LogWarning($"Liste sorguları bulunamadı. PageNumber: {pageNumber}, PageSize: {pageSize}");
                }

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
                var result = await listSearchRepository.GetAllAsync();

                if (!result.Any())
                {
                    logger.LogWarning($"Liste sorguları bulunamadı");
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

                var result = await listSearchRepository.GetByUserAsync(userId);

                if (!result.Any())
                {
                    logger.LogWarning($"{userId} ID'li kullanıcının liste sorguları bulunamadı");
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
