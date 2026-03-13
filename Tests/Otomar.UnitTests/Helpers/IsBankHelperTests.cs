using FluentAssertions;
using Otomar.Shared.Dtos.Payment;
using Otomar.Application.Helpers;

namespace Otomar.UnitTests.Helpers;

/// <summary>
/// IsBankHelper testleri - Yalnızca pure utility fonksiyonları.
/// Hash, XML ve HTTP işlemleri IsBankPaymentServiceTests'te test edilir.
/// </summary>
public class IsBankHelperTests
{
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
        result.Should().Contain("başarısız");
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
