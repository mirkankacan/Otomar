namespace Otomar.Application.Dtos.Order
{
    public class CreateOrderDto
    {
        public string Code { get; set; }
        public string Email { get; set; }
        public string IdentityNumber { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
        public AddressDto? BillingAddress { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
    }
}