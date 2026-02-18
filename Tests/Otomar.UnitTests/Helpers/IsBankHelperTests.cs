using FluentAssertions;
using Otomar.Contracts.Dtos.Payment;
using Otomar.Persistance.Helpers;
using Otomar.Persistance.Options;

namespace Otomar.UnitTests.Helpers;

/// <summary>
/// IsBankHelper testleri - Ödeme sistemi hash, XML parse ve doğrulama işlemleri.
/// </summary>
public class IsBankHelperTests
{
    private readonly PaymentOptions _paymentOptions = new()
    {
        ClientId = "100100100",
        Username = "testuser",
        Password = "testpass",
        StoreKey = "SKEY123456",
        ApiUrl = "https://test.example.com/api",
        ThreeDVerificationUrl = "https://test.example.com/3d",
        TransactionType = "Auth",
        Currency = "949",
        OkUrl = "https://test.example.com/ok",
        FailUrl = "https://test.example.com/fail",
        StoreType = "3D_PAY",
        HashAlgorithm = "ver3",
        Lang = "tr",
        RefreshTime = "5"
    };

    #region IsBankAmountConvert Tests

    [Theory]
    [InlineData(100.00, "100,00")]
    [InlineData(1234.56, "1234,56")]
    [InlineData(0.01, "0,01")]
    [InlineData(99999.99, "99999,99")]
    [InlineData(0, "0,00")]
    public void IsBankAmountConvert_ValidAmount_ReturnsFormattedString(decimal amount, string expected)
    {
        // Act
        var result = IsBankHelper.IsBankAmountConvert(amount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GenerateHash Tests

    [Fact]
    public void GenerateHash_WithParameters_ReturnsNonEmptyBase64String()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" },
            { "oid", "OTOMAR-TEST-001" },
            { "currency", "949" }
        };

        // Act
        var result = IsBankHelper.GenerateHash(formData, _paymentOptions);

        // Assert
        result.Should().NotBeNullOrEmpty();
        // Base64 format kontrolü
        var action = () => Convert.FromBase64String(result);
        action.Should().NotThrow();
    }

    [Fact]
    public void GenerateHash_SameInputs_ReturnsSameHash()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" },
            { "oid", "OTOMAR-TEST-001" }
        };

        // Act
        var hash1 = IsBankHelper.GenerateHash(formData, _paymentOptions);
        var hash2 = IsBankHelper.GenerateHash(formData, _paymentOptions);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GenerateHash_DifferentInputs_ReturnsDifferentHash()
    {
        // Arrange
        var formData1 = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" }
        };
        var formData2 = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "200,00" }
        };

        // Act
        var hash1 = IsBankHelper.GenerateHash(formData1, _paymentOptions);
        var hash2 = IsBankHelper.GenerateHash(formData2, _paymentOptions);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateHash_ExcludesHashAndEncodingKeys()
    {
        // Arrange
        var formData1 = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" }
        };
        var formData2 = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" },
            { "hash", "somehash" },
            { "encoding", "utf-8" }
        };

        // Act
        var hash1 = IsBankHelper.GenerateHash(formData1, _paymentOptions);
        var hash2 = IsBankHelper.GenerateHash(formData2, _paymentOptions);

        // Assert
        hash1.Should().Be(hash2);
    }

    #endregion

    #region ValidateHash Tests

    [Fact]
    public void ValidateHash_CorrectHash_ReturnsTrue()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" }
        };
        var hash = IsBankHelper.GenerateHash(formData, _paymentOptions);
        formData["hash"] = hash;

        // Act
        var result = IsBankHelper.ValidateHash(formData, _paymentOptions);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHash_IncorrectHash_ReturnsFalse()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "amount", "100,00" },
            { "hash", "invalidhashvalue" }
        };

        // Act
        var result = IsBankHelper.ValidateHash(formData, _paymentOptions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHash_EmptyHash_ReturnsFalse()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" },
            { "hash", "" }
        };

        // Act
        var result = IsBankHelper.ValidateHash(formData, _paymentOptions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHash_MissingHash_ReturnsFalse()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" }
        };

        // Act
        var result = IsBankHelper.ValidateHash(formData, _paymentOptions);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ParseIsBankRequest Tests

    [Fact]
    public void ParseIsBankRequest_ValidParameters_ReturnsValidXml()
    {
        // Arrange
        var parameters = new Dictionary<string, string>
        {
            { "TranType", "Auth" },
            { "Email", "test@example.com" },
            { "oid", "OTOMAR-TEST-001" },
            { "amount", "100,00" },
            { "currency", "949" },
            { "Instalment", "" },
            { "md", "cardnumber" },
            { "cavv", "cavvvalue" },
            { "eci", "05" },
            { "xid", "xidvalue" }
        };

        // Act
        var result = IsBankHelper.ParseIsBankRequest(parameters, _paymentOptions);

        // Assert
        result.Should().Contain("<CC5Request>");
        result.Should().Contain($"<Name>{_paymentOptions.Username}</Name>");
        result.Should().Contain($"<Password>{_paymentOptions.Password}</Password>");
        result.Should().Contain($"<ClientId>{_paymentOptions.ClientId}</ClientId>");
        result.Should().Contain("<Type>Auth</Type>");
        result.Should().Contain("<Email>test@example.com</Email>");
        result.Should().Contain("<OrderId>OTOMAR-TEST-001</OrderId>");
        result.Should().Contain("<Total>100,00</Total>");
        result.Should().Contain("<Currency>949</Currency>");
    }

    #endregion

    #region ParseIsBankResponse Tests

    [Fact]
    public void ParseIsBankResponse_ValidXml_ReturnsDto()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CC5Response>
  <Response>Approved</Response>
  <AuthCode>AUTH123</AuthCode>
  <HostRefNum>REF456</HostRefNum>
  <ProcReturnCode>00</ProcReturnCode>
  <TransId>TRANS789</TransId>
  <ErrMsg></ErrMsg>
  <Extra>
    <SETTLEID>SET001</SETTLEID>
    <TRXDATE>20250101</TRXDATE>
    <ERRORCODE></ERRORCODE>
    <CARDBRAND>VISA</CARDBRAND>
    <CARDISSUER>ISBANK</CARDISSUER>
  </Extra>
</CC5Response>";

        // Act
        var result = IsBankHelper.ParseIsBankResponse(xml);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be("Approved");
        result.AuthCode.Should().Be("AUTH123");
        result.HostRefNum.Should().Be("REF456");
        result.ProcReturnCode.Should().Be("00");
        result.TransId.Should().Be("TRANS789");
        result.CardBrand.Should().Be("VISA");
        result.CardIssuer.Should().Be("ISBANK");
    }

    #endregion

    #region IsPaymentSuccess Tests

    [Fact]
    public void IsPaymentSuccess_ApprovedWith00_ReturnsTrue()
    {
        // Arrange
        var dto = new IsBankResponseDto { Response = "Approved", ProcReturnCode = "00" };

        // Act
        var result = IsBankHelper.IsPaymentSuccess(dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPaymentSuccess_ApprovedWithNon00_ReturnsFalse()
    {
        // Arrange
        var dto = new IsBankResponseDto { Response = "Approved", ProcReturnCode = "99" };

        // Act
        var result = IsBankHelper.IsPaymentSuccess(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPaymentSuccess_DeclinedWith00_ReturnsFalse()
    {
        // Arrange
        var dto = new IsBankResponseDto { Response = "Declined", ProcReturnCode = "00" };

        // Act
        var result = IsBankHelper.IsPaymentSuccess(dto);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPaymentSuccess_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var dto = new IsBankResponseDto { Response = "APPROVED", ProcReturnCode = "00" };

        // Act
        var result = IsBankHelper.IsPaymentSuccess(dto);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region IsSuccess Tests

    [Fact]
    public void IsSuccess_Code00_ReturnsTrue()
    {
        IsBankHelper.IsSuccess("00").Should().BeTrue();
    }

    [Fact]
    public void IsSuccess_NonCode00_ReturnsFalse()
    {
        IsBankHelper.IsSuccess("99").Should().BeFalse();
    }

    [Fact]
    public void IsSuccess_EmptyCode_ReturnsFalse()
    {
        IsBankHelper.IsSuccess("").Should().BeFalse();
    }

    #endregion

    #region IsThreeDSecureValid Tests

    [Theory]
    [InlineData("1", true)]
    [InlineData("2", true)]
    [InlineData("3", true)]
    [InlineData("4", true)]
    [InlineData("5", false)]
    [InlineData("6", false)]
    [InlineData("7", false)]
    [InlineData("8", false)]
    [InlineData("0", false)]
    [InlineData("", false)]
    [InlineData("99", false)]
    public void IsThreeDSecureValid_VariousStatuses_ReturnsExpected(string mdStatus, bool expected)
    {
        // Act
        var result = IsBankHelper.IsThreeDSecureValid(mdStatus);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region GetThreeDSecureStatusMessage Tests

    [Fact]
    public void GetThreeDSecureStatusMessage_Status1_ReturnsFullSecure()
    {
        var result = IsBankHelper.GetThreeDSecureStatusMessage("1");
        result.Should().Contain("Full Secure");
    }

    [Theory]
    [InlineData("2")]
    [InlineData("3")]
    [InlineData("4")]
    public void GetThreeDSecureStatusMessage_Status234_ReturnsHalfSecure(string mdStatus)
    {
        var result = IsBankHelper.GetThreeDSecureStatusMessage(mdStatus);
        result.Should().Contain("Half Secure");
    }

    [Theory]
    [InlineData("5")]
    [InlineData("6")]
    [InlineData("7")]
    [InlineData("8")]
    public void GetThreeDSecureStatusMessage_Status5678_ReturnsNotEnrolled(string mdStatus)
    {
        var result = IsBankHelper.GetThreeDSecureStatusMessage(mdStatus);
        result.Should().Contain("kayıtlı değil");
    }

    [Fact]
    public void GetThreeDSecureStatusMessage_Status0_ReturnsFailure()
    {
        var result = IsBankHelper.GetThreeDSecureStatusMessage("0");
        result.Should().Contain("başars");
    }

    [Fact]
    public void GetThreeDSecureStatusMessage_UnknownStatus_ReturnsUnknown()
    {
        var result = IsBankHelper.GetThreeDSecureStatusMessage("99");
        result.Should().Contain("Bilinmeyen");
        result.Should().Contain("99");
    }

    #endregion
}
