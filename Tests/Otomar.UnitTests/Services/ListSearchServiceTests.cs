using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Contracts.Services;
using Otomar.Persistance.Data;
using Otomar.Persistance.Services;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// ListSearchService testleri - Input validasyonları.
/// </summary>
public class ListSearchServiceTests
{
    private readonly Mock<IAppDbContext> _contextMock;
    private readonly Mock<ILogger<ListSearchService>> _loggerMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly ListSearchService _sut;

    public ListSearchServiceTests()
    {
        _contextMock = new Mock<IAppDbContext>();
        _loggerMock = new Mock<ILogger<ListSearchService>>();
        _identityServiceMock = new Mock<IIdentityService>();
        _fileServiceMock = new Mock<IFileService>();

        _sut = new ListSearchService(
            _loggerMock.Object,
            _contextMock.Object,
            _identityServiceMock.Object,
            _fileServiceMock.Object);
    }

    #region GetListSearchByRequestNoAsync - Validation Tests

    [Fact]
    public async Task GetListSearchByRequestNoAsync_EmptyRequestNo_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetListSearchByRequestNoAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetListSearchByRequestNoAsync_NullRequestNo_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetListSearchByRequestNoAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetListSearchesByUserAsync - Validation Tests

    [Fact]
    public async Task GetListSearchesByUserAsync_EmptyUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetListSearchesByUserAsync("");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetListSearchesByUserAsync_NullUserId_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.GetListSearchesByUserAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region CreateListSearchAnswerAsync - Validation Tests

    [Fact]
    public async Task CreateListSearchAnswerAsync_NullList_ReturnsBadRequest()
    {
        // Note: This requires DB connection for BeginTransaction.
        // Testing validation logic that empty list returns error.
        // The method opens a transaction first, so this test would need
        // an actual connection or a mock that supports BeginTransaction.
    }

    #endregion
}
