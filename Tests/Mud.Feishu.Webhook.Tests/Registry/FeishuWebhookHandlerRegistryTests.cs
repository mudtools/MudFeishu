// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Abstractions.EventHandlers;

namespace Mud.Feishu.Webhook.Tests.Registry;

/// <summary>
/// FeishuWebhookHandlerRegistry 单元测试
/// </summary>
public class FeishuWebhookHandlerRegistryTests
{
    [Fact]
    public void Register_WithValidParameters_ShouldRegisterHandler()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        var handlerType = typeof(ITestFeishuEventHandler);

        // Act
        registry.Register("app-001", handlerType);

        // Assert
        var handlers = registry.GetHandlers("app-001");
        handlers.Should().HaveCount(1);
        handlers.Should().Contain(handlerType);
    }

    [Fact]
    public void Register_WithEmptyAppKey_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        var handlerType = typeof(ITestFeishuEventHandler);

        // Act & Assert
        var action = () => registry.Register("", handlerType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*应用键不能为空*");
    }

    [Fact]
    public void Register_WithNullAppKey_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        var handlerType = typeof(ITestFeishuEventHandler);

        // Act & Assert
        var action = () => registry.Register(null!, handlerType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*应用键不能为空*");
    }

    [Fact]
    public void Register_MultipleHandlers_ShouldAddAll()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        var handlerType1 = typeof(ITestFeishuEventHandler);
        var handlerType2 = typeof(ITestFeishuEventHandler2);

        // Act
        registry.Register("app-001", handlerType1);
        registry.Register("app-001", handlerType2);

        // Assert
        var handlers = registry.GetHandlers("app-001");
        handlers.Should().HaveCount(2);
    }

    [Fact]
    public void Register_MultipleApps_ShouldSeparateByAppKey()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        var handlerType = typeof(ITestFeishuEventHandler);

        // Act
        registry.Register("app-001", handlerType);
        registry.Register("app-002", handlerType);

        // Assert
        registry.GetHandlers("app-001").Should().HaveCount(1);
        registry.GetHandlers("app-002").Should().HaveCount(1);
    }

    [Fact]
    public void GetHandlers_WithNoHandlers_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();

        // Act
        var handlers = registry.GetHandlers("app-001");

        // Assert
        handlers.Should().BeEmpty();
    }

    [Fact]
    public void GetHandlers_WithRegisteredApp_ShouldReturnHandlers()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        var handlerType = typeof(ITestFeishuEventHandler);
        registry.Register("app-001", handlerType);

        // Act
        var handlers = registry.GetHandlers("app-001");

        // Assert
        handlers.Should().HaveCount(1);
    }

    [Fact]
    public void GetAllAppKeys_WithNoApps_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();

        // Act
        var appKeys = registry.GetAllAppKeys();

        // Assert
        appKeys.Should().BeEmpty();
    }

    [Fact]
    public void GetAllAppKeys_WithRegisteredApps_ShouldReturnAllKeys()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        registry.Register("app-001", typeof(ITestFeishuEventHandler));
        registry.Register("app-002", typeof(ITestFeishuEventHandler));
        registry.Register("app-003", typeof(ITestFeishuEventHandler));

        // Act
        var appKeys = registry.GetAllAppKeys();

        // Assert
        appKeys.Should().HaveCount(3);
        appKeys.Should().Contain("app-001");
        appKeys.Should().Contain("app-002");
        appKeys.Should().Contain("app-003");
    }

    [Fact]
    public void HasHandlers_WithNoHandlers_ShouldReturnFalse()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();

        // Act
        var hasHandlers = registry.HasHandlers("app-001");

        // Assert
        hasHandlers.Should().BeFalse();
    }

    [Fact]
    public void HasHandlers_WithRegisteredHandlers_ShouldReturnTrue()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        registry.Register("app-001", typeof(ITestFeishuEventHandler));

        // Act
        var hasHandlers = registry.HasHandlers("app-001");

        // Assert
        hasHandlers.Should().BeTrue();
    }

    [Fact]
    public void HasHandlers_WithUnregisteredApp_ShouldReturnFalse()
    {
        // Arrange
        var registry = new FeishuWebhookHandlerRegistry();
        registry.Register("app-001", typeof(ITestFeishuEventHandler));

        // Act
        var hasHandlers = registry.HasHandlers("app-002");

        // Assert
        hasHandlers.Should().BeFalse();
    }

    // Test handler interfaces for testing
    private interface ITestFeishuEventHandler : IFeishuEventHandler { }
    private interface ITestFeishuEventHandler2 : IFeishuEventHandler { }
}
