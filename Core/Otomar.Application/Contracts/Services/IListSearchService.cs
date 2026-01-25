using Otomar.Application.Common;
using Otomar.Application.Dtos.ListSearch;

namespace Otomar.Application.Contracts.Services
{
    public interface IListSearchService
    {
        Task<ServiceResult<string>> CreateListSearchAsync(CreateListSearchDto createListSearchDto, CancellationToken cancellationToken);

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync();

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByUserAsync(string userId);

        Task<ServiceResult<ListSearchDto>> GetListSearchByRequestNoAsync(string requestNo);

        Task<ServiceResult<ListSearchDto>> GetListSearchByIdAsync(Guid id);

        Task<ServiceResult<int>> CreateListSearchAnswerAsync(List<CreateListSearchAnswerDto> createListSearchAnswerDtos);
    }
}