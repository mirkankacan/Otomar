using Otomar.Shared.Dtos.ListSearch;
using Otomar.Shared.Enums;

namespace Otomar.Application.Interfaces.Repositories
{
    /// <summary>
    /// Liste sorgu (ListSearch) veritabanı işlemleri için repository arayüzü.
    /// </summary>
    public interface IListSearchRepository
    {
        /// <summary>
        /// Yeni bir liste sorgu cevabı (answer) ekler.
        /// </summary>
        Task InsertAnswerAsync(Guid answerId, CreateListSearchAnswerDto dto, string answeredBy, DateTime answeredAt, IUnitOfWork unitOfWork);

        /// <summary>
        /// Belirtilen liste sorguya ait toplam parça sayısını döner.
        /// </summary>
        Task<int> GetTotalPartsCountAsync(Guid listSearchId, IUnitOfWork unitOfWork);

        /// <summary>
        /// Belirtilen liste sorguya ait cevaplanmış parça sayısını döner.
        /// </summary>
        Task<int> GetAnsweredPartsCountAsync(Guid listSearchId, IUnitOfWork unitOfWork);

        /// <summary>
        /// Liste sorgu durumunu günceller.
        /// </summary>
        Task UpdateStatusAsync(Guid listSearchId, ListSearchStatus status, string updatedBy, DateTime updatedAt, IUnitOfWork unitOfWork);

        /// <summary>
        /// Liste sorgu oluşturan kullanıcı bilgisini döner (RequestNo, CreatedBy).
        /// </summary>
        Task<(string? RequestNo, string? CreatedBy)?> GetListSearchCreatorInfoAsync(Guid listSearchId);

        /// <summary>
        /// Yeni bir liste sorgu kaydı ekler.
        /// </summary>
        Task InsertListSearchAsync(Guid id, string requestNo, CreateListSearchDto dto, string? userId, IUnitOfWork unitOfWork);

        /// <summary>
        /// Yeni bir liste sorgu parçası ekler.
        /// </summary>
        Task InsertListSearchPartAsync(Guid listSearchId, string definition, int quantity, string? note, string? partImages, IUnitOfWork unitOfWork);

        /// <summary>
        /// ID'ye göre liste sorgu detayını (parça ve cevaplarıyla birlikte) döner.
        /// </summary>
        Task<ListSearchDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// İstek numarasına göre liste sorgu detayını (parça ve cevaplarıyla birlikte) döner.
        /// </summary>
        Task<ListSearchDto?> GetByRequestNoAsync(string requestNo);

        /// <summary>
        /// Sayfalanmış liste sorgu listesini döner.
        /// </summary>
        Task<IEnumerable<ListSearchDto>> GetPagedAsync(int offset, int pageSize);

        /// <summary>
        /// Tüm liste sorgularını döner.
        /// </summary>
        Task<IEnumerable<ListSearchDto>> GetAllAsync();

        /// <summary>
        /// Belirtilen kullanıcıya ait liste sorgularını döner.
        /// </summary>
        Task<IEnumerable<ListSearchDto>> GetByUserAsync(string userId);
    }
}
