namespace Otomar.WebApp.Dtos.Client
{
    public class TransactionDto
    {
        public string BELGE_NO { get; set; }
        public string TARIH_DISPLAY { get; set; }
        public DateTime TARIH_RAW { get; set; }
        public string ACIKLAMA { get; set; }
        public string BORC { get; set; }
        public string ALACAK { get; set; }
        public string BAKIYE { get; set; }
    }
}
