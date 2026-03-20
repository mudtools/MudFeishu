// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManageDemo.Backend.Data;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Models.Entities;
using TaskManageDemo.Backend.Services.Auth;

namespace TaskManageDemo.Backend.Tests.Services;

/// <summary>
/// 认证服务测试
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly TaskManageDbContext _dbContext;
    private readonly Mock<IPermissionService> _permissionServiceMock;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var options = new DbContextOptionsBuilder<TaskManageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskManageDbContext(options);
        _permissionServiceMock = new Mock<IPermissionService>();
        _sut = new AuthService(_dbContext, _permissionServiceMock.Object, Mock.Of<ILogger<AuthService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetUserByFeishuIdAsync_ShouldReturnNull_WhenUserNotFound()
    {
        var result = await _sut.GetUserByFeishuIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByFeishuIdAsync_ShouldReturnUserInfo_WhenUserExists()
    {
        var user = new User
        {
            FeishuId = "test-feishu-id",
            Name = "Test User",
            Email = "test@example.com",
            Role = UserRoles.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _permissionServiceMock
            .Setup(x => x.GetUserPermissionsAsync(user.Id, default))
            .ReturnsAsync(new List<string> { Permissions.TaskRead, Permissions.TaskCreate });

        var result = await _sut.GetUserByFeishuIdAsync("test-feishu-id");

        result.Should().NotBeNull();
        result!.FeishuId.Should().Be("test-feishu-id");
        result.UserName.Should().Be("Test User");
        result.Role.Should().Be(UserRoles.User);
        result.Permissions.Should().Contain(Permissions.TaskRead);
    }

    [Fact]
    public async Task SyncUserAsync_ShouldCreateUser_WhenUserNotExists()
    {
        var result = await _sut.SyncUserAsync(
            "new-feishu-id",
            "New User",
            "https://avatar.url",
            "dept-123");

        result.Should().NotBeNull();
        result.FeishuId.Should().Be("new-feishu-id");
        result.Name.Should().Be("New User");
        result.AvatarUrl.Should().Be("https://avatar.url");
        result.DepartmentId.Should().Be("dept-123");
        result.Role.Should().Be(UserRoles.User);

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.FeishuId == "new-feishu-id");
        dbUser.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncUserAsync_ShouldUpdateUser_WhenUserExists()
    {
        var existingUser = new User
        {
            FeishuId = "existing-feishu-id",
            Name = "Old Name",
            Role = UserRoles.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(existingUser);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.SyncUserAsync(
            "existing-feishu-id",
            "New Name",
            "https://new-avatar.url",
            "new-dept-123");

        result.Name.Should().Be("New Name");
        result.AvatarUrl.Should().Be("https://new-avatar.url");
        result.DepartmentId.Should().Be("new-dept-123");

        var dbUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.FeishuId == "existing-feishu-id");
        dbUser!.Name.Should().Be("New Name");
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldReturnFalse_WhenUserIdInvalid()
    {
        var result = await _sut.HasPermissionAsync("invalid-id", Permissions.TaskRead);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldCallPermissionService_WhenUserIdValid()
    {
        var user = new User
        {
            FeishuId = "test-feishu-id",
            Name = "Test User",
            Role = UserRoles.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        _permissionServiceMock
            .Setup(x => x.HasPermissionAsync(user.Id, Permissions.TaskRead, default))
            .ReturnsAsync(true);

        var result = await _sut.HasPermissionAsync(user.Id.ToString(), Permissions.TaskRead);

        result.Should().BeTrue();
        _permissionServiceMock.Verify(x => x.HasPermissionAsync(user.Id, Permissions.TaskRead, default), Times.Once);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnEmpty_WhenUserIdInvalid()
    {
        var result = await _sut.GetUserPermissionsAsync("invalid-id");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldCallPermissionService_WhenUserIdValid()
    {
        var user = new User
        {
            FeishuId = "test-feishu-id",
            Name = "Test User",
            Role = UserRoles.User,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var expectedPermissions = new List<string> { Permissions.TaskRead, Permissions.TaskCreate };
        _permissionServiceMock
            .Setup(x => x.GetUserPermissionsAsync(user.Id, default))
            .ReturnsAsync(expectedPermissions);

        var result = await _sut.GetUserPermissionsAsync(user.Id.ToString());

        result.Should().BeEquivalentTo(expectedPermissions);
    }
}
