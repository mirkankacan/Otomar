using Otomar.Contracts.Common;
using Otomar.Contracts.Dtos.ListSearch;

namespace Otomar.Application.Contracts.Services
{
    public interface IListSearchService
    {
        Task<ServiceResult<string>> CreateListSearchAsync(CreateListSearchDto createListSearchDto, CancellationToken cancellationToken);

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync();

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync(int pageNumber, int pageSize);

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByUserAsync(string userId);

        Task<ServiceResult<ListSearchDto>> GetListSearchByRequestNoAsync(string requestNo);

        Task<ServiceResult<ListSearchDto>> GetListSearchByIdAsync(Guid id);

        Task<ServiceResult<int>> CreateListSearchAnswerAsync(List<CreateListSearchAnswerDto> createListSearchAnswerDtos);
    }
}