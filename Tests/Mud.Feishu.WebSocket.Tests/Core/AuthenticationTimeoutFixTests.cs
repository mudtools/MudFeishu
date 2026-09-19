// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// 认证链路修复回归测试：P0-3（认证响应超时必须真正生效）、P1-13（冷却期在重试循环内生效）。
/// </summary>
public class AuthenticationTimeoutFixTests
{
    private static AuthenticationManager CreateManager(FeishuWebSocketOptions options)
    {
        return new AuthenticationManager(
            NullLogger<AuthenticationManager>.Instance,
            options,
            _ => Task.CompletedTask);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldThrowTimeoutException_WhenServerNeverResponds()
    {
        // Arrange
        // 修复前：_authCompletionSource.Task 与 30 秒超时 CTS 毫无关联，
        // 服务端不应答时此处会永久挂起（测试会超时失败）。
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { AuthTimeoutMs = 300, Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAuthRetryAttempts = 1, BaseDelayMs = 1000 } };
        var manager = CreateManager(options);

        // Act
        var act = async () => await manager.AuthenticateAsync("test-app-access-token");

        // Assert
        // 达到 MaxAuthRetryAttempts 后由 AuthenticateInternalAsync 包一层
        // InvalidOperationException（"已达到最大重试次数"），内层为真正的超时异常。
        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithInnerException<TimeoutException>();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldCompleteWithinTimeoutBudget_WhenServerNeverResponds()
    {
        // Arrange
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { AuthTimeoutMs = 200, Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAuthRetryAttempts = 1, BaseDelayMs = 1000 } };
        var manager = CreateManager(options);

        // Act
        Exception? thrown = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            await manager.AuthenticateAsync("test-app-access-token");
        }
        catch (Exception ex)
        {
            thrown = ex;
        }
        stopwatch.Stop();

        // Assert：必须在合理预算内结束（而非永久挂起），且异常链中含超时
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(20));
        thrown.Should().NotBeNull();
        thrown.Should().BeAssignableTo<InvalidOperationException>();
        thrown!.InnerException.Should().BeOfType<TimeoutException>();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldThrowCooldownException_WhenFailuresReachThreshold()
    {
        // Arrange
        // 修复前：冷却期只在 AuthenticateAsync 入口检查一次，
        // 重试循环内部不再感知，导致无限重试场景下冷却机制完全失效。
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { AuthTimeoutMs = 200, Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAuthRetryAttempts = 5, BaseDelayMs = 1000 } };
        var manager = CreateManager(options);

        // Act
        var act = async () => await manager.AuthenticateAsync("test-app-access-token");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
                 .WithMessage("*认证失败过多*");
    }

    [Fact]
    public void AuthTimeoutMs_ShouldFallBackToDefault_WhenSetToNonPositive()
    {
        // Arrange
        var options = new FeishuWebSocketOptions();

        // Act
        options.AuthTimeoutMs = 0;
        var zeroResult = options.AuthTimeoutMs;

        options.AuthTimeoutMs = -100;
        var negativeResult = options.AuthTimeoutMs;

        // Assert
        zeroResult.Should().Be(FeishuWebSocketOptions.DefaultAuthTimeoutMs);
        negativeResult.Should().Be(FeishuWebSocketOptions.DefaultAuthTimeoutMs);
    }

    [Fact]
    public void MaxAuthRetryAttempts_ShouldBeIndependentOfMaxReconnectAttempts()
    {
        // Arrange & Act
        var options = new Mud.Feishu.WebSocket.FeishuWebSocketOptions { Reconnect = new Mud.Feishu.WebSocket.WebSocketReconnectOptions { MaxAttempts = 0, MaxAuthRetryAttempts = 3 } };

        // Assert：认证重试次数不再被"无限重连"配置连带放大
        options.Reconnect.MaxAttempts.Should().Be(0);
        options.Reconnect.MaxAuthRetryAttempts.Should().Be(3);
    }
}
