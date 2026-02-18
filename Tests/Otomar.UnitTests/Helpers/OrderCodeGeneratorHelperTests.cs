using FluentAssertions;
using Otomar.Persistance.Helpers;

namespace Otomar.UnitTests.Helpers;

public class OrderCodeGeneratorHelperTests
{
    [Fact]
    public void Generate_Always_StartsWithOtomar()
    {
        // Act
        var result = OrderCodeGeneratorHelper.Generate();

        // Assert
        result.Should().StartWith("OTOMAR-");
    }

    [Fact]
    public void Generate_Always_HasThreeParts()
    {
        // Act
        var result = OrderCodeGeneratorHelper.Generate();

        // Assert
        var parts = result.Split('-');
        parts.Should().HaveCount(3, "format: OTOMAR-{timestamp}-{random}");
    }

    [Fact]
    public void Generate_TimestampPart_Has14Digits()
    {
        // Act
        var result = OrderCodeGeneratorHelper.Generate();
        var timestampPart = result.Split('-')[1];

        // Assert
        timestampPart.Should().HaveLength(14, "yyyyMMddHHmmss = 14 karakter");
        timestampPart.Should().MatchRegex(@"^\d{14}$");
    }

    [Fact]
    public void Generate_RandomPart_IsFiveDigitNumber()
    {
        // Act
        var result = OrderCodeGeneratorHelper.Generate();
        var randomPart = result.Split('-')[2];

        // Assert
        randomPart.Should().MatchRegex(@"^\d{5}$");
        int.Parse(randomPart).Should().BeInRange(10000, 99999);
    }

    [Fact]
    public void Generate_CalledTwice_ProducesDifferentCodes()
    {
        // Act
        var code1 = OrderCodeGeneratorHelper.Generate();
        var code2 = OrderCodeGeneratorHelper.Generate();

        // Assert - aynı milisaniyede bile random kısım farklı olmalı (çok düşük ihtimal aynı olur)
        // Bu test nadir durumlarda fail edebilir ama pratikte her zaman geçer
        code1.Should().NotBe(code2);
    }
}
