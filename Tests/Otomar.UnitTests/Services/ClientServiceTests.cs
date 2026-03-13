using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Services;
using Otomar.Shared.Dtos.Client;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// ClientService testleri - Input validasyonları.
/// </summary>
public class ClientServiceTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<ILogger<ClientService>> _loggerMock;
    private readonly ClientService _sut;

    public ClientServiceTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _loggerMock = new Mock<ILogger<ClientService>>();
        _sut = new ClientService(_clientRepositoryMock.Object, _loggerMock.Object);
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

    #region GetClientByCodeAsync - Happy Path Tests

    [Fact]
    public async Task GetClientByCodeAsync_ValidCode_ReturnsClient()
    {
        // Arrange
        var client = new ClientDto { CARI_KOD = "C001", CARI_ISIM_TRK = "Test Cari" };
        _clientRepositoryMock.Setup(x => x.GetByCodeAsync("C001")).ReturnsAsync(client);

        // Act
        var result = await _sut.GetClientByCodeAsync("C001");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.CARI_KOD.Should().Be("C001");
    }

    [Fact]
    public async Task GetClientByCodeAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _clientRepositoryMock.Setup(x => x.GetByCodeAsync("C999")).ReturnsAsync((ClientDto?)null);

        // Act
        var result = await _sut.GetClientByCodeAsync("C999");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetClientByTaxTcNumberAsync - Happy Path Tests

    [Fact]
    public async Task GetClientByTaxTcNumberAsync_TaxNumber10Chars_ReturnsClient()
    {
        // Arrange
        var client = new ClientDto { CARI_KOD = "C001", VERGI_NUMARASI = "1234567890" };
        _clientRepositoryMock.Setup(x => x.GetByTaxNumberAsync("1234567890")).ReturnsAsync(client);

        // Act
        var result = await _sut.GetClientByTaxTcNumberAsync("1234567890");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.VERGI_NUMARASI.Should().Be("1234567890");
    }

    [Fact]
    public async Task GetClientByTaxTcNumberAsync_TcNumber11Chars_ReturnsClient()
    {
        // Arrange
        var client = new ClientDto { CARI_KOD = "C001", TCKIMLIKNO = "12345678901" };
        _clientRepositoryMock.Setup(x => x.GetByTcNumberAsync("12345678901")).ReturnsAsync(client);

        // Act
        var result = await _sut.GetClientByTaxTcNumberAsync("12345678901");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.TCKIMLIKNO.Should().Be("12345678901");
    }

    [Fact]
    public async Task GetClientByTaxTcNumberAsync_InvalidLength_ReturnsNotFound()
    {
        // Act - 5 char string, neither 10 nor 11
        var result = await _sut.GetClientByTaxTcNumberAsync("12345");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClientByTaxTcNumberAsync_NotFound_ReturnsNotFound()
    {
        // Arrange
        _clientRepositoryMock.Setup(x => x.GetByTaxNumberAsync("9999999999")).ReturnsAsync((ClientDto?)null);

        // Act
        var result = await _sut.GetClientByTaxTcNumberAsync("9999999999");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetClientTransactionsByCodeAsync - Happy Path Tests

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_HasTransactions_ReturnsOk()
    {
        // Arrange
        var transactions = new List<TransactionDto>
        {
            new() { BELGE_NO = "BLG-001", ACIKLAMA = "Test", BORC = "100", ALACAK = "0", BAKIYE = "100" }
        };
        _clientRepositoryMock.Setup(x => x.GetTransactionsByCodeAsync("C001")).ReturnsAsync(transactions);

        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync("C001");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_Empty_ReturnsNotFound()
    {
        // Arrange
        _clientRepositoryMock.Setup(x => x.GetTransactionsByCodeAsync("C001"))
            .ReturnsAsync(Enumerable.Empty<TransactionDto>());

        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync("C001");

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_Paged_HasTransactions_ReturnsOk()
    {
        // Arrange
        var transactions = new List<TransactionDto>
        {
            new() { BELGE_NO = "BLG-001", ACIKLAMA = "Test" }
        };
        _clientRepositoryMock.Setup(x => x.GetTransactionsByCodeAsync("C001", 1, 10)).ReturnsAsync(transactions);

        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync("C001", 1, 10);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetClientTransactionsByCodeAsync_Paged_Empty_ReturnsNotFound()
    {
        // Arrange
        _clientRepositoryMock.Setup(x => x.GetTransactionsByCodeAsync("C001", 1, 10))
            .ReturnsAsync(Enumerable.Empty<TransactionDto>());

        // Act
        var result = await _sut.GetClientTransactionsByCodeAsync("C001", 1, 10);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
