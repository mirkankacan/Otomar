using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Contracts.Services;
using Otomar.Application.Contracts.Persistence;
using Otomar.Application.Contracts.Persistence.Repositories;
using Otomar.Application.Services;
using Otomar.Shared.Dtos.ListSearch;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// ListSearchService testleri - Input validasyonları.
/// </summary>
public class ListSearchServiceTests
{
    private readonly Mock<IListSearchRepository> _listSearchRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ListSearchService>> _loggerMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IFileService> _fileServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly ListSearchService _sut;

    public ListSearchServiceTests()
    {
        _listSearchRepositoryMock = new Mock<IListSearchRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ListSearchService>>();
        _identityServiceMock = new Mock<IIdentityService>();
        _fileServiceMock = new Mock<IFileService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _emailServiceMock = new Mock<IEmailService>();

        _sut = new ListSearchService(
            _loggerMock.Object,
            _listSearchRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _identityServiceMock.Object,
            _fileServiceMock.Object,
            _notificationServiceMock.Object,
            _emailServiceMock.Object);
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
        // Act
        var result = await _sut.CreateListSearchAnswerAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateListSearchAnswerAsync_EmptyList_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.CreateListSearchAnswerAsync(new List<CreateListSearchAnswerDto>());

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GetListSearchByRequestNoAsync - Happy Path Tests

    [Fact]
    public async Task GetListSearchByRequestNoAsync_Found_ReturnsOk()
    {
        // Arrange
        var listSearch = new ListSearchDto { Id = Guid.NewGuid(), RequestNo = "OTOMAR-ABC12345" };
        _listSearchRepositoryMock.Setup(x => x.GetByRequestNoAsync("OTOMAR-ABC12345")).ReturnsAsync(listSearch);

        // Act
        var result = await _sut.GetListSearchByRequestNoAsync("OTOMAR-ABC12345");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.RequestNo.Should().Be("OTOMAR-ABC12345");
    }

    [Fact]
    public async Task GetListSearchByRequestNoAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _listSearchRepositoryMock.Setup(x => x.GetByRequestNoAsync("OTOMAR-INVALID")).ReturnsAsync((ListSearchDto?)null);

        // Act
        var result = await _sut.GetListSearchByRequestNoAsync("OTOMAR-INVALID");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetListSearchByIdAsync Tests

    [Fact]
    public async Task GetListSearchByIdAsync_Found_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var listSearch = new ListSearchDto { Id = id, RequestNo = "OTOMAR-TEST" };
        _listSearchRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync(listSearch);

        // Act
        var result = await _sut.GetListSearchByIdAsync(id);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetListSearchByIdAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _listSearchRepositoryMock.Setup(x => x.GetByIdAsync(id)).ReturnsAsync((ListSearchDto?)null);

        // Act
        var result = await _sut.GetListSearchByIdAsync(id);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetListSearchesAsync Tests

    [Fact]
    public async Task GetListSearchesAsync_NoPaging_ReturnsList()
    {
        // Arrange
        var listSearches = new List<ListSearchDto> { new() { Id = Guid.NewGuid(), RequestNo = "OTOMAR-001" } };
        _listSearchRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(listSearches);

        // Act
        var result = await _sut.GetListSearchesAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetListSearchesAsync_Paged_ReturnsList()
    {
        // Arrange
        var listSearches = new List<ListSearchDto> { new() { Id = Guid.NewGuid(), RequestNo = "OTOMAR-001" } };
        _listSearchRepositoryMock.Setup(x => x.GetPagedAsync(0, 10)).ReturnsAsync(listSearches);

        // Act
        var result = await _sut.GetListSearchesAsync(1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    #endregion

    #region GetListSearchesByUserAsync - Happy Path Tests

    [Fact]
    public async Task GetListSearchesByUserAsync_HasResults_ReturnsOk()
    {
        // Arrange
        var listSearches = new List<ListSearchDto> { new() { Id = Guid.NewGuid(), RequestNo = "OTOMAR-001" } };
        _listSearchRepositoryMock.Setup(x => x.GetByUserAsync("user-1")).ReturnsAsync(listSearches);

        // Act
        var result = await _sut.GetListSearchesByUserAsync("user-1");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    #endregion
}
