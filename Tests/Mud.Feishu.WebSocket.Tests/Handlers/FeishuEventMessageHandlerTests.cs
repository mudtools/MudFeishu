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
}
