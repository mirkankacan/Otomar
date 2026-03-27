using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Auth;
using Otomar.Domain.Entities;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Services;
using System.Net;
using System.Security.Claims;

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
    private readonly Mock<IUserRepository> _panelUserRepositoryMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
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
        _panelUserRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<AuthService>>();

        _sut = new AuthService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _jwtProviderMock.Object,
            _httpContextAccessorMock.Object,
            _panelUserRepositoryMock.Object,
            new Mock<IEmailService>().Object,
            _loggerMock.Object);
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
    public async Task LoginAsync_UserNotFound_ReturnsBadRequest()
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
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Fail!.Title.Should().Be("Giriş Başarısız");
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
    public async Task LoginAsync_WrongPassword_ReturnsBadRequest()
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
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_AuthenticatedUser_ClearsRefreshTokenAndReturnsSuccess()
    {
        // Arrange
        var userId = "user-123";
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@test.com",
            Name = "Test",
            RefreshToken = "old-refresh-token",
            RefreshTokenExpires = DateTime.UtcNow.AddDays(7)
        };
        _userManagerMock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.Is<ApplicationUser>(u => u.RefreshToken == null)), Times.Once);
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_NoUserIdInClaims_ReturnsUnauthorized()
    {
        // Arrange - HttpContext with no claims
        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task LogoutAsync_UserNotFoundInDb_StillSignsOutSuccessfully()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);
        _userManagerMock.Setup(x => x.FindByIdAsync("user-123")).ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _sut.LogoutAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        _signInManagerMock.Verify(x => x.SignOutAsync(), Times.Once);
    }

    #endregion

    #region RefreshTokenAsync Tests

    [Fact]
    public async Task RefreshTokenAsync_NullDto_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.RefreshTokenAsync(null!);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshTokenAsync_EmptyRefreshToken_ReturnsBadRequest()
    {
        // Act
        var result = await _sut.RefreshTokenAsync(new CreateTokenByRefreshTokenDto { RefreshToken = "" });

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_ReturnsNewToken()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@test.com",
            Name = "Test",
            IsActive = true,
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpires = DateTime.UtcNow.AddDays(7)
        };
        var users = new List<ApplicationUser> { user }.AsQueryable();
        _userManagerMock.Setup(x => x.Users).Returns(users);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(IdentityResult.Success);

        var newToken = new TokenDto { Token = "new-jwt-token" };
        _jwtProviderMock.Setup(x => x.CreateTokenAsync(user)).ReturnsAsync(ServiceResult<TokenDto>.SuccessAsOk(newToken));

        // Act
        var result = await _sut.RefreshTokenAsync(new CreateTokenByRefreshTokenDto { RefreshToken = "valid-refresh-token" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Token.Should().Be("new-jwt-token");
    }

    [Fact]
    public async Task RefreshTokenAsync_TokenNotFound_ReturnsUnauthorized()
    {
        // Arrange
        var users = new List<ApplicationUser>().AsQueryable();
        _userManagerMock.Setup(x => x.Users).Returns(users);

        // Act
        var result = await _sut.RefreshTokenAsync(new CreateTokenByRefreshTokenDto { RefreshToken = "non-existent" });

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshTokenAsync_InactiveUser_ReturnsForbidden()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@test.com",
            Name = "Test",
            IsActive = false,
            RefreshToken = "valid-refresh-token",
            RefreshTokenExpires = DateTime.UtcNow.AddDays(7)
        };
        _userManagerMock.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable());

        // Act
        var result = await _sut.RefreshTokenAsync(new CreateTokenByRefreshTokenDto { RefreshToken = "valid-refresh-token" });

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RefreshTokenAsync_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = "user-1",
            Email = "test@test.com",
            Name = "Test",
            IsActive = true,
            RefreshToken = "expired-token",
            RefreshTokenExpires = DateTime.UtcNow.AddDays(-1) // Expired
        };
        _userManagerMock.Setup(x => x.Users).Returns(new List<ApplicationUser> { user }.AsQueryable());

        // Act
        var result = await _sut.RefreshTokenAsync(new CreateTokenByRefreshTokenDto { RefreshToken = "expired-token" });

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
