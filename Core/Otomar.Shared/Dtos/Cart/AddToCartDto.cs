namespace Otomar.Shared.Dtos.Cart
{
    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;

        /// <summary>
        /// Liste sorgusu cevabından gelen teklif fiyatı. Null ise stok fiyatı (SATIS_FIYAT) kullanılır.
        /// </summary>
        public decimal? OverridePrice { get; set; }

        /// <summary>
        /// Teklif fiyatının kaynağı olan ListSearchAnswer ID'si. OverridePrice doğrulaması için zorunludur.
        /// </summary>
        public Guid? ListSearchAnswerId { get; set; }
    }
}
