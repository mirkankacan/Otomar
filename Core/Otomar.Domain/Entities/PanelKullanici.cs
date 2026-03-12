namespace Otomar.Domain.Entities
{
    /// <summary>
    /// Legacy panel kullanici tablosunu temsil eder (IDV_WEB_PANEL_KULLANICI).
    /// </summary>
    public class PanelKullanici
    {
        public string KullaniciAdi { get; set; } = default!;
        public string Sifre { get; set; } = default!;
        public string? CariIsim { get; set; }
        public string? CariKod { get; set; }
    }
}
