using Carter;
using Microsoft.AspNetCore.Mvc;
using Otomar.Application.Contracts.Services;
using Otomar.WebApi.Extensions;

namespace Otomar.WebApi.Endpoints
{
    public class ClientEndpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("api/clients")
                .WithTags("Clients");

            group.MapGet("/code/{clientCode}", async (string clientCode, [FromServices] IClientService clientService) =>
            {
                var result = await clientService.GetClientByCodeAsync(clientCode);
                return result.ToGenericResult();
            })
            .WithName("GetClientByCode");

            group.MapGet("/taxNumber/{taxNumber}", async (string taxNumber, [FromServices] IClientService clientService) =>
            {
                var result = await clientService.GetClientByTaxTcNumberAsync(taxNumber);
                return result.ToGenericResult();
            })
          .WithName("GetClientByTaxNumber");

            group.MapGet("/{clientCode}/transactions", async (string clientCode, [FromServices] IClientService clientService) =>
            {
                var result = await clientService.GetClientTransactionsByCodeAsync(clientCode);
                return result.ToGenericResult();
            })
              .WithName("GetClientTransactionsByCode");

            group.MapGet("/{clientCode}/transactions/paged", async (string clientCode, [FromServices] IClientService clientService, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10) =>
            {
                var result = await clientService.GetClientTransactionsByCodeAsync(clientCode, pageNumber, pageSize);
                return result.ToGenericResult();
            })
              .WithName("GetClientTransactionsByCodePaged");
        }
    }
}