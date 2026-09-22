// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Mud.Feishu.Abstractions.Tests.EventHandlers;

/// <summary>
/// DefaultFeishuEventHandlerFactory 单元测试
/// </summary>
public class DefaultFeishuEventHandlerFactoryTests
{
    private readonly Mock<ILogger<DefaultFeishuEventHandlerFactory>> _loggerMock;
    private readonly Mock<IFeishuEventHandler> _defaultHandlerMock;
    private readonly Mock<IFeishuEventHandler> _handler1Mock;
    private readonly Mock<IFeishuEventHandler> _handler2Mock;

    public DefaultFeishuEventHandlerFactoryTests()
    {
        _loggerMock = new Mock<ILogger<DefaultFeishuEventHandlerFactory>>();
        _defaultHandlerMock = new Mock<IFeishuEventHandler>();
        _handler1Mock = new Mock<IFeishuEventHandler>();
        _handler2Mock = new Mock<IFeishuEventHandler>();

        _defaultHandlerMock.Setup(h => h.SupportedEventType).Returns("default");
        _handler1Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        _handler2Mock.Setup(h => h.SupportedEventType).Returns("test.event.type2");
    }

    [Fact]
    public void Constructor_ShouldRegisterAllHandlers()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };

        // Act
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Assert
        Assert.True(factory.IsHandlerRegistered("test.event.type1"));
        Assert.True(factory.IsHandlerRegistered("test.event.type2"));
    }

    [Fact]
    public void GetHandler_WhenEventTypeExists_ShouldReturnCorrectHandler()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandler("test.event.type1");

        // Assert
        Assert.Equal(_handler1Mock.Object, result);
    }

    [Fact]
    public void GetHandler_WhenEventTypeNotExists_ShouldReturnDefaultHandler()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandler("non.existent.type");

        // Assert
        Assert.Equal(_defaultHandlerMock.Object, result);
    }

    [Fact]
    public void GetHandlers_WhenEventTypeExists_ShouldReturnAllMatchingHandlers()
    {
        // Arrange
        var handler3Mock = new Mock<IFeishuEventHandler>();
        handler3Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");

        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, handler3Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandlers("test.event.type1");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(_handler1Mock.Object, result);
        Assert.Contains(handler3Mock.Object, result);
    }

    [Fact]
    public void RegisterHandler_ShouldAddHandlerToFactory()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler>();
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        factory.RegisterHandler(_handler1Mock.Object);

        // Assert
        Assert.True(factory.IsHandlerRegistered("test.event.type1"));
    }

    [Fact]
    public void RegisterHandler_WhenHandlerIsNull_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler>();
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.RegisterHandler(null!));
    }

    [Fact]
    public void UnregisterHandler_WhenHandlerExists_ShouldReturnTrue()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.UnregisterHandler(_handler1Mock.Object);

        // Assert
        Assert.True(result);
        Assert.False(factory.IsHandlerRegistered("test.event.type1"));
    }

    [Fact]
    public void UnregisterHandler_WhenHandlerNotExists_ShouldReturnFalse()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler>();
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.UnregisterHandler(_handler1Mock.Object);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void UnregisterHandlerByEventType_WhenEventTypeExists_ShouldReturnTrue()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.UnregisterHandler("test.event.type1");

        // Assert
        Assert.True(result);
        Assert.False(factory.IsHandlerRegistered("test.event.type1"));
    }

    [Fact]
    public void GetRegisteredEventTypes_ShouldReturnAllRegisteredTypes()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetRegisteredEventTypes();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("test.event.type1", result);
        Assert.Contains("test.event.type2", result);
    }

    [Fact]
    public async Task HandleEventParallelAsync_ShouldCallAllHandlers()
    {
        // Arrange
        var handler3Mock = new Mock<IFeishuEventHandler>();
        handler3Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");

        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, handler3Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.event.type1"
        };

        // Act
        await factory.HandleEventParallelAsync("test.event.type1", eventData, CancellationToken.None);

        // Assert
        _handler1Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
        handler3Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleEventParallelAsync_WhenHandlerThrows_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        var handler3Mock = new Mock<IFeishuEventHandler>();
        handler3Mock.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        handler3Mock.Setup(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, handler3Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        var eventData = new EventData
        {
            EventId = "test-event-id",
            EventType = "test.event.type1"
        };

        // Act - 异常现在会被传播给调用方，以便正确回滚去重状态
        // 但 Task.WhenAll 仍会等待所有并行任务完成，所以其他处理器不受影响
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.HandleEventParallelAsync("test.event.type1", eventData, CancellationToken.None));

        // Assert - 两个处理器都应该被调用（异常不会中断其他并行处理器）
        _handler1Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
        handler3Mock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== E2（R2 §9）：扇出部分失败 → 成功处理器已执行的回归锁定 =====

    [Fact]
    public async Task HandleEventParallelAsync_WhenOneOfManyFails_OtherHandlersComplete_BeforeRollback()
    {
        // Arrange：锁定 at-least-once 事实——任一处理器失败时，Task.WhenAll 已等待全部任务
        // 完成，失败前成功执行完毕的处理器不会被中断。调用方随后的去重回滚 + 服务端重发
        // 将使其再次执行（处理器必须幂等，P2-7）。
        var completionSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var successCount = 0;

        var successMock = new Mock<IFeishuEventHandler>();
        successMock.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        successMock
            .Setup(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(async (EventData _, CancellationToken _) =>
            {
                await Task.Yield();
                Interlocked.Increment(ref successCount);
                completionSignal.TrySetResult();
            });

        var failingMock = new Mock<IFeishuEventHandler>();
        failingMock.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        failingMock
            .Setup(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()))
            .Returns(async (EventData _, CancellationToken _) =>
            {
                // 等成功处理器完整跑完后再失败：确保异常传播时成功处理器已收尾
                await completionSignal.Task;
                throw new InvalidOperationException("fanout partial failure");
            });

        var handlers = new List<IFeishuEventHandler> { successMock.Object, failingMock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        var eventData = new EventData
        {
            EventId = "test-event-e2",
            EventType = "test.event.type1"
        };

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            factory.HandleEventParallelAsync("test.event.type1", eventData, CancellationToken.None));

        // Assert：失败前成功处理器恰好执行 1 次（重发后将被再次执行——at-least-once）
        successCount.Should().Be(1, "失败处理器不得中断已成功的并行处理器");
        successMock.Verify(h => h.HandleAsync(It.IsAny<EventData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ClearHandlers_ShouldRemoveAllHandlers()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        factory.ClearHandlers();

        // Assert
        Assert.Empty(factory.GetRegisteredEventTypes());
    }

    [Fact]
    public void GetHandlerInfo_ShouldReturnHandlerInformation()
    {
        // Arrange
        var handlers = new List<IFeishuEventHandler> { _handler1Mock.Object, _handler2Mock.Object };
        var factory = new DefaultFeishuEventHandlerFactory(_loggerMock.Object, handlers, _defaultHandlerMock.Object);

        // Act
        var result = factory.GetHandlerInfo();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("test.event.type1"));
        Assert.True(result.ContainsKey("test.event.type2"));
    }

    // ===== P1-4：空类型守卫不得依赖日志级别 =====

    [Fact]
    public void GetHandlers_ShouldReturnDefaultHandler_WhenEventTypeNull_WithNullLogger()
    {
        var factory = new DefaultFeishuEventHandlerFactory(
            NullLogger<DefaultFeishuEventHandlerFactory>.Instance,
            [_handler1Mock.Object],
            _defaultHandlerMock.Object);

        var result = factory.GetHandlers(null!);

        Assert.Single(result);
        Assert.Same(_defaultHandlerMock.Object, result[0]);
    }

    [Fact]
    public void GetHandlers_ShouldReturnDefaultHandler_WhenEventTypeEmpty_WithNullLogger()
    {
        var factory = new DefaultFeishuEventHandlerFactory(
            NullLogger<DefaultFeishuEventHandlerFactory>.Instance,
            [_handler1Mock.Object],
            _defaultHandlerMock.Object);

        var result = factory.GetHandlers("");

        Assert.Single(result);
        Assert.Same(_defaultHandlerMock.Object, result[0]);
    }

    [Fact]
    public void RegisterHandler_ShouldNotThrow_WhenSupportedEventTypeNull_WithNullLogger()
    {
        var factory = new DefaultFeishuEventHandlerFactory(
            NullLogger<DefaultFeishuEventHandlerFactory>.Instance,
            [],
            _defaultHandlerMock.Object);

        var badHandler = new Mock<IFeishuEventHandler>();
        badHandler.Setup(h => h.SupportedEventType).Returns((string)null!);

        var act = () => factory.RegisterHandler(badHandler.Object);

        act.Should().NotThrow();
        factory.GetRegisteredEventTypes().Should().BeEmpty();
    }

    [Fact]
    public void RegisterHandler_ShouldIgnoreDuplicateInstance()
    {
        var factory = new DefaultFeishuEventHandlerFactory(
            _loggerMock.Object,
            [_handler1Mock.Object],
            _defaultHandlerMock.Object);

        factory.RegisterHandler(_handler1Mock.Object);

        var handlers = factory.GetHandlers("test.event.type1");
        handlers.Count(h => ReferenceEquals(h, _handler1Mock.Object)).Should().Be(1);
    }

    [Fact]
    public async Task Registry_ShouldNotThrow_WhenMutatedConcurrentlyWithDispatch()
    {
        // P1-5：注册表全部读写必须共用同一把锁。
        // 此前仅 GetHandlers/RegisterHandler 入锁，UnregisterHandler ×2 / ClearHandlers 未入锁，
        // 因而锁不提供任何互斥语义——快照 ToArray() 与 List 修改可并发，产生不一致结果或异常。
        var factory = new DefaultFeishuEventHandlerFactory(
            NullLogger<DefaultFeishuEventHandlerFactory>.Instance,
            new List<IFeishuEventHandler> { _handler1Mock.Object },
            _defaultHandlerMock.Object);

        var extra = new Mock<IFeishuEventHandler>();
        extra.Setup(h => h.SupportedEventType).Returns("test.event.type1");

        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var stopped = 0;

        var writer = Task.Run(() =>
        {
            try
            {
                for (var i = 0; i < 5_000; i++)
                {
                    factory.RegisterHandler(extra.Object);
                    factory.UnregisterHandler(extra.Object);
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
            finally
            {
                Volatile.Write(ref stopped, 1);
            }
        });

        var readers = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
        {
            try
            {
                while (Volatile.Read(ref stopped) == 0)
                {
                    factory.GetHandlers("test.event.type1");
                    factory.GetHandler("test.event.type1");
                    factory.GetHandlerInfo();
                    factory.GetRegisteredEventTypes();
                    factory.IsHandlerRegistered("test.event.type1");
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        })).ToArray();

        await Task.WhenAll(readers.Append(writer));

        errors.Should().BeEmpty("P1-5：注册表读写必须线程安全（不得因并发修改抛异常）");
    }

    [Fact]
    public void GetHandlers_ShouldNotBeAffected_WhenListMutatedAfterReturn()
    {
        var factory = new DefaultFeishuEventHandlerFactory(
            _loggerMock.Object,
            [_handler1Mock.Object],
            _defaultHandlerMock.Object);

        var snapshot = factory.GetHandlers("test.event.type1");
        var countBefore = snapshot.Count;

        factory.RegisterHandler(_handler2Mock.Object);
        // 即便后续向同类型再注册（通过另一实例），已返回快照长度不应变化
        // （此处 _handler2 类型不同；用同类型再注册验证快照）
        var handler1Dup = new Mock<IFeishuEventHandler>();
        handler1Dup.Setup(h => h.SupportedEventType).Returns("test.event.type1");
        factory.RegisterHandler(handler1Dup.Object);

        snapshot.Count.Should().Be(countBefore, "GetHandlers 应返回不可变快照");
    }
}
