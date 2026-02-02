using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Dtos.ListSearch;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class ListSearchEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/listsearches")
                .WithTags("ListSearches");

            group.MapPost("/", async (
                [FromForm] CreateListSearchDto dto,
                [FromServices] IListSearchService listSearchService,
                CancellationToken cancellationToken) =>
            {
                var result = await listSearchService.CreateListSearchAsync(dto, cancellationToken);
                return result.ToGenericResult();
            })
            .WithName("CreateListSearch")
            .Accepts<CreateListSearchDto>("multipart/form-data")
            .DisableAntiforgery();

            group.MapGet("/", async ([FromServices] IListSearchService listSearchService) =>
            {
                var result = await listSearchService.GetListSearchesAsync();
                return result.ToGenericResult();
            })
            .WithName("GetListSearches");

            group.MapGet("/user/{userId}", async (string userId, [FromServices] IListSearchService listSearchService) =>
            {
                var result = await listSearchService.GetListSearchesByUserAsync(userId);
                return result.ToGenericResult();
            })
            .WithName("GetListSearchesByUser");

            group.MapGet("/{requestNo}", async (string requestNo, [FromServices] IListSearchService listSearchService) =>
            {
                var result = await listSearchService.GetListSearchByRequestNoAsync(requestNo);
                return result.ToGenericResult();
            })
            .WithName("GetListSearchByRequestNo");

            group.MapGet("/{id:guid}", async (Guid id, [FromServices] IListSearchService listSearchService) =>
            {
                var result = await listSearchService.GetListSearchByIdAsync(id);
                return result.ToGenericResult();
            })
            .WithName("GetListSearchById");

            group.MapPost("/answer", async ([FromBody] List<CreateListSearchAnswerDto> dtos, [FromServices] IListSearchService listSearchService) =>
            {
                var result = await listSearchService.CreateListSearchAnswerAsync(dtos);
                return result.ToGenericResult();
            })
            .WithName("CreateListSearchAnswer");
        }
    }
}