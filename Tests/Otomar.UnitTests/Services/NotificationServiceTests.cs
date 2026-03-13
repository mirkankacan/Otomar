using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Otomar.Application.Interfaces.Repositories;
using Otomar.Application.Interfaces.Services;
using Otomar.Shared.Interfaces;
using Otomar.Application.Services;
using Otomar.Shared.Common;
using Otomar.Shared.Dtos.Notification;
using Otomar.Shared.Enums;
using System.Net;

namespace Otomar.UnitTests.Services;

/// <summary>
/// NotificationService unit tests.
/// Covers all five public methods: CreateNotificationAsync, GetNotificationsByUserAsync,
/// GetUnreadCountAsync, MarkAsReadAsync, MarkAllAsReadAsync.
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly Mock<INotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IIdentityService> _identityServiceMock;
    private readonly Mock<IRealtimeNotifier> _realtimeNotifierMock;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _notificationRepositoryMock = new Mock<INotificationRepository>();
        _identityServiceMock = new Mock<IIdentityService>();
        _realtimeNotifierMock = new Mock<IRealtimeNotifier>();

        _identityServiceMock
            .Setup(x => x.GetUserId())
            .Returns("admin-user-id");

        _sut = new NotificationService(
            _loggerMock.Object,
            _notificationRepositoryMock.Object,
            _identityServiceMock.Object,
            _realtimeNotifierMock.Object);
    }

    #region CreateNotificationAsync Tests

    [Fact]
    public async Task CreateNotificationAsync_NoTargetSpecified_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.Info,
            TargetUserId = null,
            TargetRoleName = null
        };

        // Act
        var result = await _sut.CreateNotificationAsync(dto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateNotificationAsync_EmptyTargets_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.Info,
            TargetUserId = "",
            TargetRoleName = ""
        };

        // Act
        var result = await _sut.CreateNotificationAsync(dto);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateNotificationAsync_TargetUserIdSet_CreatesOneNotification()
    {
        // Arrange
        var targetUserId = "target-user-123";
        var dto = new CreateNotificationDto
        {
            Title = "Sipariş Onaylandı",
            Message = "Siparişiniz onaylandı.",
            Type = NotificationType.Info,
            RedirectUrl = "/orders/1",
            TargetUserId = targetUserId
        };

        // Act
        var result = await _sut.CreateNotificationAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data.Should().HaveCount(1);
        result.Data![0].UserId.Should().Be(targetUserId);
        result.Data[0].Title.Should().Be(dto.Title);
        result.Data[0].Message.Should().Be(dto.Message);
        result.Data[0].Type.Should().Be(dto.Type);
        result.Data[0].RedirectUrl.Should().Be(dto.RedirectUrl);
        result.Data[0].IsRead.Should().BeFalse();

        _notificationRepositoryMock.Verify(
            x => x.InsertNotificationAsync(
                It.IsAny<Guid>(), targetUserId, dto.Title, dto.Message, dto.Type, dto.RedirectUrl, It.IsAny<DateTime>(), "admin-user-id"),
            Times.Once);

        _realtimeNotifierMock.Verify(
            x => x.SendNotificationsAsync(It.Is<List<NotificationDto>>(n => n.Count == 1)),
            Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_TargetRoleNameSet_CreatesNotificationsForAllUsers()
    {
        // Arrange
        var roleUserIds = new List<string> { "user-1", "user-2" };
        var dto = new CreateNotificationDto
        {
            Title = "Duyuru",
            Message = "Yeni kampanya başladı.",
            Type = NotificationType.Info,
            TargetRoleName = "Customer"
        };

        _notificationRepositoryMock
            .Setup(x => x.GetUserIdsByRoleAsync("CUSTOMER"))
            .ReturnsAsync(roleUserIds);

        // Act
        var result = await _sut.CreateNotificationAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data.Should().HaveCount(2);
        result.Data![0].UserId.Should().Be("user-1");
        result.Data[1].UserId.Should().Be("user-2");

        _notificationRepositoryMock.Verify(
            x => x.GetUserIdsByRoleAsync("CUSTOMER"),
            Times.Once);

        _notificationRepositoryMock.Verify(
            x => x.InsertNotificationAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), dto.Title, dto.Message, dto.Type, dto.RedirectUrl, It.IsAny<DateTime>(), "admin-user-id"),
            Times.Exactly(2));

        _realtimeNotifierMock.Verify(
            x => x.SendNotificationsAsync(It.Is<List<NotificationDto>>(n => n.Count == 2)),
            Times.Once);
    }

    [Fact]
    public async Task CreateNotificationAsync_NoUsersFoundForRole_ReturnsEmptyCreatedResult()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            Title = "Duyuru",
            Message = "Mesaj",
            Type = NotificationType.Info,
            TargetRoleName = "NonExistentRole"
        };

        _notificationRepositoryMock
            .Setup(x => x.GetUserIdsByRoleAsync("NONEXISTENTROLE"))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _sut.CreateNotificationAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data.Should().BeEmpty();

        _notificationRepositoryMock.Verify(
            x => x.InsertNotificationAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<NotificationType>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<string>()),
            Times.Never);

        _realtimeNotifierMock.Verify(
            x => x.SendNotificationsAsync(It.IsAny<List<NotificationDto>>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateNotificationAsync_RealtimeNotifierThrows_StillReturnsSuccess()
    {
        // Arrange
        var dto = new CreateNotificationDto
        {
            Title = "Test",
            Message = "Test message",
            Type = NotificationType.Info,
            TargetUserId = "user-1"
        };

        _realtimeNotifierMock
            .Setup(x => x.SendNotificationsAsync(It.IsAny<List<NotificationDto>>()))
            .ThrowsAsync(new InvalidOperationException("SignalR connection failed"));

        // Act
        var result = await _sut.CreateNotificationAsync(dto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Data.Should().HaveCount(1);
    }

    #endregion

    #region GetNotificationsByUserAsync Tests

    [Fact]
    public async Task GetNotificationsByUserAsync_ReturnsPagedNotifications()
    {
        // Arrange
        var userId = "user-123";
        var pageNumber = 2;
        var pageSize = 10;
        var expectedOffset = 10; // (2 - 1) * 10

        var notifications = new List<NotificationDto>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, Title = "Bildirim 1", Message = "Mesaj 1", Type = NotificationType.Info, CreatedAt = DateTime.Now },
            new() { Id = Guid.NewGuid(), UserId = userId, Title = "Bildirim 2", Message = "Mesaj 2", Type = NotificationType.Info, CreatedAt = DateTime.Now }
        };
        var totalCount = 25;

        _notificationRepositoryMock
            .Setup(x => x.GetNotificationsByUserAsync(userId, expectedOffset, pageSize))
            .ReturnsAsync((notifications.AsEnumerable(), totalCount));

        // Act
        var result = await _sut.GetNotificationsByUserAsync(userId, pageNumber, pageSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data!.Data.Should().HaveCount(2);
        result.Data.TotalCount.Should().Be(totalCount);
        result.Data.PageNumber.Should().Be(pageNumber);
        result.Data.PageSize.Should().Be(pageSize);
    }

    [Fact]
    public async Task GetNotificationsByUserAsync_FirstPage_CalculatesOffsetCorrectly()
    {
        // Arrange
        var userId = "user-123";
        var pageNumber = 1;
        var pageSize = 20;
        var expectedOffset = 0; // (1 - 1) * 20

        _notificationRepositoryMock
            .Setup(x => x.GetNotificationsByUserAsync(userId, expectedOffset, pageSize))
            .ReturnsAsync((Enumerable.Empty<NotificationDto>(), 0));

        // Act
        var result = await _sut.GetNotificationsByUserAsync(userId, pageNumber, pageSize);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Data.Should().BeEmpty();
        result.Data.TotalCount.Should().Be(0);

        _notificationRepositoryMock.Verify(
            x => x.GetNotificationsByUserAsync(userId, 0, pageSize),
            Times.Once);
    }

    #endregion

    #region GetUnreadCountAsync Tests

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsUnreadCount()
    {
        // Arrange
        var userId = "user-123";
        var expectedCount = 7;

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _sut.GetUnreadCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Data!.Count.Should().Be(expectedCount);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoUnread_ReturnsZero()
    {
        // Arrange
        var userId = "user-123";

        _notificationRepositoryMock
            .Setup(x => x.GetUnreadCountAsync(userId))
            .ReturnsAsync(0);

        // Act
        var result = await _sut.GetUnreadCountAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.Count.Should().Be(0);
    }

    #endregion

    #region MarkAsReadAsync Tests

    [Fact]
    public async Task MarkAsReadAsync_NotificationFound_ReturnsNoContent()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var userId = "user-123";

        _notificationRepositoryMock
            .Setup(x => x.MarkAsReadAsync(notificationId, userId, It.IsAny<DateTime>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.MarkAsReadAsync(notificationId, userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task MarkAsReadAsync_NotificationNotFound_ReturnsNotFound()
    {
        // Arrange
        var notificationId = Guid.NewGuid();
        var userId = "user-123";

        _notificationRepositoryMock
            .Setup(x => x.MarkAsReadAsync(notificationId, userId, It.IsAny<DateTime>()))
            .ReturnsAsync(0);

        // Act
        var result = await _sut.MarkAsReadAsync(notificationId, userId);

        // Assert
        result.IsFail.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region MarkAllAsReadAsync Tests

    [Fact]
    public async Task MarkAllAsReadAsync_ReturnsNoContent()
    {
        // Arrange
        var userId = "user-123";

        _notificationRepositoryMock
            .Setup(x => x.MarkAllAsReadAsync(userId, It.IsAny<DateTime>()));

        // Act
        var result = await _sut.MarkAllAsReadAsync(userId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(HttpStatusCode.NoContent);

        _notificationRepositoryMock.Verify(
            x => x.MarkAllAsReadAsync(userId, It.IsAny<DateTime>()),
            Times.Once);
    }

    #endregion
}
