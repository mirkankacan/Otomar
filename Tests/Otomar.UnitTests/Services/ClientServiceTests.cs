using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Persistance.Data;
using Otomar.Persistance.Services;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// ClientService testleri - Input validasyonları.
/// </summary>
public class ClientServiceTests
{
    private readonly Mock<IAppDbContext> _contextMock;
    private readonly Mock<ILogger<ClientService>> _loggerMock;
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        _contextMock = new Mock<IAppDbContext>();
        _loggerMock = new Mock<ILogger<ClientService>>();
        _sut = new ClientService(_contextMock.Object, _loggerMock.Object);
    }

    #region GetClientByCodeAsync - Validation Tests

    [Fact]
    public async Task GetClientByCodeAsync_EmptyCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientByCodeAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Detail.Should().Contain("boş");
    }

    [Fact]
    public async Task GetClientByCodeAsync_NullCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientByCodeAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetClientByTaxTcNumberAsync - Validation Tests

    [Fact]
    public async Task GetClientByTaxTcNumberAsync_EmptyNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientByTaxTcNumberAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClientByTaxTcNumberAsync_NullNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientByTaxTcNumberAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetClientTransactionsByCodeAsync - Validation Tests

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_EmptyCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_NullCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_Paged_EmptyCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync("", 1, 10);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_Paged_NullCode_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync(null!, 1, 10);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
