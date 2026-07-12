// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Mud.Feishu.Abstractions.Observability;

namespace Mud.Feishu.Abstractions.Tests.Interceptors;

/// <summary>
/// TelemetryEventInterceptor 单元测试
/// </summary>
public class TelemetryEventInterceptorTests : IDisposable
{
    private readonly TelemetryEventInterceptor _interceptor;
    private readonly ActivityListener _activityListener;

    public TelemetryEventInterceptorTests()
    {
        _interceptor = new TelemetryEventInterceptor("test_app_key");

        // 注册 ActivityListener 确保 StartActivity 返回非 null
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == FeishuActivitySource.Name,
            SampleUsingParentId = (ref ActivityCreationOptions<string> options) => ActivitySamplingResult.AllData,
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener.Dispose();
    }

    [Fact]
    public async Task BeforeHandleAsync_ShouldReturnTrue()
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
        result.Should().BeTrue();
    }

    [Fact]
    public async Task BeforeHandleAsync_ShouldStoreActivityInItems()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType
        };

        // Act
        await _interceptor.BeforeHandleAsync(eventType, eventData, CancellationToken.None);

        // Assert
        eventData.Items.Should().NotBeNull();
        eventData.Items!.Should().ContainKey("__activity");
    }

    [Fact]
    public async Task AfterHandleAsync_WhenSuccess_ShouldCompleteActivity()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType
        };

        // Act
        await _interceptor.BeforeHandleAsync(eventType, eventData, CancellationToken.None);
        await Task.Delay(10); // 模拟处理时间
        await _interceptor.AfterHandleAsync(eventType, eventData, null, CancellationToken.None);

        // Assert - 验证方法执行成功，Activity 已从 Items 中移除
        eventData.Items!.Should().NotContainKey("__activity");
    }

    [Fact]
    public async Task AfterHandleAsync_WhenException_ShouldMarkActivityAsError()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType
        };
        var exception = new InvalidOperationException("Test exception");

        // Act
        await _interceptor.BeforeHandleAsync(eventType, eventData, CancellationToken.None);
        await _interceptor.AfterHandleAsync(eventType, eventData, exception, CancellationToken.None);

        // Assert - 验证方法执行成功，Activity 已从 Items 中移除
        eventData.Items!.Should().NotContainKey("__activity");
    }

    [Fact]
    public async Task AfterHandleAsync_WhenItemsNull_ShouldNotThrow()
    {
        // Arrange
        var eventType = "test.event.type";
        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = eventType
        };

        // Act - 不调用 BeforeHandleAsync，Items 为 null
        var exception = await Record.ExceptionAsync(() =>
            _interceptor.AfterHandleAsync(eventType, eventData, null, CancellationToken.None));

        // Assert
        exception.Should().BeNull();
    }

    [Fact]
    public void Constructor_WhenNullAppKey_ShouldThrow()
    {
        // Act
        var act = () => new TelemetryEventInterceptor(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
