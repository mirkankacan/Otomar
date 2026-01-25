namespace Otomar.WebApp.Dtos.Cart
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string ImagePath { get; set; }
        public string ManufacturerLogo { get; set; }
        public decimal? StockQuantity { get; set; }
    }
}
