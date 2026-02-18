namespace Otomar.Contracts.Dtos.Category
{
    public class BrandModelYearDto
    {
        public int MARKA_KODU { get; set; }
        public string MARKA_ADI { get; set; }
        public bool AKTIF { get; set; }
        public IEnumerable<ModelYearDto> ModelsYears { get; set; } = Enumerable.Empty<ModelYearDto>();
    }

    public class ModelYearDto
    {
        public int MODEL_KODU { get; set; }
        public string MODEL_ADI { get; set; }
        public int MARKA_KODU { get; set; }
        public bool AKTIF { get; set; }
        public IEnumerable<YearDto> Years { get; set; } = Enumerable.Empty<YearDto>();
    }
}
