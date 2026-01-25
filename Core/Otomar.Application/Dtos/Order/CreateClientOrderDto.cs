namespace Otomar.Application.Dtos.Order
{
    public class CreateClientOrderDto
    {
        public string ClientName { get; set; }
        public string ClientAddress { get; set; }
        public string ClientPhone { get; set; }
        public string InsuranceCompany { get; set; }
        public string DocumentNo { get; set; }
        public string LicensePlate { get; set; }
        public string? Note { get; set; }
        //public List<OrderItemDto> Items { get; set; } = new();
    }
}