namespace Otomar.Shared.Dtos.ListSearch
{
    public class CreateListSearchAnswerDto
    {
        public Guid ListSearchId { get; set; }
        public int ListSearchPartId { get; set; }
        public string? StockCode { get; set; }
        public string? OemCode { get; set; }
        public string? StockName { get; set; }
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public bool KdvIncluded { get; set; }
    }
}
