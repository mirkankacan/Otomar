using Otomar.Domain.Enums;

namespace Otomar.Application.Dtos.ListSearch
{
    public class ListSearchDto
    {
        public int Id { get; set; }
        public string NameSurname { get; set; }
        public string? CompanyName { get; set; }
        public string RequestNo { get; set; }

        public string PhoneNumber { get; set; }
        public string ChassisNumber { get; set; }
        public string? Email { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string? Engine { get; set; }
        public string? LicensePlate { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public ListSearchStatus Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public IEnumerable<ListSearchPartDto> Parts { get; set; } = Enumerable.Empty<ListSearchPartDto>();
    }
}