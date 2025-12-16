namespace Otomar.Application.Dtos.Payment
{
    public class IsBankResponseDto
    {
        public string Response { get; set; }
        public string AuthCode { get; set; }
        public string HostRefNum { get; set; }
        public string ProcReturnCode { get; set; }
        public string TransId { get; set; }
        public string ErrMsg { get; set; }
        public string ErrorCode { get; set; }
        public string SettleId { get; set; }
        public string TrxDate { get; set; }
        public string? CardBrand { get; set; }
        public string? CardIssuer { get; set; }
        public string? AvsApprove { get; set; }
        public string? HostDate { get; set; }
        public string? AvsErrorCodeDetail { get; set; }
        public string NumCode { get; set; }
    }
}