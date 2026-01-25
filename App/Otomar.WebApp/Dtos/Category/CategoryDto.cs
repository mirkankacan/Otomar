namespace Otomar.WebApp.Dtos.Category
{
    public class CategoryDto
    {
        public int ANA_ID { get; set; }
        public int GRUP_ID { get; set; }
        public string ANA_GRUP_ADI { get; set; }
        public string GRUP_IKON { get; set; }
        public IEnumerable<SubCategoryDto> SubCategories { get; set; } = Enumerable.Empty<SubCategoryDto>();
    }
}
