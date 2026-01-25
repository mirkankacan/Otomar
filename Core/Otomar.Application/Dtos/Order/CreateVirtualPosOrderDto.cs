using Otomar.Domain.Enums;
using System.Text.Json.Serialization;

namespace Otomar.Application.Dtos.Order
{
    public class CreateVirtualPosOrderDto
    {
        // PaymentService/Initialize generates
        [JsonIgnore]
        public string Code { get; set; }

        public string Email { get; set; }
        public string IdentityNumber { get; set; }
        public decimal Amount { get; set; }

        [JsonIgnore]
        public OrderType OrderType { get; set; }

        public AddressDto? BillingAddress { get; set; }

        public CorporateDto? Corporate { get; set; }
    }
}