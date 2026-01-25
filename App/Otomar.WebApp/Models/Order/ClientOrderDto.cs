namespace Otomar.WebApp.Models.Order
{
    public class ClientOrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = default!;
        public string CreatedBy { get; set; } = default!;
        public string CreatedByFullName { get; set; } = default!;
        public string ClientName { get; set; } = default!;
        public string ClientAddress { get; set; } = default!;
        public string ClientPhone { get; set; } = default!;
        public string InsuranceCompany { get; set; } = default!;
        public string DocumentNo { get; set; } = default!;
        public string LicensePlate { get; set; } = default!;
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
