using FluentAssertions;
using Moq;
using Moq.Protected;
using Otomar.Application.Options;
using Otomar.Persistence.Services;
using System.Net;
using System.Text;

namespace Otomar.UnitTests.Services;

/// <summary>
/// IsBankPaymentService testleri - Hash üretimi, XML işleme ve doğrulama.
/// </summary>
public class IsBankPaymentServiceTests
{
    private readonly PaymentOptions _paymentOptions;
    private readonly IsBankPaymentService _sut;

    public IsBankPaymentServiceTests()
    {
        _paymentOptions = new PaymentOptions
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

        var httpClient = new HttpClient();
        _sut = new IsBankPaymentService(httpClient, _paymentOptions);
    }

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
        var result = _sut.GenerateHash(formData);

        // Assert
        result.Should().NotBeNullOrEmpty();
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
        var hash1 = _sut.GenerateHash(formData);
        var hash2 = _sut.GenerateHash(formData);

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
        var hash1 = _sut.GenerateHash(formData1);
        var hash2 = _sut.GenerateHash(formData2);

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
        var hash1 = _sut.GenerateHash(formData1);
        var hash2 = _sut.GenerateHash(formData2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void GenerateHash_SpecialCharacters_EscapesPipeAndBackslash()
    {
        // Arrange — pipe ve backslash escape edilmeli
        var formData = new Dictionary<string, string>
        {
            { "value", @"test|with\special" }
        };

        // Act
        var hash1 = _sut.GenerateHash(formData);

        // Escape'siz farklı veri ile karşılaştır
        var formData2 = new Dictionary<string, string>
        {
            { "value", "testwithspecial" }
        };
        var hash2 = _sut.GenerateHash(formData2);

        // Assert — farklı hash üretmeli
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateHash_EmptyDictionary_ReturnsHashOfStoreKeyOnly()
    {
        // Arrange
        var formData = new Dictionary<string, string>();

        // Act
        var result = _sut.GenerateHash(formData);

        // Assert — sadece StoreKey hash'lenmeli, boş olmamalı
        result.Should().NotBeNullOrEmpty();
        var action = () => Convert.FromBase64String(result);
        action.Should().NotThrow();
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
        var hash = _sut.GenerateHash(formData);
        formData["hash"] = hash;

        // Act
        var result = _sut.ValidateHash(formData);

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
        var result = _sut.ValidateHash(formData);

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
        var result = _sut.ValidateHash(formData);

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
        var result = _sut.ValidateHash(formData);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHash_NullHashKey_ReturnsFalse()
    {
        // Arrange — hash key mevcut ama null value
        var formData = new Dictionary<string, string>
        {
            { "clientid", "100100100" }
        };

        // Act
        var result = _sut.ValidateHash(formData);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region BuildRequestXml Tests

    [Fact]
    public void BuildRequestXml_ValidParameters_ReturnsValidXml()
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
        var result = _sut.BuildRequestXml(parameters);

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

    [Fact]
    public void BuildRequestXml_MissingKey_ThrowsKeyNotFoundException()
    {
        // Arrange — eksik parametre ile çağır
        var parameters = new Dictionary<string, string>
        {
            { "TranType", "Auth" },
            { "Email", "test@example.com" }
            // oid, amount, currency vb. eksik
        };

        // Act
        var action = () => _sut.BuildRequestXml(parameters);

        // Assert
        action.Should().Throw<KeyNotFoundException>();
    }

    #endregion

    #region ParseResponse Tests

    [Fact]
    public void ParseResponse_ValidXml_ReturnsDto()
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
        var result = _sut.ParseResponse(xml);

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

    [Fact]
    public void ParseResponse_InvalidXml_ThrowsException()
    {
        // Arrange
        var invalidXml = "this is not xml";

        // Act
        var action = () => _sut.ParseResponse(invalidXml);

        // Assert
        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ParseResponse_EmptyResponse_ReturnsEmptyFields()
    {
        // Arrange — minimum valid XML
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CC5Response>
  <Response></Response>
  <ProcReturnCode></ProcReturnCode>
</CC5Response>";

        // Act
        var result = _sut.ParseResponse(xml);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().BeEmpty();
        result.ProcReturnCode.Should().BeEmpty();
    }

    #endregion

    #region SendPaymentRequestAsync Tests

    [Fact]
    public async Task SendPaymentRequestAsync_SuccessfulResponse_ReturnsParsedDto()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
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

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseXml, Encoding.UTF8, "text/xml")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var sut = new IsBankPaymentService(httpClient, _paymentOptions);

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
        var result = await sut.SendPaymentRequestAsync(parameters, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be("Approved");
        result.ProcReturnCode.Should().Be("00");
        result.AuthCode.Should().Be("AUTH123");
        result.TransId.Should().Be("TRANS789");
    }

    [Fact]
    public async Task SendPaymentRequestAsync_DeclinedResponse_ReturnsParsedDto()
    {
        // Arrange
        var responseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CC5Response>
  <Response>Declined</Response>
  <AuthCode></AuthCode>
  <HostRefNum></HostRefNum>
  <ProcReturnCode>05</ProcReturnCode>
  <TransId></TransId>
  <ErrMsg>Kart limiti yetersiz</ErrMsg>
</CC5Response>";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseXml, Encoding.UTF8, "text/xml")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var sut = new IsBankPaymentService(httpClient, _paymentOptions);

        var parameters = new Dictionary<string, string>
        {
            { "TranType", "Auth" },
            { "Email", "test@example.com" },
            { "oid", "OTOMAR-TEST-002" },
            { "amount", "50000,00" },
            { "currency", "949" },
            { "Instalment", "" },
            { "md", "cardnumber" },
            { "cavv", "cavvvalue" },
            { "eci", "05" },
            { "xid", "xidvalue" }
        };

        // Act
        var result = await sut.SendPaymentRequestAsync(parameters, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Response.Should().Be("Declined");
        result.ProcReturnCode.Should().Be("05");
        result.ErrMsg.Should().Contain("Kart limiti");
    }

    [Fact]
    public async Task SendPaymentRequestAsync_SendsCorrectXmlToCorrectUrl()
    {
        // Arrange
        HttpRequestMessage? capturedRequest = null;
        var responseXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<CC5Response>
  <Response>Approved</Response>
  <ProcReturnCode>00</ProcReturnCode>
</CC5Response>";

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseXml, Encoding.UTF8, "text/xml")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var sut = new IsBankPaymentService(httpClient, _paymentOptions);

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
        await sut.SendPaymentRequestAsync(parameters, CancellationToken.None);

        // Assert — doğru URL'ye POST atılmalı
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri!.ToString().Should().Be(_paymentOptions.ApiUrl);

        // XML body doğrulama
        var body = await capturedRequest.Content!.ReadAsStringAsync();
        body.Should().Contain("<CC5Request>");
        body.Should().Contain("<OrderId>OTOMAR-TEST-001</OrderId>");
        body.Should().Contain($"<ClientId>{_paymentOptions.ClientId}</ClientId>");
    }

    [Fact]
    public async Task SendPaymentRequestAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = new HttpClient(mockHandler.Object);
        var sut = new IsBankPaymentService(httpClient, _paymentOptions);

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
        var action = () => sut.SendPaymentRequestAsync(parameters, cts.Token);

        // Assert
        await action.Should().ThrowAsync<TaskCanceledException>();
    }

    #endregion
}
