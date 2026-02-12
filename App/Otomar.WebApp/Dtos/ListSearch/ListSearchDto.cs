using Otomar.WebApp.Enums;

namespace Otomar.WebApp.Dtos.ListSearch
{
    public class ListSearchDto
    {
        public Guid Id { get; set; }
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
        public List<ListSearchPartDto> Parts { get; set; } = new();
    }
}