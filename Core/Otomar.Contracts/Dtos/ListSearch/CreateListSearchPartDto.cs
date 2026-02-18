using Microsoft.AspNetCore.Http;

namespace Otomar.Contracts.Dtos.ListSearch
{
    public class CreateListSearchPartDto
    {
        public string Definition { get; set; }
        public string? Note { get; set; }
        public int Quantity { get; set; }
        public List<IFormFile>? PartImages { get; set; } = new();
    }
}
