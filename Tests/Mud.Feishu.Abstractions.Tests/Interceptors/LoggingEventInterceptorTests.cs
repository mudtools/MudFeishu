// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.Tests.Interceptors;

/// <summary>
/// LoggingEventInterceptor 单元测试
/// </summary>
public class LoggingEventInterceptorTests
{
    private readonly Mock<ILogger<LoggingEventInterceptor>> _loggerMock;
    private readonly LoggingEventInterceptor _interceptor;

    public LoggingEventInterceptorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingEventInterceptor>>();
        _interceptor = new LoggingEventInterceptor(_loggerMock.Object);
    }

    [Fact]
    public async Task BeforeHandleAsync_ShouldLogInformation()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType,
            TenantKey = "test-tenant"
        };

        // Act
        var result = await _interceptor.BeforeHandleAsync(eventType, eventData, CancellationToken.None);

        // Assert
        Assert.True(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("开始处理事件")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AfterHandleAsync_WhenSuccess_ShouldLogInformation()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType,
            TenantKey = "test-tenant"
        };

        // Act
        await _interceptor.AfterHandleAsync(eventType, eventData, null, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("事件处理成功")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AfterHandleAsync_WhenException_ShouldLogError()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType,
            TenantKey = "test-tenant"
        };
        var exception = new InvalidOperationException("Test exception");

        // Act
        await _interceptor.AfterHandleAsync(eventType, eventData, exception, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("事件处理失败")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task BeforeHandleAsync_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType
        };

        // Act
        var result = await _interceptor.BeforeHandleAsync(eventType, eventData, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task AfterHandleAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType
        };

        // Act & Assert - 不应抛出异常
        await _interceptor.AfterHandleAsync(eventType, eventData, null, CancellationToken.None);
    }
}
