namespace Otomar.Application.Dtos.Client
{
    public class TransactionDto
    {
        public string BELGE_NO { get; set; }
        public string TARIH_DISPLAY { get; set; } // gösterilecek
        public DateTime TARIH_RAW { get; set; }     // sıralama için
        public string ACIKLAMA { get; set; }
        public string BORC { get; set; }
        public string ALACAK { get; set; }
        public string BAKIYE { get; set; }
    }
}