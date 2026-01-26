namespace Otomar.WebApp.Dtos.Product
{
    public class FeaturedProductDto
    {
        public IEnumerable<ProductDto> Recommended { get; set; }
        public IEnumerable<ProductDto> BestSeller { get; set; }
        public IEnumerable<ProductDto> Latest { get; set; }
        public IEnumerable<ProductDto> Lowestprice { get; set; }
        public IEnumerable<ProductDto> HighestPrice { get; set; }
    }
}