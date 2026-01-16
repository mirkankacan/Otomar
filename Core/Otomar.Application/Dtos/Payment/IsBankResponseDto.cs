using System.Xml.Serialization;

namespace Otomar.Application.Dtos.Payment
{
    [XmlRoot("CC5Response")]  // ✅ Root element
    public class IsBankResponseDto
    {
        [XmlElement("Response")]
        public string Response { get; set; }

        [XmlElement("AuthCode")]
        public string AuthCode { get; set; }

        [XmlElement("HostRefNum")]
        public string HostRefNum { get; set; }

        [XmlElement("ProcReturnCode")]
        public string ProcReturnCode { get; set; }

        [XmlElement("TransId")]
        public string TransId { get; set; }

        [XmlElement("ErrMsg")]
        public string ErrMsg { get; set; }

        [XmlElement("Extra")]  // ✅ Extra nested element
        public ExtraInfo Extra { get; set; }

        // Properties from Extra (for backward compatibility)
        [XmlIgnore]
        public string ErrorCode => Extra?.ErrorCode;

        [XmlIgnore]
        public string SettleId => Extra?.SettleId;

        [XmlIgnore]
        public string TrxDate => Extra?.TrxDate;

        [XmlIgnore]
        public string CardBrand => Extra?.CardBrand;

        [XmlIgnore]
        public string CardIssuer => Extra?.CardIssuer;

        [XmlIgnore]
        public string AvsApprove => Extra?.AvsApprove;

        [XmlIgnore]
        public string HostDate => Extra?.HostDate;

        [XmlIgnore]
        public string AvsErrorCodeDetail => Extra?.AvsErrorCodeDetail;

        [XmlIgnore]
        public string NumCode => Extra?.NumCode;
    }

    public class ExtraInfo
    {
        [XmlElement("SETTLEID")]
        public string SettleId { get; set; }

        [XmlElement("TRXDATE")]
        public string TrxDate { get; set; }

        [XmlElement("ERRORCODE")]
        public string ErrorCode { get; set; }

        [XmlElement("NUMCODE")]
        public string NumCode { get; set; }

        [XmlElement("CARDBRAND")]
        public string CardBrand { get; set; }

        [XmlElement("CARDISSUER")]
        public string CardIssuer { get; set; }

        [XmlElement("AVSAPPROVE")]
        public string AvsApprove { get; set; }

        [XmlElement("HOSTDATE")]
        public string HostDate { get; set; }

        [XmlElement("AVSERRORCODEDETAIL")]
        public string AvsErrorCodeDetail { get; set; }
    }
}