using Otomar.Application.Dtos.Order;

namespace Otomar.Application.Dtos.ListSearch
{
    public class ListSearchDto
    {
        public int Id { get; set; }
        public string RequestNo { get; set; }

        public string ChassisNumber { get; set; }
        public string? Email { get; set; }
        public string Brand { get; set; }
        public string? Model { get; set; }
        public string? Year { get; set; }
        public string? Engine { get; set; }
        public string? License { get; set; }
        public string? Annotation { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? UserId { get; set; }
        public IEnumerable<OrderItemDto> Items { get; set; } = Enumerable.Empty<OrderItemDto>();
    }
}