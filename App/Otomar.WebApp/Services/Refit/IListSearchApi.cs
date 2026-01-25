using Otomar.WebApp.Dtos.ListSearch;
using Refit;

namespace Otomar.WebApp.Services.Refit
{
    public interface IListSearchApi
    {
        [Post("/api/listsearches")]
        [Multipart]
        Task<ListSearchDto> CreateListSearchAsync(
            CreateListSearchDto dto,
            CancellationToken cancellationToken = default);

        [Get("/api/listsearches")]
        Task<IEnumerable<ListSearchDto>> GetListSearchesAsync(CancellationToken cancellationToken = default);

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
