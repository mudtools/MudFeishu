// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Interceptors;
using Mud.Feishu.WebSocket.Handlers;

namespace Mud.Feishu.WebSocket.Tests.Handlers;

/// <summary>
/// FeishuEventMessageHandler 单元测试
/// </summary>
public class FeishuEventMessageHandlerTests
{
    private readonly Mock<ILogger<FeishuEventMessageHandler>> _loggerMock;
    private readonly Mock<IFeishuEventHandlerFactory> _handlerFactoryMock;
    private readonly FeishuWebSocketOptions _options;

    public FeishuEventMessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<FeishuEventMessageHandler>>();
        _handlerFactoryMock = new Mock<IFeishuEventHandlerFactory>();
        _options = new FeishuWebSocketOptions();
    }

    private FeishuEventMessageHandler CreateHandler()
    {
        return new FeishuEventMessageHandler(
            _loggerMock.Object,
            _handlerFactoryMock.Object,
            null,
            null,
            _options,
            null);
    }

    [Fact]
    public void Constructor_WithNullEventHandlerFactory_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuEventMessageHandler(
            _loggerMock.Object,
            null!,
            null,
            null,
            _options,
            null);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("eventHandlerFactory");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new FeishuEventMessageHandler(
            _loggerMock.Object,
            _handlerFactoryMock.Object,
            null,
            null,
            null!,
            null);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Theory]
    [InlineData("event")]
    [InlineData("EVENT")]
    [InlineData("Event")]
    [InlineData("event_callback")]
    [InlineData("EVENT_CALLBACK")]
    [InlineData("binary_event")]
    [InlineData("BINARY_EVENT")]
    public void CanHandle_WithValidMessageType_ShouldReturnTrue(string messageType)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.CanHandle(messageType);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("auth")]
    [InlineData("ping")]
    [InlineData("pong")]
    [InlineData("")]
    public void CanHandle_WithInvalidMessageType_ShouldReturnFalse(string messageType)
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.CanHandle(messageType);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_WithEmptyMessage_ShouldNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert - should not throw
        await handler.HandleAsync("");
    }

    [Fact]
    public async Task HandleAsync_WithWhitespaceMessage_ShouldNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert - should not throw
        await handler.HandleAsync("   ");
    }

    [Fact]
    public async Task HandleAsync_WithInvalidJson_ShouldNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act & Assert - should not throw
        await handler.HandleAsync("not valid json");
    }

    [Fact]
    public async Task HandleAsync_WithV2Event_ShouldPopulateHeader()
    {
        var handler = CreateHandler();
        var v2Message = @"{""schema"":""2.0"",""header"":{""event_id"":""evt_ws_001"",""event_type"":""drive.file.edit_v1"",""create_time"":""1704067200000"",""token"":""ws_token_abc"",""tenant_key"":""tk_ws"",""app_id"":""cli_ws""},""event"":{""file_token"":""ft_ws_123""}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(v2Message);

        capturedEventData.Should().NotBeNull();
        capturedEventData!.Header.Should().NotBeNull();
        capturedEventData.Header!.Schema.Should().Be("2.0");
        capturedEventData.Header.EventId.Should().Be("evt_ws_001");
        capturedEventData.Header.EventType.Should().Be("drive.file.edit_v1");
        capturedEventData.Header.Token.Should().Be("ws_token_abc");
        capturedEventData.Header.CreateTime.Should().Be("1704067200000");
        capturedEventData.Header.TenantKey.Should().Be("tk_ws");
        capturedEventData.Header.AppId.Should().Be("cli_ws");
    }

    [Fact]
    public async Task HandleAsync_WithV2Event_HeaderAndFlatPropertiesShouldBeConsistent()
    {
        var handler = CreateHandler();
        var v2Message = @"{""schema"":""2.0"",""header"":{""event_id"":""evt_ws_cons"",""event_type"":""drive.file.read_v1"",""create_time"":""1704067200000"",""token"":""ws_tok"",""tenant_key"":""tk_cons"",""app_id"":""cli_cons""},""event"":{}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(v2Message);

        capturedEventData.Should().NotBeNull();
        capturedEventData!.EventId.Should().Be(capturedEventData.Header!.EventId);
        capturedEventData.EventType.Should().Be(capturedEventData.Header.EventType);
        capturedEventData.TenantKey.Should().Be(capturedEventData.Header.TenantKey);
        capturedEventData.AppId.Should().Be(capturedEventData.Header.AppId);
    }

    [Fact]
    public async Task HandleAsync_WithV2Event_SchemaShouldBePopulatedFromRootLevel()
    {
        var handler = CreateHandler();
        var v2Message = @"{""schema"":""2.0"",""header"":{""event_id"":""evt_schema_ws"",""event_type"":""test.event"",""tenant_key"":""tk"",""app_id"":""app""},""event"":{}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(v2Message);

        capturedEventData.Should().NotBeNull();
        capturedEventData!.Schema.Should().Be("2.0");
        capturedEventData.Header!.Schema.Should().Be("2.0");
    }

    [Fact]
    public async Task HandleAsync_WithV1Event_HeaderShouldBeNull()
    {
        var handler = CreateHandler();
        var v1Message = @"{""data"":{""event_id"":""evt_v1_ws"",""event_type"":""test.v1.event"",""app_id"":""cli_v1"",""tenant_key"":""tk_v1""}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(v1Message);

        capturedEventData.Should().NotBeNull();
        capturedEventData!.Header.Should().BeNull();
        capturedEventData.Schema.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithV2EventWithNumericCreateTime_HeaderCreateTimeShouldBeString()
    {
        var handler = CreateHandler();
        var v2Message = @"{""schema"":""2.0"",""header"":{""event_id"":""evt_num_ct_ws"",""event_type"":""test.event"",""create_time"":1704067200000,""tenant_key"":""tk"",""app_id"":""app""},""event"":{}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(v2Message);

        capturedEventData.Should().NotBeNull();
        capturedEventData!.Header.Should().NotBeNull();
        capturedEventData.Header!.CreateTime.Should().Be("1704067200000");
    }

    // ===== P1-2：空 EventId fail-closed =====

    [Fact]
    public async Task HandleAsync_WithEmptyEventId_ShouldSkipDedupAndProcessing_ByDefault()
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            null,
            null,
            new FeishuWebSocketOptions { RejectEmptyEventIds = true },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        await handler.HandleAsync(message);

        factoryMock.Verify(
            f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyEventId_ShouldProcess_WhenRejectEmptyEventIdsDisabled()
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            null,
            null,
            new FeishuWebSocketOptions { RejectEmptyEventIds = false },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        await handler.HandleAsync(message);

        factoryMock.Verify(
            f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ===== P1-1：Mark 失败禁止回滚（WHF-07 WS 对齐） =====

    [Fact]
    public async Task HandleAsync_WhenMarkCompletedThrows_ShouldNotRollback_AndNotRethrow()
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis mark failed"));

        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            null,
            new FeishuWebSocketOptions(),
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_mark_fail\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        var act = () => handler.HandleAsync(message);

        await act.Should().NotThrowAsync("Mark 失败不得向上传播（WHF-07）");
        factoryMock.Verify(
            f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()),
            Times.Once);
        dedupMock.Verify(
            d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ===== P1-7：拦截回滚 + 终态标记 =====

    [Fact]
    public async Task HandleAsync_WhenInterceptorBlocks_ShouldRollbackDedup_AndRethrow()
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(i => i.BeforeHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            [interceptorMock.Object],
            new FeishuWebSocketOptions(),
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_intercept\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        var act = () => handler.HandleAsync(message);

        await act.Should().ThrowAsync<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>();
        dedupMock.Verify(
            d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        factoryMock.Verify(
            f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenInterceptorBlocks_ShouldPassInterceptedMarkerToAfterHandle()
    {
        // Arrange - P1-7/决策 D：拦截终态必须以 EventHandlingOutcomeException 传给后置拦截器
        // （接口契约 exception==null 表示成功，拦截不是成功也不是业务异常）
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();

        Exception? capturedAfterHandle = null;
        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(i => i.BeforeHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        interceptorMock
            .Setup(i => i.AfterHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<Exception?>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, Exception?, CancellationToken>((_, _, ex, _) => capturedAfterHandle = ex)
            .Returns(Task.CompletedTask);

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            [interceptorMock.Object],
            new FeishuWebSocketOptions(),
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_intercept_marker\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        // Act
        var act = () => handler.HandleAsync(message);

        // Assert
        await act.Should().ThrowAsync<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>();
        capturedAfterHandle.Should().BeOfType<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>();
        ((Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException)capturedAfterHandle!)
            .OutcomeKind.Should().Be("intercepted");
    }

    // ===== P2-1：外部取消的终态与指标一次性口径 =====

    [Fact]
    public async Task HandleAsync_WhenExternallyCancelled_ShouldRollbackOnce_AndPassCanceledMarkerToAfterHandle()
    {
        // Arrange - P2-1：此前 OCE 会先被内层通用 catch 记为 failure(OperationCanceledException)，
        // 再由外层 catch(OperationCanceledException) 记为 failure(canceled) —— 同一事件双计数，
        // 且第二次的 event_type 退化为 "unknown"（污染维度）。修复后内层收口、只记一次。
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("外部取消"));

        Exception? capturedAfterHandle = null;
        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(i => i.BeforeHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        interceptorMock
            .Setup(i => i.AfterHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<Exception?>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, Exception?, CancellationToken>((_, _, ex, _) => capturedAfterHandle = ex)
            .Returns(Task.CompletedTask);

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // 唯一 AppKey：MeterListener 是进程级观测通道，用独立 AppKey 把本用例的测量
        // 与同程序集内其它用例（默认 AppKey="default"）隔离，避免并行串扰
        const string probeAppKey = "ws_cancel_probe";
        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            [interceptorMock.Object],
            new FeishuWebSocketOptions { AppKey = probeAppKey },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_cancel\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        // Act：观测 feishu.event.handling 指标，断言 canceled 只被记一次且 event_type 不被污染为 unknown
        var canceledRecords = new List<Dictionary<string, object?>>();
        using var listener = new System.Diagnostics.Metrics.MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == Mud.Feishu.Abstractions.Metrics.FeishuMetrics.MeterName &&
                    instrument.Name == "feishu.event.handling")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var captured = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                captured[tag.Key] = tag.Value;
            }
            if (captured.TryGetValue(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.AppKey, out var appKey) &&
                Equals(appKey, probeAppKey) &&
                captured.TryGetValue(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.ErrorType, out var errorType) &&
                Equals(errorType, "canceled"))
            {
                canceledRecords.Add(captured);
            }
        });
        listener.Start();

        var act = () => handler.HandleAsync(message);
        await act.Should().ThrowAsync<OperationCanceledException>("取消语义必须继续向调用方传播");
        listener.Dispose();

        // Assert
        canceledRecords.Should().ContainSingle("P2-1：取消终态只能记一次指标");
        canceledRecords[0][Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.EventType]
            .Should().Be("drive.file.edit_v1", "取消指标必须携带真实 event_type，不得退化为 unknown");

        capturedAfterHandle.Should().BeOfType<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>();
        ((Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException)capturedAfterHandle!)
            .OutcomeKind.Should().Be("canceled");

        dedupMock.Verify(
            d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once, "取消路径必须回滚去重状态（且仅一次）");
    }

    // ===== P0-1（R2）：补偿性去重操作不消费调用方令牌（AGENTS.md D15） =====

    [Fact]
    public async Task HandleAsync_WhenExternallyCancelled_ShouldRollbackDedup_WithNoneToken()
    {
        // Arrange - P0-1：外部取消路径上调用方令牌已取消，若回滚透传该令牌，
        // Redis 后端入口 ThrowIfCancellationRequested 会令回滚失效（键停留 processing →
        // 服务端重发被判重跳过并 ACK 200 → 事件丢失）。回滚必须以 CancellationToken.None 落至后端。
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("外部取消"));

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            null,
            new FeishuWebSocketOptions(),
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_cancel_none_token\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        // Act
        var act = () => handler.HandleAsync(message);
        await act.Should().ThrowAsync<OperationCanceledException>();

        // Assert：回滚必须发生，且后端调用收到的是 CancellationToken.None（而非已取消的调用方令牌）
        dedupMock.Verify(
            d => d.RollbackProcessingAsync(
                "evt_cancel_none_token",
                It.IsAny<string?>(),
                CancellationToken.None),
            Times.Once, "D15：补偿性回滚必须以 CancellationToken.None 落至后端，保证取消/超时路径下回滚真实执行");
    }

    [Fact]
    public async Task HandleAsync_WhenRollbackThrows_ShouldRethrowOriginalException_AndRecordOriginalFailureMetric()
    {
        // Arrange - P0-1 附加修复：回滚失败不得替换/吞没原始业务异常
        //（此前 catch(Exception) 块内 await 回滚抛出的异常会顶替原始异常向上传播，
        //  外层按 "canceled" 记指标，真实失败类型丢失。）
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("原始业务异常"));

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("回滚故障"));

        const string probeAppKey = "ws_rollback_fail_probe";
        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            null,
            new FeishuWebSocketOptions { AppKey = probeAppKey },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_rb_fail\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        var (records, listener) = StartHandlingRecordProbe(probeAppKey);
        var act = () => handler.HandleAsync(message);
        var thrown = await act.Should().ThrowAsync<InvalidOperationException>("必须传播原始业务异常，而非回滚异常");
        listener.Dispose();

        thrown.Which.Message.Should().Be("原始业务异常");

        // 指标按原始异常类型记录（回滚故障不产生额外 outcome 记录）
        var failureRecords = records.Where(r =>
            Equals(r.GetValueOrDefault(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.Outcome), "failure")).ToList();
        failureRecords.Should().ContainSingle("原始失败只记一次");
        failureRecords[0][Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.ErrorType]
            .Should().Be("InvalidOperationException", "指标必须反映原始业务异常类型");
    }

    // ===== P2-1（R2）：成功口径只记一次 =====

    [Fact]
    public async Task HandleAsync_WhenMarkCompletedThrows_ShouldRecordExactlyOneSuccessOutcome()
    {
        // Arrange - P2-1：此前 mark_completed_failed(success) 与裸 success 同时记录，EventHandlingCount 双计
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis mark failed"));

        const string probeAppKey = "ws_mark_once_probe";
        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            null,
            new FeishuWebSocketOptions { AppKey = probeAppKey },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_mark_once\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        var (records, listener) = StartHandlingRecordProbe(probeAppKey);
        var act = () => handler.HandleAsync(message);
        await act.Should().NotThrowAsync("Mark 失败按成功口径收口（WHF-07）");
        listener.Dispose();

        var successRecords = records.Where(r =>
            Equals(r.GetValueOrDefault(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.Outcome), "success")).ToList();
        successRecords.Should().ContainSingle("P2-1：成功口径只记一次（mark_completed_failed 或裸 success，二者不叠加）");
        successRecords[0][Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.ErrorType]
            .Should().Be("mark_completed_failed");
    }

    // ===== E4（R2 §9）：拦截终态的指标 =====

    [Fact]
    public async Task HandleAsync_WhenInterceptorBlocks_ShouldRecordInterceptedOutcome()
    {
        // Arrange - E4：拦截路径的状态（回滚 + 终态异常）已有用例，本用例锁定其指标口径
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        var interceptorMock = new Mock<IFeishuEventInterceptor>();
        interceptorMock
            .Setup(i => i.BeforeHandleAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        const string probeAppKey = "ws_intercept_metric_probe";
        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            [interceptorMock.Object],
            new FeishuWebSocketOptions { AppKey = probeAppKey },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_intercept_metric\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        var (records, listener) = StartHandlingRecordProbe(probeAppKey);
        var act = () => handler.HandleAsync(message);
        await act.Should().ThrowAsync<Mud.Feishu.Abstractions.EventHandlers.EventHandlingOutcomeException>();
        listener.Dispose();

        var interceptedRecords = records.Where(r =>
            Equals(r.GetValueOrDefault(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.ErrorType), "intercepted")).ToList();
        interceptedRecords.Should().ContainSingle("拦截终态必须记一次 intercepted outcome");
        interceptedRecords[0][Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.Outcome]
            .Should().Be("failure");
        interceptedRecords[0][Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.EventType]
            .Should().Be("drive.file.edit_v1");
    }

    // ===== E6①（R2 §9）：handler 自抛 OCE 且调用方令牌未取消（锁定现状） =====

    [Fact]
    public async Task HandleAsync_WhenHandlerThrowsOceWithUncancelledCallerToken_ShouldRecordCanceledOutcome()
    {
        // Arrange - 锁定现状：内层 catch(OCE) 不区分「外部取消」与「处理器自抛 OCE」，
        // 一律按 canceled 记账并回滚。审查报告 E6① 标记为评审决策点（是否对齐 Webhook 的
        // when (!cancellationToken.IsCancellationRequested) 判据）；本用例防止该行为在无决策时静默漂移。
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException("处理器内部取消"));

        var dedupMock = new Mock<Mud.Feishu.Abstractions.Services.IFeishuEventDeduplicator>();
        dedupMock
            .Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mud.Feishu.Abstractions.Services.DeduplicationResult.Success("ws"));
        dedupMock
            .Setup(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        const string probeAppKey = "ws_oce_inner_probe";
        var handler = new FeishuEventMessageHandler(
            _loggerMock.Object,
            factoryMock.Object,
            dedupMock.Object,
            null,
            new FeishuWebSocketOptions { AppKey = probeAppKey },
            null);

        var message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_oce_inner\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        var (records, listener) = StartHandlingRecordProbe(probeAppKey);
        var act = () => handler.HandleAsync(message);
        await act.Should().ThrowAsync<OperationCanceledException>();
        listener.Dispose();

        var canceledRecords = records.Where(r =>
            Equals(r.GetValueOrDefault(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.ErrorType), "canceled")).ToList();
        canceledRecords.Should().ContainSingle("内层收口，取消终态只记一次");
    }

    // ===== E9（R2 §9）：v2 Event 元素 Clone 脱离 JsonDocument 生命周期 =====

    [Fact]
    public async Task HandleAsync_WithV2Event_EventShouldBeReadable_AfterParseScopeDisposes()
    {
        // Arrange - E9/P1-10：eventElement 隶属于 using jsonDoc 的池化缓冲，Clone() 使其脱离文档
        // 生命周期。HandleAsync 返回即文档已 Dispose——此后读取 eventData.Event 必须安全。
        // 若有人移除 Clone()（直接赋值 JsonElement），本用例将抛 ObjectDisposedException 或读到脏内存。
        var handler = CreateHandler();
        var v2Message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_clone_life\",\"event_type\":\"drive.file.edit_v1\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{\"file_token\":\"ft_clone_123\",\"user\":\"u1\"}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(v2Message);

        // Assert：HandleAsync 已返回（jsonDoc 已 Dispose），Event 必须仍是可读的独立 JsonElement
        capturedEventData.Should().NotBeNull();
        capturedEventData!.Event.Should().BeOfType<System.Text.Json.JsonElement>();
        var element = (System.Text.Json.JsonElement)capturedEventData.Event!;
        element.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
        element.TryGetProperty("file_token", out var fileToken).Should().BeTrue();
        fileToken.GetString().Should().Be("ft_clone_123", "Clone 后元素内容必须完整可读");
    }

    [Fact]
    public async Task HandleAsync_WithV2Event_NonNumericCreateTime_ShouldNotThrow_AndCreateTimeIsZero()
    {
        // Arrange - E9：create_time 非数字字符串（既非毫秒数也非纯数字）时不抛异常，
        // EventData.CreateTime 静默为 0，Header.CreateTime 保留原文字段
        var handler = CreateHandler();
        var v2Message = "{\"schema\":\"2.0\",\"header\":{\"event_id\":\"evt_ct_bad\",\"event_type\":\"test.event\",\"create_time\":\"not-a-number\",\"tenant_key\":\"tk\",\"app_id\":\"app\"},\"event\":{}}";

        EventData? capturedEventData = null;
        _handlerFactoryMock
            .Setup(f => f.HandleEventParallelAsync(It.IsAny<string>(), It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Callback<string, EventData, CancellationToken>((_, ed, _) => capturedEventData = ed)
            .Returns(Task.CompletedTask);

        var act = () => handler.HandleAsync(v2Message);

        await act.Should().NotThrowAsync();
        capturedEventData.Should().NotBeNull();
        capturedEventData!.CreateTime.Should().Be(0, "非数字 create_time 不参与秒换算，静默为 0");
        capturedEventData.Header!.CreateTime.Should().Be("not-a-number", "Header 保留原始字符串供业务侧自查");
    }

    /// <summary>
    /// 进程级指标探针：订阅 feishu.event.handling 并采集 probeAppKey 维度的全部记录。
    /// </summary>
    /// <remarks>
    /// MeterListener 是进程级观测通道，各用例必须用独立 AppKey 与同程序集其它用例隔离。
    /// 返回的记录含 RecordEventHandling（无 Outcome 标签）与 RecordEventOutcome（含 Outcome/ErrorType）
    /// 两类写入，断言侧按需过滤。
    /// </remarks>
    private static (List<Dictionary<string, object?>> Records, System.Diagnostics.Metrics.MeterListener Listener)
        StartHandlingRecordProbe(string probeAppKey)
    {
        var records = new List<Dictionary<string, object?>>();
        var listener = new System.Diagnostics.Metrics.MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == Mud.Feishu.Abstractions.Metrics.FeishuMetrics.MeterName &&
                    instrument.Name == "feishu.event.handling")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var captured = new Dictionary<string, object?>();
            foreach (var tag in tags)
            {
                captured[tag.Key] = tag.Value;
            }

            if (Equals(captured.GetValueOrDefault(Mud.Feishu.Abstractions.Metrics.FeishuMetrics.Tags.AppKey), probeAppKey))
                records.Add(captured);
        });
        listener.Start();
        return (records, listener);
    }
}
