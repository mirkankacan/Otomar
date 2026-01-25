namespace Otomar.Application.Dtos.ListSearch
{
    public class ListSearchPartDto
    {
        public int Id { get; set; }
        public Guid ListSearchId { get; set; }
        public string Definition { get; set; }
        public string? Note { get; set; }
        public int Quantity { get; set; }
        public List<string> PartImages { get; set; }
    }
}