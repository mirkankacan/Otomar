using FluentAssertions;
using Otomar.WebApp.Helpers;

namespace Otomar.UnitTests.Helpers;

/// <summary>
/// SlugHelper testleri - Türkçe karakter dönüşümü ve slug oluşturma.
/// </summary>
public class SlugHelperTests
{
    #region Generate Tests

    [Fact]
    public void Generate_NullInput_ReturnsEmpty()
    {
        SlugHelper.Generate(null!).Should().BeEmpty();
    }

    [Fact]
    public void Generate_EmptyInput_ReturnsEmpty()
    {
        SlugHelper.Generate("").Should().BeEmpty();
    }

    [Fact]
    public void Generate_WhitespaceInput_ReturnsEmpty()
    {
        SlugHelper.Generate("   ").Should().BeEmpty();
    }

    [Theory]
    [InlineData("ç", "c")]
    [InlineData("Ç", "c")]
    [InlineData("ğ", "g")]
    [InlineData("Ğ", "g")]
    [InlineData("ı", "i")]
    [InlineData("İ", "i")]
    [InlineData("ö", "o")]
    [InlineData("Ö", "o")]
    [InlineData("ş", "s")]
    [InlineData("Ş", "s")]
    [InlineData("ü", "u")]
    [InlineData("Ü", "u")]
    public void Generate_TurkishCharacters_ConvertsCorrectly(string input, string expected)
    {
        SlugHelper.Generate(input).Should().Be(expected);
    }

    [Fact]
    public void Generate_SpacesConvertedToHyphens()
    {
        SlugHelper.Generate("ön tampon sağ").Should().Be("on-tampon-sag");
    }

    [Fact]
    public void Generate_MultipleSpacesConvertedToSingleHyphen()
    {
        SlugHelper.Generate("ön   tampon   sağ").Should().Be("on-tampon-sag");
    }

    [Fact]
    public void Generate_SpecialCharactersRemoved()
    {
        SlugHelper.Generate("test!@#$%^&*()product").Should().Be("testproduct");
    }

    [Fact]
    public void Generate_UpperCaseConvertedToLower()
    {
        SlugHelper.Generate("TOYOTA COROLLA").Should().Be("toyota-corolla");
    }

    [Fact]
    public void Generate_ComplexTurkishText_GeneratesCorrectSlug()
    {
        SlugHelper.Generate("Ön Çamurluk Sağ Üst").Should().Be("on-camurluk-sag-ust");
    }

    [Fact]
    public void Generate_LeadingTrailingSpaces_Trimmed()
    {
        SlugHelper.Generate("  test  ").Should().Be("test");
    }

    #endregion

    #region ToTitle Tests

    [Fact]
    public void ToTitle_NullInput_ReturnsEmpty()
    {
        SlugHelper.ToTitle(null!).Should().BeEmpty();
    }

    [Fact]
    public void ToTitle_EmptyInput_ReturnsEmpty()
    {
        SlugHelper.ToTitle("").Should().BeEmpty();
    }

    [Fact]
    public void ToTitle_SimpleSlug_ReturnsTitleCase()
    {
        SlugHelper.ToTitle("on-tampon-sag").Should().Be("On Tampon Sag");
    }

    [Fact]
    public void ToTitle_SingleWord_ReturnsTitleCase()
    {
        SlugHelper.ToTitle("toyota").Should().Be("Toyota");
    }

    [Fact]
    public void ToTitle_MultiWordSlug_EachWordCapitalized()
    {
        SlugHelper.ToTitle("on-camurluk-sag-ust").Should().Be("On Camurluk Sag Ust");
    }

    #endregion
}
