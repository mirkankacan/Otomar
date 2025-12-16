namespace Otomar.WebApp.Models.Order
{
    public class CreateOrderItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public Guid OrderId { get; set; }
    }
}

