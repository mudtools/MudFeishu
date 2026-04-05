// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Authentication;

namespace Mud.Feishu.Tests.Authentication;

/// <summary>
/// CurrentUserContext 单元测试
/// </summary>
public class CurrentUserContextTests
{
    private readonly Mock<ILogger<CurrentUserContext>> _loggerMock;

    public CurrentUserContextTests()
    {
        _loggerMock = new Mock<ILogger<CurrentUserContext>>();
    }

    private CurrentUserContext CreateContext()
    {
        return new CurrentUserContext(_loggerMock.Object);
    }

    #region SetUser Tests

    [Fact]
    public void SetUser_ValidParameters_SetsProperties()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.SetUser("open_id_123", "union_id_456", "user_id_789", "Test User");

        // Assert
        Assert.Equal("open_id_123", context.OpenId);
        Assert.Equal("union_id_456", context.UnionId);
        Assert.Equal("user_id_789", context.UserId);
        Assert.Equal("Test User", context.Name);
        Assert.True(context.IsAuthenticated);
    }

    [Fact]
    public void SetUser_OnlyOpenId_SetsOnlyOpenId()
    {
        // Arrange
        var context = CreateContext();

        // Act
        context.SetUser("open_id_123");

        // Assert
        Assert.Equal("open_id_123", context.OpenId);
        Assert.Null(context.UnionId);
        Assert.Null(context.UserId);
        Assert.Null(context.Name);
        Assert.True(context.IsAuthenticated);
    }

    [Fact]
    public void SetUser_EmptyOpenId_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetUser(string.Empty));
    }

    [Fact]
    public void SetUser_NullOpenId_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetUser(null!));
    }

    [Fact]
    public void SetUser_WhitespaceOpenId_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => context.SetUser("   "));
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_AfterSetUser_PropertiesAreNull()
    {
        // Arrange
        var context = CreateContext();
        context.SetUser("open_id_123", "union_id_456", "user_id_789", "Test User");

        // Act
        context.Clear();

        // Assert
        Assert.Null(context.OpenId);
        Assert.Null(context.UnionId);
        Assert.Null(context.UserId);
        Assert.Null(context.Name);
        Assert.False(context.IsAuthenticated);
    }

    [Fact]
    public void Clear_WhenNoUser_DoesNotThrow()
    {
        // Arrange
        var context = CreateContext();

        // Act & Assert - should not throw
        context.Clear();
        Assert.False(context.IsAuthenticated);
    }

    #endregion

    #region IsAuthenticated Tests

    [Fact]
    public void IsAuthenticated_NoUser_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext();

        // Assert
        Assert.False(context.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_ValidUser_ReturnsTrue()
    {
        // Arrange
        var context = CreateContext();
        context.SetUser("open_id_123");

        // Assert
        Assert.True(context.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_AfterClear_ReturnsFalse()
    {
        // Arrange
        var context = CreateContext();
        context.SetUser("open_id_123");
        context.Clear();

        // Assert
        Assert.False(context.IsAuthenticated);
    }

    #endregion

    #region AsyncLocal Isolation Tests

    [Fact]
    public async Task AsyncLocal_IsolationBetweenAsyncContexts()
    {
        // Arrange
        var context = CreateContext();

        // Act - Run two async operations with different users
        var task1 = Task.Run(async () =>
        {
            context.SetUser("open_id_task1", "union_id_task1");
            await Task.Delay(50);
            return context.OpenId;
        });

        var task2 = Task.Run(async () =>
        {
            context.SetUser("open_id_task2", "union_id_task2");
            await Task.Delay(10);
            return context.OpenId;
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert - Each task should see its own user
        Assert.Equal("open_id_task1", results[0]);
        Assert.Equal("open_id_task2", results[1]);
    }

    [Fact]
    public async Task AsyncLocal_IsolationBetweenThreads()
    {
        // Arrange
        var context = CreateContext();
        var results = new System.Collections.Concurrent.ConcurrentBag<string?>();

        // Act - Run multiple threads with different users
        Parallel.For(0, 10, i =>
        {
            context.SetUser($"open_id_{i}");
            Thread.Sleep(10);
            results.Add(context.OpenId);
        });

        await Task.CompletedTask;

        // Assert - Each thread should see its own user (0-9)
        Assert.Equal(10, results.Count);
        Assert.All(results, r => Assert.Matches(@"open_id_\d", r!));
    }

    [Fact]
    public async Task AsyncLocal_ClearInOneContext_DoesNotAffectOthers()
    {
        // Arrange
        var context = CreateContext();

        // Act
        var task1 = Task.Run(() =>
        {
            context.SetUser("open_id_task1");
            context.Clear();
            return context.IsAuthenticated;
        });

        var task2 = Task.Run(() =>
        {
            context.SetUser("open_id_task2");
            return context.IsAuthenticated;
        });

        var results = await Task.WhenAll(task1, task2);

        // Assert
        Assert.False(results[0]); // task1 cleared
        Assert.True(results[1]);  // task2 still authenticated
    }

    #endregion

    #region Overwrite Tests

    [Fact]
    public void SetUser_OverwritesExistingUser()
    {
        // Arrange
        var context = CreateContext();
        context.SetUser("open_id_1", "union_id_1", "user_id_1", "User 1");

        // Act
        context.SetUser("open_id_2", "union_id_2", "user_id_2", "User 2");

        // Assert
        Assert.Equal("open_id_2", context.OpenId);
        Assert.Equal("union_id_2", context.UnionId);
        Assert.Equal("user_id_2", context.UserId);
        Assert.Equal("User 2", context.Name);
    }

    #endregion
}
