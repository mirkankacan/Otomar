using Otomar.Domain.Enums;
using System.Text.Json.Serialization;

namespace Otomar.Application.Dtos.Order
{
    public class CreatePurchaseOrderDto
    {
        // PaymentService/Initialize generates
        [JsonIgnore]
        public string Code { get; set; }

        public string Email { get; set; }
        public string IdentityNumber { get; set; }
        [JsonIgnore]
        public OrderType OrderType => OrderType.Purchase;
        public AddressDto? BillingAddress { get; set; }

        public AddressDto? ShippingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
    }
}