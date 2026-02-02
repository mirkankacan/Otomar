using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Otomar.WebApp.Dtos.ListSearch;
using Otomar.WebApp.Extensions;
using Otomar.WebApp.Services.Refit;

namespace Otomar.WebApp.Controllers
{
    [Authorize]
    [Route("liste-sorgu")]
    public class ListSearchController(IListSearchApi listSearchApi, IHttpClientFactory httpClientFactory) : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("olustur")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("olustur")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateListSearch(
    [FromForm] CreateListSearchDto dto,
    CancellationToken cancellationToken = default)
        {
            using var content = new MultipartFormDataContent();

            // Ana alanları ekle
            content.Add(new StringContent(dto.NameSurname), "NameSurname");

            if (!string.IsNullOrEmpty(dto.CompanyName))
                content.Add(new StringContent(dto.CompanyName), "CompanyName");

            content.Add(new StringContent(dto.PhoneNumber), "PhoneNumber");
            content.Add(new StringContent(dto.ChassisNumber), "ChassisNumber");

            if (!string.IsNullOrEmpty(dto.Email))
                content.Add(new StringContent(dto.Email), "Email");

            content.Add(new StringContent(dto.Brand), "Brand");
            content.Add(new StringContent(dto.Model), "Model");
            content.Add(new StringContent(dto.Year), "Year");

            if (!string.IsNullOrEmpty(dto.Engine))
                content.Add(new StringContent(dto.Engine), "Engine");

            if (!string.IsNullOrEmpty(dto.LicensePlate))
                content.Add(new StringContent(dto.LicensePlate), "LicensePlate");

            if (!string.IsNullOrEmpty(dto.Note))
                content.Add(new StringContent(dto.Note), "Note");

            // Parts'ları ekle
            for (int i = 0; i < dto.Parts.Count; i++)
            {
                var part = dto.Parts[i];

                content.Add(new StringContent(part.Definition), $"Parts[{i}].Definition");

                if (!string.IsNullOrEmpty(part.Note))
                    content.Add(new StringContent(part.Note), $"Parts[{i}].Note");

                content.Add(new StringContent(part.Quantity.ToString()), $"Parts[{i}].Quantity");

                // Resimleri BUFFER'A AL - bu önemli!
                if (part.PartImages != null && part.PartImages.Any())
                {
                    foreach (var image in part.PartImages)
                    {
                        // Stream'i byte array'e çevir ki retry'da tekrar kullanılabilsin
                        using var memoryStream = new MemoryStream();
                        await image.OpenReadStream().CopyToAsync(memoryStream, cancellationToken);
                        var bytes = memoryStream.ToArray();

                        var byteArrayContent = new ByteArrayContent(bytes);
                        byteArrayContent.Headers.ContentType =
                            new System.Net.Http.Headers.MediaTypeHeaderValue(image.ContentType);
                        content.Add(byteArrayContent, $"Parts[{i}].PartImages", image.FileName);
                    }
                }
            }

            var client = httpClientFactory.CreateClient("OtomarApi");
            using var response = await client.PostAsync("api/listsearches", content, cancellationToken);

            return await response.ToActionResultAsync(cancellationToken);
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