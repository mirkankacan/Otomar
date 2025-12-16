using Otomar.Application.Common;
using Otomar.Application.Dtos.ListSearch;

namespace Otomar.Application.Contracts.Services
{
    public interface IListSearchService
    {
        Task<ServiceResult<string>> CreateListSearchAsync(CreateListSearchDto createListSearchDto);

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesAsync();

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByUserAsync(string userId);

        Task<ServiceResult<IEnumerable<ListSearchDto>>> GetListSearchesByRequestNoAsync(string requestNo);

        Task<ServiceResult<int>> CreateListSearchAnswerAsync(List<CreateListSearchAnswerDto> createListSearchAnswerDtos);
    }
}