namespace Otomar.Application.Dtos.Cart
{
    public class CartDto
    {
        public List<CartItemDto> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(x => x.UnitPrice * x.Quantity);
        public decimal ShippingCost { get; set; }
        public decimal Total => SubTotal + ShippingCost;
        public int ItemCount => Items.Sum(x => x.Quantity);
    }
}