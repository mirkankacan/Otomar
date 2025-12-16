namespace Otomar.Application.Dtos.Product
{
    public record ProductDto(int ID, string STOK_KODU, string? OEM_KODU, string STOK_ADI, string URETICI_KODU, string URETICI_MARKA_ADI, string URETICI_MARKA_LOGO, decimal SATIS_FIYAT, string ANA_GRUP_ID, string ANA_GRUP_ADI, string ALT_GRUP_ID, string ALT_GRUP_ADI, string DOSYA_KONUM, string? VITRIN_FOTO, string? HASHTAG, string? ACIKLAMA, string MARKA_ADI, string MODEL_ADI, string KASA_ADI, decimal? STOK_BAKIYE);
}