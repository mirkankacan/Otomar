using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Otomar.Application.Contracts.Providers;
using Otomar.Contracts.Common;
using Otomar.Contracts.Dtos.Auth;
using Otomar.Domain.Entities;
using Otomar.Persistance.Services;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// AuthService testleri - Gerçek dünya mock senaryosu.
/// UserManager ve SignInManager gibi karmaşık bağımlılıkların mock'lanmasını gösterir.
/// </summary>
public class AuthServiceTests
{
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private readonly Mock<IJwtProvider> _jwtProviderMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        // UserManager mock'u - abstract class olduğu için özel setup gerekir
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // SignInManager mock'u - UserManager'a bağımlı
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            null!, null!, null!, null!);

        _jwtProviderMock = new Mock<IJwtProvider>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        _sut = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtProviderMock.Object,
            _httpContextAccessorMock.Object);
    }

    #region LoginAsync Tests

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@otomar.com", Password = "Test123!" };
        var user = new ApplicationUser { Email = loginDto.Email, Name = "Test", IsActive = true };
        var expectedToken = new TokenDto { Token = "jwt-token-here" };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(SignInResult.Success);

        _jwtProviderMock
            .Setup(x => x.CreateTokenAsync(user))
            .ReturnsAsync(ServiceResult<TokenDto>.SuccessAsOk(expectedToken));

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().Be("jwt-token-here");
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "unknown@test.com", Password = "Test123!" };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        result.Fail!.Title.Should().Be("Kullanıcı Bulunamadı");
    }

    [Fact]
    public async Task LoginAsync_InactiveUser_ReturnsForbidden()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "inactive@test.com", Password = "Test123!" };
        var user = new ApplicationUser { Email = loginDto.Email, Name = "Test", IsActive = false };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        result.Fail!.Title.Should().Be("Hesap Pasif");
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ReturnsUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@otomar.com", Password = "WrongPass" };
        var user = new ApplicationUser { Email = loginDto.Email, Name = "Test", IsActive = true };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(loginDto.Email))
            .ReturnsAsync(user);

        _signInManagerMock
            .Setup(x => x.CheckPasswordSignInAsync(user, loginDto.Password, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LoginAsync_NullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.LoginAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("", "Test123!")]
    [InlineData("test@test.com", "")]
    [InlineData("  ", "Test123!")]
    public async Task LoginAsync_EmptyEmailOrPassword_ReturnsBadRequest(string email, string password)
    {
        // Arrange
        var loginDto = new LoginDto { Email = email, Password = password };

        // Act
        var result = await _sut.LoginAsync(loginDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region RegisterAsync Tests

    [Fact]
    public async Task RegisterAsync_ValidData_ReturnsToken()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "Ahmet",
            Surname = "Yılmaz",
            Email = "ahmet@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!",
            PhoneNumber = "+905551234567"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((ApplicationUser?)null); // Mevcut kullanıcı yok

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Success);

        var expectedToken = new TokenDto { Token = "new-user-token" };
        _jwtProviderMock
            .Setup(x => x.CreateTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(ServiceResult<TokenDto>.SuccessAsOk(expectedToken));

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().Be("new-user-token");
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "Ahmet",
            Surname = "Yılmaz",
            Email = "existing@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        var existingUser = new ApplicationUser { Email = registerDto.Email, Name = "Existing" };
        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
        result.Fail!.Title.Should().Be("Kullanıcı Mevcut");
    }

    [Fact]
    public async Task RegisterAsync_PasswordMismatch_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "Ahmet",
            Surname = "Yılmaz",
            Email = "ahmet@test.com",
            Password = "Test123!",
            ConfirmPassword = "DifferentPassword!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Şifre Uyuşmazlığı");
    }

    [Fact]
    public async Task RegisterAsync_NullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.RegisterAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterAsync_MissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange - Name boş
        var registerDto = new RegisterDto
        {
            Name = "",
            Surname = "Yılmaz",
            Email = "test@test.com",
            Password = "Test123!",
            ConfirmPassword = "Test123!"
        };

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Eksik Bilgi");
    }

    [Fact]
    public async Task RegisterAsync_IdentityFailure_ReturnsBadRequest()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Name = "Ahmet",
            Surname = "Yılmaz",
            Email = "ahmet@test.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        _userManagerMock
            .Setup(x => x.FindByEmailAsync(registerDto.Email))
            .ReturnsAsync((ApplicationUser?)null);

        _userManagerMock
            .Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), registerDto.Password))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Şifre en az 6 karakter olmalıdır" }));

        // Act
        var result = await _sut.RegisterAsync(registerDto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Kayıt Başarısız");
    }

    #endregion

    #region ResetPasswordAsync Tests

    [Fact]
    public async Task ResetPasswordAsync_Always_ReturnsNotImplemented()
    {
        // Act
        var result = await _sut.ResetPasswordAsync(new ResetPasswordDto());

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    #endregion
}
