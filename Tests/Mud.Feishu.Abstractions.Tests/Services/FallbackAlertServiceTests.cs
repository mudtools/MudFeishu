// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Abstractions.Tests.Services;

public class FallbackAlertServiceTests
{
    private readonly Mock<ILogger<FallbackAlertService>> _loggerMock;

    public FallbackAlertServiceTests()
    {
        _loggerMock = new Mock<ILogger<FallbackAlertService>>();
    }

    [Fact]
    public async Task RaiseAlertAsync_ShouldInvokeAlertRaisedEvent()
    {
        var sut = new FallbackAlertService(_loggerMock.Object);
        FallbackAlertEventArgs? capturedArgs = null;
        sut.AlertRaised += (_, args) => capturedArgs = args;

        await sut.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "Test alert");

        Assert.NotNull(capturedArgs);
        Assert.Equal(FallbackAlertType.RedisConnectionFailed, capturedArgs.AlertType);
        Assert.Equal("Test alert", capturedArgs.Message);
    }

    [Fact]
    public async Task RaiseAlertAsync_ShouldCallRegisteredHandler()
    {
        var handlerMock = new Mock<IFallbackAlertHandler>();
        var sut = new FallbackAlertService(_loggerMock.Object, new[] { handlerMock.Object });

        await sut.RaiseAlertAsync(FallbackAlertType.RedisTimeout, "Timeout alert");

        handlerMock.Verify(
            x => x.HandleAlertAsync(It.Is<FallbackAlertEventArgs>(
                a => a.AlertType == FallbackAlertType.RedisTimeout && a.Message == "Timeout alert")),
            Times.Once);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithException_ShouldIncludeExceptionInArgs()
    {
        var sut = new FallbackAlertService(_loggerMock.Object);
        FallbackAlertEventArgs? capturedArgs = null;
        sut.AlertRaised += (_, args) => capturedArgs = args;
        var testEx = new InvalidOperationException("Test error");

        await sut.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "Alert", testEx);

        Assert.NotNull(capturedArgs);
        Assert.Equal(testEx, capturedArgs.Exception);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithAdditionalData_ShouldIncludeDataInArgs()
    {
        var sut = new FallbackAlertService(_loggerMock.Object);
        FallbackAlertEventArgs? capturedArgs = null;
        sut.AlertRaised += (_, args) => capturedArgs = args;
        var data = new Dictionary<string, object?> { { "key", "value" } };

        await sut.RaiseAlertAsync(FallbackAlertType.RedisFallbackActivated, "Alert", null, data);

        Assert.NotNull(capturedArgs);
        Assert.True(capturedArgs.AdditionalData.ContainsKey("key"));
        Assert.Equal("value", capturedArgs.AdditionalData["key"]);
    }

    [Fact]
    public async Task RaiseAlertAsync_WhenHandlerThrows_ShouldNotPropagateException()
    {
        var failingHandler = new Mock<IFallbackAlertHandler>();
        failingHandler
            .Setup(x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()))
            .ThrowsAsync(new Exception("Handler failed"));

        var sut = new FallbackAlertService(_loggerMock.Object, new[] { failingHandler.Object });

        await sut.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "Alert");
    }

    [Fact]
    public async Task RaiseAlertAsync_WithMultipleHandlers_ShouldCallAllHandlers()
    {
        var handler1 = new Mock<IFallbackAlertHandler>();
        var handler2 = new Mock<IFallbackAlertHandler>();
        var sut = new FallbackAlertService(_loggerMock.Object, new[] { handler1.Object, handler2.Object });

        await sut.RaiseAlertAsync(FallbackAlertType.RedisRecovered, "Recovered");

        handler1.Verify(x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()), Times.Once);
        handler2.Verify(x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()), Times.Once);
    }

    [Fact]
    public void RegisterHandler_ShouldAddHandler()
    {
        var sut = new FallbackAlertService(_loggerMock.Object);
        var handler = new Mock<IFallbackAlertHandler>();

        sut.RegisterHandler(handler.Object);

        Assert.Single(sut.GetHandlersForTest());
    }

    [Fact]
    public void RemoveHandler_ShouldRemoveHandler()
    {
        var handler = new Mock<IFallbackAlertHandler>();
        var sut = new FallbackAlertService(_loggerMock.Object, new[] { handler.Object });

        sut.RemoveHandler(handler.Object);

        Assert.Empty(sut.GetHandlersForTest());
    }

    [Fact]
    public async Task RaiseAlertAsync_WhenNoHandlers_ShouldNotThrow()
    {
        var sut = new FallbackAlertService(_loggerMock.Object);

        await sut.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "No handlers");
    }

    [Fact]
    public async Task RaiseAlertAsync_WhenNoSubscribers_ShouldNotThrow()
    {
        var sut = new FallbackAlertService(_loggerMock.Object);

        await sut.RaiseAlertAsync(FallbackAlertType.RedisTimeout, "No subscribers");
    }
}

internal static class FallbackAlertServiceTestExtensions
{
    public static List<IFallbackAlertHandler> GetHandlersForTest(this FallbackAlertService service)
    {
        var field = typeof(FallbackAlertService).GetField("_handlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (List<IFallbackAlertHandler>)field!.GetValue(service)!;
    }
}
