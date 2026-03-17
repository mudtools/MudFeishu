// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.Webhook.Tests.Registry;

/// <summary>
/// FeishuWebhookInterceptorRegistry 单元测试
/// </summary>
public class FeishuWebhookInterceptorRegistryTests
{
    [Fact]
    public void Register_WithValidParameters_ShouldRegisterInterceptor()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        var interceptorType = typeof(ITestInterceptor);

        // Act
        registry.Register("app-001", interceptorType);

        // Assert
        var interceptors = registry.GetInterceptors("app-001");
        interceptors.Should().HaveCount(1);
        interceptors.Should().Contain(interceptorType);
    }

    [Fact]
    public void Register_WithEmptyAppKey_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        var interceptorType = typeof(ITestInterceptor);

        // Act & Assert
        var action = () => registry.Register("", interceptorType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*应用键不能为空*");
    }

    [Fact]
    public void Register_WithNullAppKey_ShouldThrowArgumentException()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        var interceptorType = typeof(ITestInterceptor);

        // Act & Assert
        var action = () => registry.Register(null!, interceptorType);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*应用键不能为空*");
    }

    [Fact]
    public void Register_MultipleInterceptors_ShouldAddAll()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        var interceptorType1 = typeof(ITestInterceptor);
        var interceptorType2 = typeof(ITestInterceptor2);

        // Act
        registry.Register("app-001", interceptorType1);
        registry.Register("app-001", interceptorType2);

        // Assert
        var interceptors = registry.GetInterceptors("app-001");
        interceptors.Should().HaveCount(2);
    }

    [Fact]
    public void Register_MultipleApps_ShouldSeparateByAppKey()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        var interceptorType = typeof(ITestInterceptor);

        // Act
        registry.Register("app-001", interceptorType);
        registry.Register("app-002", interceptorType);

        // Assert
        registry.GetInterceptors("app-001").Should().HaveCount(1);
        registry.GetInterceptors("app-002").Should().HaveCount(1);
    }

    [Fact]
    public void GetInterceptors_WithNoInterceptors_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();

        // Act
        var interceptors = registry.GetInterceptors("app-001");

        // Assert
        interceptors.Should().BeEmpty();
    }

    [Fact]
    public void GetInterceptors_WithRegisteredApp_ShouldReturnInterceptors()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        var interceptorType = typeof(ITestInterceptor);
        registry.Register("app-001", interceptorType);

        // Act
        var interceptors = registry.GetInterceptors("app-001");

        // Assert
        interceptors.Should().HaveCount(1);
    }

    [Fact]
    public void GetAllAppKeys_WithNoApps_ShouldReturnEmptyList()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();

        // Act
        var appKeys = registry.GetAllAppKeys();

        // Assert
        appKeys.Should().BeEmpty();
    }

    [Fact]
    public void GetAllAppKeys_WithRegisteredApps_ShouldReturnAllKeys()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        registry.Register("app-001", typeof(ITestInterceptor));
        registry.Register("app-002", typeof(ITestInterceptor));
        registry.Register("app-003", typeof(ITestInterceptor));

        // Act
        var appKeys = registry.GetAllAppKeys();

        // Assert
        appKeys.Should().HaveCount(3);
        appKeys.Should().Contain("app-001");
        appKeys.Should().Contain("app-002");
        appKeys.Should().Contain("app-003");
    }

    [Fact]
    public void HasInterceptors_WithNoInterceptors_ShouldReturnFalse()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();

        // Act
        var hasInterceptors = registry.HasInterceptors("app-001");

        // Assert
        hasInterceptors.Should().BeFalse();
    }

    [Fact]
    public void HasInterceptors_WithRegisteredInterceptors_ShouldReturnTrue()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        registry.Register("app-001", typeof(ITestInterceptor));

        // Act
        var hasInterceptors = registry.HasInterceptors("app-001");

        // Assert
        hasInterceptors.Should().BeTrue();
    }

    [Fact]
    public void HasInterceptors_WithUnregisteredApp_ShouldReturnFalse()
    {
        // Arrange
        var registry = new FeishuWebhookInterceptorRegistry();
        registry.Register("app-001", typeof(ITestInterceptor));

        // Act
        var hasInterceptors = registry.HasInterceptors("app-002");

        // Assert
        hasInterceptors.Should().BeFalse();
    }

    // Test interceptor interfaces for testing
    private interface ITestInterceptor : IFeishuEventInterceptor { }
    private interface ITestInterceptor2 : IFeishuEventInterceptor { }
}
