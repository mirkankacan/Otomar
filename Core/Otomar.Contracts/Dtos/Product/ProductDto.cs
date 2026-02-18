namespace Otomar.Contracts.Dtos.Product
{
    public class ProductDto
    {
        public int ID { get; set; }
        public string STOK_KODU { get; set; }
        public string? OEM_KODU { get; set; }
        public string STOK_ADI { get; set; }
        public string URETICI_KODU { get; set; }
        public string URETICI_MARKA_ADI { get; set; }
        public string URETICI_MARKA_LOGO { get; set; }
        public decimal SATIS_FIYAT { get; set; }
        public string ANA_GRUP_ID { get; set; }
        public string ANA_GRUP_ADI { get; set; }
        public string ALT_GRUP_ID { get; set; }
        public string ALT_GRUP_ADI { get; set; }
        public string DOSYA_KONUM { get; set; }
        public string? VITRIN_FOTO { get; set; }
        public string? HASHTAG { get; set; }
        public string? ACIKLAMA { get; set; }
        public string MARKA_ADI { get; set; }
        public string MODEL_ADI { get; set; }
        public string KASA_ADI { get; set; }
        public decimal? STOK_BAKIYE { get; set; }
        public bool HasStock => STOK_BAKIYE > 0 && STOK_BAKIYE is not null ? true : false;
    }
}
