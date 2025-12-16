namespace Otomar.Application.Dtos.Product
{
    public class FeaturedProductDto
    {
        public IEnumerable<ProductDto> Recent { get; set; }
        public IEnumerable<ProductDto> BestSeller { get; set; }
        public IEnumerable<ProductDto> Top { get; set; }
        public IEnumerable<ProductDto> ByRate { get; set; }
    }
}