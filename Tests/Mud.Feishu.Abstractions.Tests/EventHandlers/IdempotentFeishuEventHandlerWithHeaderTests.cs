// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Mud.Feishu.Abstractions.Services;

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

public class IdempotentFeishuEventHandlerWithHeaderTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<IFeishuEventDeduplicator> _deduplicatorMock;

    public IdempotentFeishuEventHandlerWithHeaderTests()
    {
        _loggerMock = new Mock<ILogger>();
        _deduplicatorMock = new Mock<IFeishuEventDeduplicator>();
    }

    public class TestEventData : IEventResult
    {
        public string? UserId { get; set; }
    }

    public class TestHeaderHandler : IdempotentFeishuEventHandler<TestEventData, FeishuEventHeader>
    {
        public TestHeaderHandler(
            IFeishuEventDeduplicator businessDeduplicator,
            ILogger logger,
            IAppKeyAccessor? appKeyAccessor = null)
            : base(businessDeduplicator, logger, appKeyAccessor)
        {
        }

        public override string SupportedEventType => "test.header.event";

        public EventData? LastEventData { get; private set; }
        public TestEventData? LastEventEntity { get; private set; }
        public FeishuEventHeader? LastHeader { get; private set; }

        protected override Task ProcessBusinessLogicAsync(
            EventData eventData,
            TestEventData? eventEntity,
            FeishuEventHeader? header,
            CancellationToken cancellationToken = default)
        {
            LastEventData = eventData;
            LastEventEntity = eventEntity;
            LastHeader = header;
            return Task.CompletedTask;
        }
    }

    public class TestHeaderHandlerWithoutOverride : IdempotentFeishuEventHandler<TestEventData, FeishuEventHeader>
    {
        public TestHeaderHandlerWithoutOverride(
            IFeishuEventDeduplicator businessDeduplicator,
            ILogger logger,
            IAppKeyAccessor? appKeyAccessor = null)
            : base(businessDeduplicator, logger, appKeyAccessor)
        {
        }

        public override string SupportedEventType => "test.header.no_override";

        public bool HeaderProcessBusinessLogicCalled { get; private set; }
        public FeishuEventHeader? LastHeader { get; private set; }

        protected override Task ProcessBusinessLogicAsync(
            EventData eventData,
            TestEventData? eventEntity,
            FeishuEventHeader? header,
            CancellationToken cancellationToken = default)
        {
            HeaderProcessBusinessLogicCalled = true;
            LastHeader = header;
            return Task.CompletedTask;
        }
    }

    private EventData CreateV2EventData()
    {
        return new EventData
        {
            EventId = "evt_v2_001",
            EventType = "test.header.event",
            AppId = "cli_test",
            TenantKey = "tk_test",
            CreateTime = 1704067200,
            Header = new FeishuEventHeader
            {
                Schema = "2.0",
                EventId = "evt_v2_001",
                EventType = "test.header.event",
                Token = "token_v2_abc",
                CreateTime = "1704067200000",
                TenantKey = "tk_test",
                AppId = "cli_test"
            },
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData { UserId = "user_001" }))
        };
    }

    [Fact]
    public async Task HandleAsync_ShouldDeserializeHeader_WhenHeaderIsPresent()
    {
        var handler = new TestHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = CreateV2EventData();
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        handler.LastHeader.Should().NotBeNull();
        handler.LastHeader!.Schema.Should().Be("2.0");
        handler.LastHeader.EventId.Should().Be("evt_v2_001");
        handler.LastHeader.Token.Should().Be("token_v2_abc");
        handler.LastHeader.EventType.Should().Be("test.header.event");
        handler.LastHeader.TenantKey.Should().Be("tk_test");
        handler.LastHeader.AppId.Should().Be("cli_test");
    }

    [Fact]
    public async Task HandleAsync_ShouldPassNullHeader_WhenHeaderIsNull()
    {
        var handler = new TestHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = new EventData
        {
            EventId = "evt_v1_001",
            EventType = "test.header.event",
            Header = null,
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData { UserId = "user_001" }))
        };
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        handler.LastHeader.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_BackwardCompat_ShouldCallHeaderMethodWithHeader_WhenUsingBaseDefault()
    {
        var handler = new TestHeaderHandlerWithoutOverride(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = CreateV2EventData();
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        handler.HeaderProcessBusinessLogicCalled.Should().BeTrue();
        handler.LastHeader.Should().NotBeNull();
        handler.LastHeader!.EventId.Should().Be("evt_v2_001");
    }

    [Fact]
    public async Task HandleAsync_ShouldPassEventDataAndEntity_WhenHeaderIsPresent()
    {
        var handler = new TestHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = CreateV2EventData();
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        handler.LastEventData.Should().NotBeNull();
        handler.LastEventData!.EventId.Should().Be("evt_v2_001");
        handler.LastEventEntity.Should().NotBeNull();
        handler.LastEventEntity!.UserId.Should().Be("user_001");
    }

    [Fact]
    public async Task HandleAsync_ShouldPassNullHeader_WhenHeaderIsNullOnEventData()
    {
        var handler = new TestHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = new EventData
        {
            EventId = "evt_no_header",
            EventType = "test.header.event",
            Header = null,
            Event = JsonDocument.Parse(JsonSerializer.Serialize(new TestEventData { UserId = "user_002" }))
        };
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        handler.LastHeader.Should().BeNull();
        handler.LastEventEntity.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldSupportIdempotency_WithHeader()
    {
        var handler = new TestHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = CreateV2EventData();
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Duplicate("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        handler.LastEventData.Should().BeNull();
        handler.LastHeader.Should().BeNull();
        _deduplicatorMock.Verify(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ShouldMarkCompleted_WhenProcessingSucceedsWithHeader()
    {
        var handler = new TestHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = CreateV2EventData();
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await handler.HandleAsync(eventData, CancellationToken.None);

        _deduplicatorMock.Verify(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldRollback_WhenProcessingFailsWithHeader()
    {
        var handler = new FailingHeaderHandler(_deduplicatorMock.Object, _loggerMock.Object);
        var eventData = CreateV2EventData();
        _deduplicatorMock.Setup(d => d.TryMarkAsProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>())).ReturnsAsync(DeduplicationResult.Success("test"));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(eventData, CancellationToken.None));

        _deduplicatorMock.Verify(d => d.RollbackProcessingAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        _deduplicatorMock.Verify(d => d.MarkAsCompletedAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public class FailingHeaderHandler : IdempotentFeishuEventHandler<TestEventData, FeishuEventHeader>
    {
        public FailingHeaderHandler(IFeishuEventDeduplicator businessDeduplicator, ILogger logger)
            : base(businessDeduplicator, logger) { }

        public override string SupportedEventType => "test.failing.header.event";

        protected override Task ProcessBusinessLogicAsync(
            EventData eventData, TestEventData? eventEntity,
            FeishuEventHeader? header, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Business logic failed");
        }
    }
}
