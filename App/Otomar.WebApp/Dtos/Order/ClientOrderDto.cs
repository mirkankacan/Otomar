namespace Otomar.WebApp.Dtos.Order
{
    public class ClientOrderDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByFullName { get; set; }
        public string ClientName { get; set; }
        public string ClientAddress { get; set; }
        public string ClientPhone { get; set; }
        public string InsuranceCompany { get; set; }
        public string DocumentNo { get; set; }
        public string LicensePlate { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SubTotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
