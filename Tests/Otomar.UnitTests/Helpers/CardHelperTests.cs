using FluentAssertions;
using Otomar.Persistance.Helpers;

namespace Otomar.UnitTests.Helpers;

/// <summary>
/// CardHelper testleri - Saf static method'lar, mock gerekmez.
/// Her test AAA (Arrange-Act-Assert) pattern'ini takip eder.
/// İsimlendirme: MethodAdı_Senaryo_BeklenenSonuç
/// </summary>
public class CardHelperTests
{
    #region GetBrandName Tests

    [Theory]
    [InlineData("4111111111111111", "100")] // VISA
    [InlineData("4000000000000000", "100")] // VISA
    [InlineData("5500000000000004", "200")] // MASTERCARD
    [InlineData("5111111111111111", "200")] // MASTERCARD
    [InlineData("9000000000000000", "300")] // TROY
    [InlineData("3400000000000000", "400")] // AMEX (34xx)
    [InlineData("3700000000000000", "400")] // AMEX (37xx)
    public void GetBrandName_ValidCardNumber_ReturnsCorrectBrandCode(string cardNumber, string expectedCode)
    {
        // Act
        var result = CardHelper.GetBrandName(cardNumber);

        // Assert
        result.Should().Be(expectedCode);
    }

    [Fact]
    public void GetBrandName_UnknownPrefix_ReturnsDefaultVisa()
    {
        // Arrange - 6 ile başlayan kart (Discover gibi) tanımlı değil
        var cardNumber = "6011000000000000";

        // Act
        var result = CardHelper.GetBrandName(cardNumber);

        // Assert
        result.Should().Be("100", "bilinmeyen kart tipleri VISA'ya default olmalı");
    }

    [Fact]
    public void GetBrandName_CardWithSpaces_CleansAndReturnsCorrectCode()
    {
        // Arrange
        var cardNumber = "4111 1111 1111 1111";

        // Act
        var result = CardHelper.GetBrandName(cardNumber);

        // Assert
        result.Should().Be("100");
    }

    [Fact]
    public void GetBrandName_CardWithDashes_CleansAndReturnsCorrectCode()
    {
        // Arrange
        var cardNumber = "5500-0000-0000-0004";

        // Act
        var result = CardHelper.GetBrandName(cardNumber);

        // Assert
        result.Should().Be("200");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetBrandName_NullOrEmpty_ThrowsArgumentException(string? cardNumber)
    {
        // Act
        var act = () => CardHelper.GetBrandName(cardNumber!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region GetBrandDisplayName Tests

    [Theory]
    [InlineData("4111111111111111", "VISA")]
    [InlineData("5500000000000004", "MASTERCARD")]
    [InlineData("9000000000000000", "TROY")]
    [InlineData("3400000000000000", "AMERICAN EXPRESS")]
    public void GetBrandDisplayName_ValidCard_ReturnsReadableName(string cardNumber, string expectedName)
    {
        // Act
        var result = CardHelper.GetBrandDisplayName(cardNumber);

        // Assert
        result.Should().Be(expectedName);
    }

    #endregion

    #region IsValidCardNumber Tests

    [Theory]
    [InlineData("4539578763621486")]   // Geçerli VISA (Luhn valid)
    [InlineData("5425233430109903")]   // Geçerli MASTERCARD
    [InlineData("4111111111111111")]   // Test VISA kartı
    public void IsValidCardNumber_LuhnValidCard_ReturnsTrue(string cardNumber)
    {
        // Act
        var result = CardHelper.IsValidCardNumber(cardNumber);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("1234567890123456")]   // Luhn geçersiz
    [InlineData("1111111111111112")]   // Luhn geçersiz
    public void IsValidCardNumber_LuhnInvalidCard_ReturnsFalse(string cardNumber)
    {
        // Act
        var result = CardHelper.IsValidCardNumber(cardNumber);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidCardNumber_NullOrEmpty_ReturnsFalse(string? cardNumber)
    {
        // Act
        var result = CardHelper.IsValidCardNumber(cardNumber!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidCardNumber_TooShort_ReturnsFalse()
    {
        // Act
        var result = CardHelper.IsValidCardNumber("123456789012"); // 12 hane

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidCardNumber_TooLong_ReturnsFalse()
    {
        // Act
        var result = CardHelper.IsValidCardNumber("12345678901234567890"); // 20 hane

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidCardNumber_ContainsLetters_ReturnsFalse()
    {
        // Act
        var result = CardHelper.IsValidCardNumber("4111abcd11111111");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidCardNumber_WithSpaces_CleansAndValidates()
    {
        // Arrange - 4111 1111 1111 1111 Luhn-valid bir kart
        var result = CardHelper.IsValidCardNumber("4111 1111 1111 1111");

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region MaskCardNumber Tests

    [Fact]
    public void MaskCardNumber_StandardCard_MasksMiddleDigits()
    {
        // Arrange
        var cardNumber = "4111111111111111";

        // Act
        var result = CardHelper.MaskCardNumber(cardNumber);

        // Assert - Çıktı: "4111 11** **11" (ilk 6 görünür, ortada ****, son 4'ten 2'si görünür, 4'lü gruplara bölünür)
        result.Should().Be("4111 11** **11");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void MaskCardNumber_NullOrEmpty_ReturnsEmpty(string? cardNumber, string expected)
    {
        // Act
        var result = CardHelper.MaskCardNumber(cardNumber!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void MaskCardNumber_TooShort_ReturnsAsIs()
    {
        // Arrange - 12 haneden kısa kart
        var cardNumber = "12345678901";

        // Act
        var result = CardHelper.MaskCardNumber(cardNumber);

        // Assert
        result.Should().Be(cardNumber);
    }

    #endregion

    #region ConvertExpiryDateToYYMM Tests

    [Theory]
    [InlineData("01/26", "2601")]
    [InlineData("12/25", "2512")]
    [InlineData("3/30", "3003")]
    public void ConvertExpiryDateToYYMM_ValidDate_ReturnsYYMMFormat(string input, string expected)
    {
        // Act
        var result = CardHelper.ConvertExpiryDateToYYMM(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void ConvertExpiryDateToYYMM_NullOrEmpty_ReturnsEmpty(string? input, string expected)
    {
        // Act
        var result = CardHelper.ConvertExpiryDateToYYMM(input!);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ConvertExpiryDateToYYMM_InvalidFormat_ReturnsAsIs()
    {
        // Arrange - slash yok
        var input = "1226";

        // Act
        var result = CardHelper.ConvertExpiryDateToYYMM(input);

        // Assert
        result.Should().Be("1226", "slash içermeyen format olduğu gibi döner");
    }

    #endregion

    #region FormatAmount Tests

    [Theory]
    [InlineData(123.45, "123.45")]
    [InlineData(100, "100.00")]
    [InlineData(0.5, "0.50")]
    [InlineData(99999.99, "99999.99")]
    public void FormatAmount_ValidAmount_ReturnsInvariantCultureFormat(decimal amount, string expected)
    {
        // Act
        var result = CardHelper.FormatAmount(amount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
