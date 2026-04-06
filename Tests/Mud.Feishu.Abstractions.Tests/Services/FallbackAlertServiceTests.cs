// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Services;
using Xunit;

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
        var service = new FallbackAlertService(_loggerMock.Object);
        FallbackAlertEventArgs? receivedArgs = null;
        service.AlertRaised += (sender, args) => receivedArgs = args;

        await service.RaiseAlertAsync(
            FallbackAlertType.RedisConnectionFailed,
            "Redis connection failed",
            new InvalidOperationException("Test exception"));

        Assert.NotNull(receivedArgs);
        Assert.Equal(FallbackAlertType.RedisConnectionFailed, receivedArgs.AlertType);
        Assert.Equal("Redis connection failed", receivedArgs.Message);
        Assert.NotNull(receivedArgs.Exception);
        Assert.IsType<InvalidOperationException>(receivedArgs.Exception);
    }

    [Fact]
    public async Task RaiseAlertAsync_ShouldCallRegisteredHandlers()
    {
        var handlerMock = new Mock<IFallbackAlertHandler>();
        var service = new FallbackAlertService(_loggerMock.Object, new[] { handlerMock.Object });

        await service.RaiseAlertAsync(
            FallbackAlertType.RedisConnectionFailed,
            "Test message");

        handlerMock.Verify(
            x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()),
            Times.Once);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithMultipleHandlers_ShouldCallAllHandlers()
    {
        var handler1Mock = new Mock<IFallbackAlertHandler>();
        var handler2Mock = new Mock<IFallbackAlertHandler>();
        var service = new FallbackAlertService(_loggerMock.Object, new[] { handler1Mock.Object, handler2Mock.Object });

        await service.RaiseAlertAsync(
            FallbackAlertType.RedisConnectionFailed,
            "Test message");

        handler1Mock.Verify(
            x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()),
            Times.Once);
        handler2Mock.Verify(
            x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()),
            Times.Once);
    }

    [Fact]
    public async Task RaiseAlertAsync_WhenHandlerThrows_ShouldContinueWithOtherHandlers()
    {
        var handler1Mock = new Mock<IFallbackAlertHandler>();
        var handler2Mock = new Mock<IFallbackAlertHandler>();

        handler1Mock
            .Setup(x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()))
            .ThrowsAsync(new InvalidOperationException("Handler error"));

        var service = new FallbackAlertService(_loggerMock.Object, new[] { handler1Mock.Object, handler2Mock.Object });

        await service.RaiseAlertAsync(
            FallbackAlertType.RedisConnectionFailed,
            "Test message");

        handler1Mock.Verify(
            x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()),
            Times.Once);
        handler2Mock.Verify(
            x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()),
            Times.Once);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithAdditionalData_ShouldIncludeInEventArgs()
    {
        var service = new FallbackAlertService(_loggerMock.Object);
        FallbackAlertEventArgs? receivedArgs = null;
        service.AlertRaised += (sender, args) => receivedArgs = args;

        var additionalData = new Dictionary<string, object?>
        {
            ["Key1"] = "Value1",
            ["Key2"] = 123
        };

        await service.RaiseAlertAsync(
            FallbackAlertType.RedisConnectionFailed,
            "Test message",
            additionalData: additionalData);

        Assert.NotNull(receivedArgs);
        Assert.Equal("Value1", receivedArgs.AdditionalData["Key1"]);
        Assert.Equal(123, receivedArgs.AdditionalData["Key2"]);
    }

    [Fact]
    public async Task RegisterHandler_ShouldAddHandlerToService()
    {
        var handlerMock = new Mock<IFallbackAlertHandler>();
        var service = new FallbackAlertService(_loggerMock.Object);

        service.RegisterHandler(handlerMock.Object);

        await service.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "Test");

        handlerMock.Verify(x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()), Times.Once);
    }

    [Fact]
    public async Task RemoveHandler_ShouldRemoveHandlerFromService()
    {
        var handlerMock = new Mock<IFallbackAlertHandler>();
        var service = new FallbackAlertService(_loggerMock.Object);
        service.RegisterHandler(handlerMock.Object);

        service.RemoveHandler(handlerMock.Object);

        await service.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "Test");

        handlerMock.Verify(x => x.HandleAlertAsync(It.IsAny<FallbackAlertEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task RaiseAlertAsync_WithDifferentAlertTypes_ShouldPassCorrectType()
    {
        var service = new FallbackAlertService(_loggerMock.Object);
        var receivedTypes = new List<FallbackAlertType>();
        service.AlertRaised += (sender, args) => receivedTypes.Add(args.AlertType);

        await service.RaiseAlertAsync(FallbackAlertType.RedisConnectionFailed, "Test1");
        await service.RaiseAlertAsync(FallbackAlertType.RedisTimeout, "Test2");
        await service.RaiseAlertAsync(FallbackAlertType.RedisFallbackActivated, "Test3");
        await service.RaiseAlertAsync(FallbackAlertType.RedisRecovered, "Test4");

        Assert.Equal(4, receivedTypes.Count);
        Assert.Equal(FallbackAlertType.RedisConnectionFailed, receivedTypes[0]);
        Assert.Equal(FallbackAlertType.RedisTimeout, receivedTypes[1]);
        Assert.Equal(FallbackAlertType.RedisFallbackActivated, receivedTypes[2]);
        Assert.Equal(FallbackAlertType.RedisRecovered, receivedTypes[3]);
    }
}
