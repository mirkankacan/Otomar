using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.ListSearch;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Route("liste-arama")]
    public class ListSearchController(IListSearchApi listSearchApi) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("olustur")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateListSearch(
            [FromForm] CreateListSearchDto dto,
            CancellationToken cancellationToken = default)
        {
            return await listSearchApi.CreateListSearchAsync(dto, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("listele")]
        public async Task<IActionResult> GetListSearches(CancellationToken cancellationToken = default)
        {
            return await listSearchApi.GetListSearchesAsync(cancellationToken).ToActionResultAsync();
        }

        [HttpGet("kullanici/{userId}")]
        public async Task<IActionResult> GetListSearchesByUser(string userId, CancellationToken cancellationToken = default)
        {
            return await listSearchApi.GetListSearchesByUserAsync(userId, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("talep-no/{requestNo}")]
        public async Task<IActionResult> GetListSearchByRequestNo(string requestNo, CancellationToken cancellationToken = default)
        {
            return await listSearchApi.GetListSearchByRequestNoAsync(requestNo, cancellationToken).ToActionResultAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetListSearchById(Guid id, CancellationToken cancellationToken = default)
        {
            return await listSearchApi.GetListSearchByIdAsync(id, cancellationToken).ToActionResultAsync();
        }

        [HttpPost("cevap")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateListSearchAnswer([FromBody] List<CreateListSearchAnswerDto> dtos, CancellationToken cancellationToken = default)
        {
            return await listSearchApi.CreateListSearchAnswerAsync(dtos, cancellationToken).ToActionResultAsync();
        }
    }
}
