namespace Otomar.WebApp.Dtos.Cart
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public int ItemCount { get; set; }
    }
}
