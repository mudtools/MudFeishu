// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
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
/// 权限服务测试
/// </summary>
public class PermissionServiceTests : IDisposable
{
    private readonly TaskManageDbContext _dbContext;
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        var options = new DbContextOptionsBuilder<TaskManageDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new TaskManageDbContext(options);
        _sut = new PermissionService(_dbContext, Mock.Of<ILogger<PermissionService>>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnEmpty_WhenUserNotFound()
    {
        var result = await _sut.GetUserPermissionsAsync(999);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnRolePermissions_WhenUserExists()
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

        var result = await _sut.GetUserPermissionsAsync(user.Id);

        result.Should().NotBeEmpty();
        result.Should().Contain(Permissions.TaskCreate);
        result.Should().Contain(Permissions.TaskRead);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnAdminPermissions_WhenUserIsAdmin()
    {
        var user = new User
        {
            FeishuId = "admin-feishu-id",
            Name = "Admin User",
            Role = UserRoles.Admin,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetUserPermissionsAsync(user.Id);

        result.Should().Contain(Permissions.TaskDelete);
        result.Should().Contain(Permissions.UserManage);
        result.Should().Contain(Permissions.DepartmentManage);
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldReturnTrue_WhenUserHasPermission()
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

        var result = await _sut.HasPermissionAsync(user.Id, Permissions.TaskRead);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_ShouldReturnFalse_WhenUserLacksPermission()
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

        var result = await _sut.HasPermissionAsync(user.Id, Permissions.TaskDelete);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GrantPermissionAsync_ShouldGrantPermission_WhenPermissionNotExists()
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

        await _sut.GrantPermissionAsync(user.Id, Permissions.TaskDelete, grantedBy: 1);

        var hasPermission = await _sut.HasPermissionAsync(user.Id, Permissions.TaskDelete);
        hasPermission.Should().BeTrue();
    }

    [Fact]
    public async Task RevokePermissionAsync_ShouldRevokePermission()
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

        await _sut.GrantPermissionAsync(user.Id, Permissions.TaskDelete, grantedBy: 1);
        await _sut.RevokePermissionAsync(user.Id, Permissions.TaskDelete);

        var hasPermission = await _sut.HasPermissionAsync(user.Id, Permissions.TaskDelete);
        hasPermission.Should().BeFalse();
    }

    [Fact]
    public async Task InitializePermissionsAsync_ShouldCreateAllPermissions()
    {
        await _sut.InitializePermissionsAsync();

        var permissions = await _dbContext.Permissions.ToListAsync();
        permissions.Should().NotBeEmpty();
        permissions.Should().Contain(p => p.Code == Permissions.TaskCreate);
        permissions.Should().Contain(p => p.Code == Permissions.TaskRead);
        permissions.Should().Contain(p => p.Code == Permissions.UserManage);
    }

    [Fact]
    public async Task CanAccessTaskAsync_ShouldReturnTrue_WhenUserIsTaskMember()
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

        var task = new TaskSync
        {
            TaskGuid = "test-task-guid",
            Summary = "Test Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        var taskMember = new TaskMemberEntity
        {
            TaskSyncId = task.Id,
            UserId = user.Id,
            Role = TaskMemberRoles.Assignee,
            JoinedAt = DateTime.UtcNow
        };
        _dbContext.TaskMembers.Add(taskMember);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CanAccessTaskAsync(user.Id, task.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModifyTaskAsync_ShouldReturnTrue_WhenUserIsAssignee()
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

        var task = new TaskSync
        {
            TaskGuid = "test-task-guid",
            Summary = "Test Task",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.Tasks.Add(task);
        await _dbContext.SaveChangesAsync();

        var taskMember = new TaskMemberEntity
        {
            TaskSyncId = task.Id,
            UserId = user.Id,
            Role = TaskMemberRoles.Assignee,
            JoinedAt = DateTime.UtcNow
        };
        _dbContext.TaskMembers.Add(taskMember);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.CanModifyTaskAsync(user.Id, task.Id);

        result.Should().BeTrue();
    }
}
