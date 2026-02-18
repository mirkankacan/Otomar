using FluentAssertions;
using Otomar.Contracts.Common;
using System.Net;

namespace Otomar.UnitTests.Common;

public class ServiceResultTests
{
    #region Non-Generic ServiceResult

    [Fact]
    public void SuccessAsNoContent_ReturnsSuccessWithNoContentStatus()
    {
        // Act
        var result = ServiceResult.SuccessAsNoContent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFail.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        result.Fail.Should().BeNull();
    }

    [Fact]
    public void ErrorAsNotFound_ReturnsFailWithNotFoundStatus()
    {
        // Act
        var result = ServiceResult.ErrorAsNotFound();

        // Assert
        result.IsFail.Should().BeTrue();
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        result.Fail.Should().NotBeNull();
        result.Fail!.Title.Should().Be("Not Found");
    }

    [Fact]
    public void Error_WithTitleAndDescription_SetsAllProperties()
    {
        // Act
        var result = ServiceResult.Error("Bad Request", "Email alanı zorunludur", HttpStatusCode.BadRequest);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Bad Request");
        result.Fail.Detail.Should().Be("Email alanı zorunludur");
        result.Fail.Status.Should().Be(400);
    }

    [Fact]
    public void Error_WithTitleOnly_SetsWithoutDescription()
    {
        // Act
        var result = ServiceResult.Error("Unauthorized", HttpStatusCode.Unauthorized);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Fail!.Title.Should().Be("Unauthorized");
        result.Fail.Detail.Should().BeNull();
    }

    [Fact]
    public void ErrorFromValidation_SetsValidationErrors()
    {
        // Arrange
        var errors = new Dictionary<string, object>
        {
            { "Email", new[] { "Email alanı zorunludur" } },
            { "Password", new[] { "Şifre en az 6 karakter olmalıdır" } }
        };

        // Act
        var result = ServiceResult.ErrorFromValidation(errors);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Validation Error");
        result.Fail.Extensions.Should().ContainKey("Email");
        result.Fail.Extensions.Should().ContainKey("Password");
    }

    #endregion

    #region Generic ServiceResult<T>

    [Fact]
    public void SuccessAsOk_ReturnsSuccessWithData()
    {
        // Arrange
        var data = "test data";

        // Act
        var result = ServiceResult<string>.SuccessAsOk(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data.Should().Be("test data");
    }

    [Fact]
    public void SuccessAsCreated_ReturnsCreatedWithDataAndUrl()
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act
        var result = ServiceResult<Guid>.SuccessAsCreated(id, $"/api/orders/{id}");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data.Should().Be(id);
        result.UrlAsCreated.Should().Contain(id.ToString());
    }

    [Fact]
    public void GenericError_ReturnsFailWithNullData()
    {
        // Act
        var result = ServiceResult<string>.Error("Not Found", HttpStatusCode.NotFound);

        // Assert
        result.IsFail.Should().BeTrue();
        result.Data.Should().BeNull();
        result.Fail!.Title.Should().Be("Not Found");
    }

    #endregion
}
