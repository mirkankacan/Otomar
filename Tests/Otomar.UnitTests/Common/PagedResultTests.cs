using FluentAssertions;
using Otomar.Contracts.Common;

namespace Otomar.UnitTests.Common;

public class PagedResultTests
{
    [Fact]
    public void Constructor_WithParameters_SetsAllProperties()
    {
        // Arrange
        var items = new List<string> { "item1", "item2", "item3" };

        // Act
        var result = new PagedResult<string>(items, pageNumber: 1, pageSize: 10, totalCount: 25);

        // Assert
        result.Data.Should().HaveCount(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(25);
    }

    [Theory]
    [InlineData(25, 10, 3)]   // 25 kayıt, 10'arlı = 3 sayfa
    [InlineData(30, 10, 3)]   // 30 kayıt, 10'arlı = tam 3 sayfa
    [InlineData(1, 10, 1)]    // 1 kayıt = 1 sayfa
    [InlineData(100, 25, 4)]  // 100 kayıt, 25'erli = 4 sayfa
    public void TotalPages_CalculatesCorrectly(int totalCount, int pageSize, int expectedPages)
    {
        // Act
        var result = new PagedResult<string>([], 1, pageSize, totalCount);

        // Assert
        result.TotalPages.Should().Be(expectedPages);
    }

    [Fact]
    public void HasPrevious_FirstPage_ReturnsFalse()
    {
        // Act
        var result = new PagedResult<string>([], pageNumber: 1, pageSize: 10, totalCount: 50);

        // Assert
        result.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public void HasPrevious_SecondPage_ReturnsTrue()
    {
        // Act
        var result = new PagedResult<string>([], pageNumber: 2, pageSize: 10, totalCount: 50);

        // Assert
        result.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public void HasNext_LastPage_ReturnsFalse()
    {
        // Arrange - 50 kayıt, 10'arlı = 5 sayfa, 5. sayfadayız
        var result = new PagedResult<string>([], pageNumber: 5, pageSize: 10, totalCount: 50);

        // Assert
        result.HasNext.Should().BeFalse();
    }

    [Fact]
    public void HasNext_MiddlePage_ReturnsTrue()
    {
        // Arrange - 50 kayıt, 10'arlı = 5 sayfa, 3. sayfadayız
        var result = new PagedResult<string>([], pageNumber: 3, pageSize: 10, totalCount: 50);

        // Assert
        result.HasNext.Should().BeTrue();
    }

    [Fact]
    public void EmptyResult_HasCorrectDefaults()
    {
        // Act
        var result = new PagedResult<string>([], pageNumber: 1, pageSize: 10, totalCount: 0);

        // Assert
        result.Data.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
        result.HasPrevious.Should().BeFalse();
        result.HasNext.Should().BeFalse();
    }
}
