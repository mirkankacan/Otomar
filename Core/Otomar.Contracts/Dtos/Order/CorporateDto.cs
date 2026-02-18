namespace Otomar.Contracts.Dtos.Order
{
    public class CorporateDto
    {
        public string? CompanyName { get; set; }
        public string? TaxNumber { get; set; }
        public string? TaxOffice { get; set; }
        public bool? IsEInvoiceUser { get; set; }
    }
}
