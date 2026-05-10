// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// EventSubscriptionManager 事件订阅管理器测试类
/// </summary>
public class EventSubscriptionManagerTests
{
    private readonly Mock<ILogger<EventSubscriptionManager>> _loggerMock;
    private readonly FeishuWebSocketOptions _options;
    private readonly Mock<Func<string, Task>> _sendMessageCallbackMock;
    private readonly EventSubscriptionManager _subscriptionManager;

    public EventSubscriptionManagerTests()
    {
        _loggerMock = new Mock<ILogger<EventSubscriptionManager>>();
        _options = new FeishuWebSocketOptions();
        _sendMessageCallbackMock = new Mock<Func<string, Task>>();
        _sendMessageCallbackMock.Setup(f => f(It.IsAny<string>())).Returns(Task.CompletedTask);
        _subscriptionManager = new EventSubscriptionManager(_loggerMock.Object, _options, _sendMessageCallbackMock.Object);
    }

    [Fact]
    public void SubscribeEvent_ShouldThrowArgumentException_WhenEventTypeIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _subscriptionManager.SubscribeEvent(""));
        Assert.Throws<ArgumentException>(() => _subscriptionManager.SubscribeEvent(null!));
        Assert.Throws<ArgumentException>(() => _subscriptionManager.SubscribeEvent("   "));
    }

    [Fact]
    public void SubscribeEvent_ShouldAddEventType_WhenEventTypeIsValid()
    {
        // Arrange
        var eventType = "im.message.receive_v1";

        // Act
        _subscriptionManager.SubscribeEvent(eventType);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().ContainSingle();
        events.Should().Contain(eventType);
    }

    [Fact]
    public void SubscribeEvent_ShouldAddUniqueEventTypes()
    {
        // Arrange
        var eventType1 = "im.message.receive_v1";
        var eventType2 = "contact.user.created_v3";
        var eventType3 = "im.message.message_read_v1";

        // Act
        _subscriptionManager.SubscribeEvent(eventType1);
        _subscriptionManager.SubscribeEvent(eventType2);
        _subscriptionManager.SubscribeEvent(eventType3);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().HaveCount(3);
        events.Should().Contain(eventType1);
        events.Should().Contain(eventType2);
        events.Should().Contain(eventType3);
    }

    [Fact]
    public void SubscribeEvent_ShouldHandleDuplicateEventType()
    {
        // Arrange
        var eventType = "im.message.receive_v1";

        // Act
        _subscriptionManager.SubscribeEvent(eventType);
        _subscriptionManager.SubscribeEvent(eventType);
        _subscriptionManager.SubscribeEvent(eventType);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().ContainSingle();
        events.Should().Contain(eventType);
    }

    [Fact]
    public void SubscribeEvents_ShouldThrowArgumentNullException_WhenEventTypesIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _subscriptionManager.SubscribeEvents(null!));
    }

    [Fact]
    public void SubscribeEvents_ShouldAddAllEventTypes()
    {
        // Arrange
        var eventTypes = new[]
        {
            "im.message.receive_v1",
            "contact.user.created_v3",
            "im.message.message_read_v1"
        };

        // Act
        _subscriptionManager.SubscribeEvents(eventTypes);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().HaveCount(3);
        foreach (var eventType in eventTypes)
        {
            events.Should().Contain(eventType);
        }
    }

    [Fact]
    public void SubscribeEvents_ShouldHandleEmptyCollection()
    {
        // Arrange
        var eventTypes = Array.Empty<string>();

        // Act
        _subscriptionManager.SubscribeEvents(eventTypes);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().BeEmpty();
    }

    [Fact]
    public void GetSubscribedEvents_ShouldReturnEmptyArray_WhenNoEventsSubscribed()
    {
        // Act
        var events = _subscriptionManager.GetSubscribedEvents();

        // Assert
        events.Should().NotBeNull();
        events.Should().BeEmpty();
    }

    [Fact]
    public void HasSubscribed_ShouldBeFalse_Initially()
    {
        // Act & Assert
        _subscriptionManager.HasSubscribed.Should().BeFalse();
    }

    [Fact]
    public async Task SendSubscriptionRequestAsync_ShouldSendMessage_WhenEventsSubscribed()
    {
        // Arrange
        var eventType = "im.message.receive_v1";
        _subscriptionManager.SubscribeEvent(eventType);

        // Act
        await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);

        // Assert
        _sendMessageCallbackMock.Verify(f => f(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SendSubscriptionRequestAsync_ShouldNotSendMessage_WhenNoEventsSubscribed()
    {
        // Act
        await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);

        // Assert
        _sendMessageCallbackMock.Verify(f => f(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendSubscriptionRequestAsync_ShouldSetHasSubscribedToTrue()
    {
        // Arrange
        var eventType = "im.message.receive_v1";
        _subscriptionManager.SubscribeEvent(eventType);

        // Act
        await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);

        // Assert
        _subscriptionManager.HasSubscribed.Should().BeTrue();
    }

    [Fact]
    public async Task SendSubscriptionRequestAsync_ShouldRaiseSubscribedEvent_WhenSuccess()
    {
        // Arrange
        var eventTypes = new[] { "im.message.receive_v1", "contact.user.created_v3" };
        _subscriptionManager.SubscribeEvents(eventTypes);

        var eventRaised = false;
        SubscriptionEventArgs? eventArgs = null;
        _subscriptionManager.Subscribed += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);

        // Assert
        eventRaised.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.EventTypes.Should().BeEquivalentTo(eventTypes);
        eventArgs.IsSuccess.Should().BeTrue();
        eventArgs.Message.Should().Be("订阅请求已发送");
    }

    [Fact]
    public async Task SendSubscriptionRequestAsync_ShouldThrow_WhenSendMessageCallbackThrows()
    {
        // Arrange
        var eventType = "im.message.receive_v1";
        _subscriptionManager.SubscribeEvent(eventType);
        _sendMessageCallbackMock.Setup(f => f(It.IsAny<string>())).ThrowsAsync(new Exception("Send failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None));
    }

    [Fact]
    public async Task SendSubscriptionRequestAsync_ShouldRaiseSubscriptionFailedEvent_WhenExceptionOccurs()
    {
        // Arrange
        var eventType = "im.message.receive_v1";
        _subscriptionManager.SubscribeEvent(eventType);
        _sendMessageCallbackMock.Setup(f => f(It.IsAny<string>())).ThrowsAsync(new Exception("Send failed"));

        var eventRaised = false;
        SubscriptionEventArgs? eventArgs = null;
        _subscriptionManager.SubscriptionFailed += (sender, args) =>
        {
            eventRaised = true;
            eventArgs = args;
        };

        // Act
        try
        {
            await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);
        }
        catch
        {
            // Expected exception
        }

        // Assert
        eventRaised.Should().BeTrue();
        eventArgs.Should().NotBeNull();
        eventArgs!.EventTypes.Should().Contain(eventType);
        eventArgs.IsSuccess.Should().BeFalse();
        eventArgs.Message.Should().Contain("订阅失败");
    }

    [Fact]
    public void ClearSubscriptions_ShouldRemoveAllEventTypes()
    {
        // Arrange
        var eventTypes = new[] { "im.message.receive_v1", "contact.user.created_v3" };
        _subscriptionManager.SubscribeEvents(eventTypes);

        // Act
        _subscriptionManager.ClearSubscriptions();

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().BeEmpty();
    }

    [Fact]
    public void ClearSubscriptions_ShouldResetHasSubscribed()
    {
        // Arrange
        _subscriptionManager.SubscribeEvent("im.message.receive_v1");
        _subscriptionManager.HasSubscribed.Should().BeFalse();

        // Act
        _subscriptionManager.ClearSubscriptions();

        // Assert
        _subscriptionManager.HasSubscribed.Should().BeFalse();
    }

    [Fact]
    public async Task ClearSubscriptions_ShouldAllowNewSubscriptions()
    {
        // Arrange
        var eventType1 = "im.message.receive_v1";
        _subscriptionManager.SubscribeEvent(eventType1);
        await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);

        // Act
        _subscriptionManager.ClearSubscriptions();
        var eventType2 = "contact.user.created_v3";
        _subscriptionManager.SubscribeEvent(eventType2);
        await _subscriptionManager.SendSubscriptionRequestAsync(CancellationToken.None);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().ContainSingle();
        events.Should().Contain(eventType2);
        events.Should().NotContain(eventType1);
    }

    [Fact]
    public void SubscribeEvents_ShouldHandleMixedEventTypes()
    {
        // Arrange
        var eventTypes = new[]
        {
            "im.message.receive_v1",
            "contact.user.created_v3",
            "im.chat.member.user_added_v1",
            "im.chat.member.user_deleted_v1",
            "approval.approval.instance_created_v1"
        };

        // Act
        _subscriptionManager.SubscribeEvents(eventTypes);

        // Assert
        var events = _subscriptionManager.GetSubscribedEvents();
        events.Should().HaveCount(5);
        foreach (var eventType in eventTypes)
        {
            events.Should().Contain(eventType);
        }
    }
}
