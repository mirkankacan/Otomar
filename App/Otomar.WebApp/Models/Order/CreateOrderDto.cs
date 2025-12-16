namespace Otomar.WebApp.Models.Order
{
    public class CreateOrderDto
    {
        public string Email { get; set; }
        public string IdentityNumber { get; set; }

        public IEnumerable<CreateOrderItemDto> Items { get; set; } = Enumerable.Empty<CreateOrderItemDto>();
        public AddressDto? BillingAddress { get; set; }
        public AddressDto? ShippingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
    }
}

