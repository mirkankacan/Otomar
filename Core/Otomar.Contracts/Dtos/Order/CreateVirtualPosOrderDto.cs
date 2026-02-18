using Otomar.Contracts.Enums;
using System.Text.Json.Serialization;

namespace Otomar.Contracts.Dtos.Order
{
    public class CreateVirtualPosOrderDto
    {
        [JsonIgnore]
        public string Code { get; set; }

        [JsonIgnore]
        public string? CartSessionId { get; set; }

        public string Email { get; set; }
        public string IdentityNumber { get; set; }
        public decimal Amount { get; set; }

        [JsonIgnore]
        public OrderType OrderType => OrderType.VirtualPOS;

        public AddressDto? BillingAddress { get; set; }
        public CorporateDto? Corporate { get; set; }
    }
}
