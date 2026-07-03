// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// MessageRouter 消息处理超时控制测试
/// </summary>
public class MessageRouterTimeoutTests
{
    [Fact]
    public void FeishuWebSocketOptions_MessageHandlerTimeoutMs_DefaultShouldBe30000()
    {
        var options = new FeishuWebSocketOptions();
        options.MessageHandlerTimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void FeishuWebSocketOptions_MessageHandlerTimeoutMs_CanBeSetToZero()
    {
        var options = new FeishuWebSocketOptions { MessageHandlerTimeoutMs = 0 };
        options.MessageHandlerTimeoutMs.Should().Be(0);
    }

    [Fact]
    public void FeishuWebSocketOptions_MessageHandlerTimeoutMs_CanBeSetToCustomValue()
    {
        var options = new FeishuWebSocketOptions { MessageHandlerTimeoutMs = 5000 };
        options.MessageHandlerTimeoutMs.Should().Be(5000);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldCompleteNormally_WhenHandlerFinishesWithinTimeout()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            MessageHandlerTimeoutMs = 5000,
            EnableLogging = false
        };
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);

        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("event")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        router.RegisterHandler(handlerMock.Object);

        var message = """{"type":"event","data":"test"}""";

        // Act
        await router.RouteMessageAsync(message);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldNotBlock_WhenHandlerExceedsTimeout()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            MessageHandlerTimeoutMs = 200, // 200ms 超时
            EnableLogging = false
        };
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);

        var handlerStarted = new TaskCompletionSource<bool>();
        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("event")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string msg, CancellationToken ct) =>
            {
                handlerStarted.TrySetResult(true);
                try
                {
                    await Task.Delay(10000, ct); // 10秒，远超超时
                }
                catch (OperationCanceledException)
                {
                    // 预期被取消
                }
            });

        router.RegisterHandler(handlerMock.Object);

        var message = """{"type":"event","data":"slow"}""";

        // Act
        var routeTask = router.RouteMessageAsync(message);

        // 等待超时 + 宽限期（200ms + 2s grace）
        var completed = await Task.WhenAny(routeTask, Task.Delay(5000));

        // Assert
        completed.Should().Be(routeTask, "路由应该在超时后完成，而不是无限阻塞");
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RouteMessageAsync_ShouldNotApplyTimeout_WhenTimeoutIsZero()
    {
        // Arrange
        var options = new FeishuWebSocketOptions
        {
            MessageHandlerTimeoutMs = 0, // 不限制超时
            EnableLogging = false
        };
        var router = new MessageRouter(NullLogger<MessageRouter>.Instance, options);

        var handlerMock = new Mock<IMessageHandler>();
        handlerMock.Setup(h => h.CanHandle("event")).Returns(true);
        handlerMock.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string msg, CancellationToken ct) =>
            {
                await Task.Delay(100, ct);
            });

        router.RegisterHandler(handlerMock.Object);

        var message = """{"type":"event","data":"test"}""";

        // Act
        await router.RouteMessageAsync(message);

        // Assert
        handlerMock.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
