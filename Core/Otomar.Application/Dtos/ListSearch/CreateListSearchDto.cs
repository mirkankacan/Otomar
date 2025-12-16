namespace Otomar.Application.Dtos.ListSearch
{
    public class CreateListSearchDto
    {
        public string PhoneNumber { get; set; }
        public string ChassisNumber { get; set; }
        public string? Email { get; set; }
        public string Brand { get; set; }
        public string? Model { get; set; }
        public string? Year { get; set; }
        public string? Engine { get; set; }
        public string? License { get; set; }
        public string? Annotation { get; set; }
    }
}