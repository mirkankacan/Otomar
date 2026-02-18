using Otomar.Contracts.Dtos.ListSearch;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    /// <summary>
    /// Body (JSON) istekleri Refit ile. FormData (multipart) istekleri HttpClient ile gönderilir.
    /// </summary>
    public interface IListSearchApi
    {
        [Get("/api/listsearches")]
        Task<IEnumerable<ListSearchDto>> GetListSearchesAsync(CancellationToken cancellationToken = default);

        [Get("/api/listsearches/paged")]
        Task<IEnumerable<ListSearchDto>> GetListSearchesPagedAsync([Query] int pageNumber, [Query] int pageSize, CancellationToken cancellationToken = default);

        [Get("/api/listsearches/user/{userId}")]
        Task<IEnumerable<ListSearchDto>> GetListSearchesByUserAsync(string userId, CancellationToken cancellationToken = default);

        [Get("/api/listsearches/{requestNo}")]
        Task<ListSearchDto> GetListSearchByRequestNoAsync(string requestNo, CancellationToken cancellationToken = default);

        [Get("/api/listsearches/{id}")]
        Task<ListSearchDto> GetListSearchByIdAsync(Guid id, CancellationToken cancellationToken = default);

        [Post("/api/listsearches/answer")]
        Task CreateListSearchAnswerAsync([Body] List<CreateListSearchAnswerDto> dtos, CancellationToken cancellationToken = default);
    }
}