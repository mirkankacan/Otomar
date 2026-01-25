namespace Otomar.WebApp.Dtos.ListSearch
{
    public class CreateListSearchDto
    {
        public string PhoneNumber { get; set; }
        public string ChassisNumber { get; set; }
        public string? Email { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Year { get; set; }
        public string? Engine { get; set; }
        public string? LicensePlate { get; set; }
        public string? Annotation { get; set; }
        public List<CreateListSearchPartDto> Parts { get; set; } = new();
    }
}
