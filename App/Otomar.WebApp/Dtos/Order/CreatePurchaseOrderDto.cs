using System.Text.Json.Serialization;
using Otomar.WebApp.Enums;

namespace Otomar.WebApp.Dtos.Order
{
    public class CreatePurchaseOrderDto
    {
        [JsonIgnore]
        public string Code { get; set; }

        public string Email { get; set; }
        public string IdentityNumber { get; set; }

        [JsonIgnore]
        public OrderType OrderType { get; set; }

        public AddressDto? BillingAddress { get; set; }

        public AddressDto? ShippingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
    }
}
