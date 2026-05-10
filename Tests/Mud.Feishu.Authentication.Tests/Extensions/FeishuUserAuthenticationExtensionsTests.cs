// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Authentication;
using System.Security.Claims;
using Xunit;

namespace Mud.Feishu.Authentication.Tests.Extensions;

/// <summary>
/// FeishuUserAuthenticationExtensions 单元测试
/// </summary>
public class FeishuUserAuthenticationExtensionsTests
{
    #region AddFeishuUserContext Tests

    [Fact]
    public void AddFeishuUserContext_RegistersAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFeishuUserContext();

        // Assert
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IFeishuCurrentUserContext));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(typeof(CurrentUserContext), descriptor.ImplementationType);
    }

    [Fact]
    public void AddFeishuUserContext_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFeishuUserContext();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddFeishuUserContext_CanResolveIFeishuCurrentUserContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var context = serviceProvider.GetService<IFeishuCurrentUserContext>();

        // Assert
        Assert.NotNull(context);
        Assert.IsType<CurrentUserContext>(context);
    }

    [Fact]
    public void AddFeishuUserContext_MultipleCallsReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var context1 = serviceProvider.GetService<IFeishuCurrentUserContext>();
        var context2 = serviceProvider.GetService<IFeishuCurrentUserContext>();

        // Assert
        Assert.Same(context1, context2);
    }

    [Fact]
    public void AddFeishuUserContext_TryAddSingleton_DoesNotOverrideExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        var customContext = new Mock<IFeishuCurrentUserContext>().Object;
        services.AddSingleton<IFeishuCurrentUserContext>(customContext);

        // Act
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Should still return the custom implementation
        var context = serviceProvider.GetService<IFeishuCurrentUserContext>();
        Assert.Same(customContext, context);
    }

    #endregion

    #region AddFeishuUserContext with Options Tests

    [Fact]
    public void AddFeishuUserContext_WithOptions_RegistersOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFeishuUserContext(options =>
        {
            options.OpenIdClaimType = "custom_open_id";
            options.EnableSensitiveLog = true;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<FeishuUserAuthenticationOptions>>();
        Assert.NotNull(options);
        Assert.Equal("custom_open_id", options.Value.OpenIdClaimType);
        Assert.True(options.Value.EnableSensitiveLog);
    }

    [Fact]
    public void AddFeishuUserContext_WithOptions_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddFeishuUserContext(_ => { });

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddFeishuUserContext_DefaultOptions_HasCorrectDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<FeishuUserAuthenticationOptions>>().Value;

        // Assert
        Assert.Equal("open_id", options.OpenIdClaimType);
        Assert.Equal(ClaimTypes.NameIdentifier, options.OpenIdFallbackClaimType);
        Assert.Equal("union_id", options.UnionIdClaimType);
        Assert.Equal("user_id", options.UserIdClaimType);
        Assert.Equal(ClaimTypes.Name, options.NameClaimType);
        Assert.True(options.EnableDistributedTracing);
        Assert.False(options.EnableSensitiveLog);
    }

    #endregion

    #region UseFeishuUserAuthentication Tests

    [Fact]
    public void UseFeishuUserAuthentication_ReturnsApplicationBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        var appBuilder = new Mock<IApplicationBuilder>();
        appBuilder.SetupGet(x => x.ApplicationServices).Returns(serviceProvider);
        appBuilder.Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(appBuilder.Object);

        // Act
        var result = appBuilder.Object.UseFeishuUserAuthentication();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void UseFeishuUserAuthentication_RegistersMiddleware()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        Func<RequestDelegate, RequestDelegate>? registeredMiddleware = null;

        var appBuilder = new Mock<IApplicationBuilder>();
        appBuilder.SetupGet(x => x.ApplicationServices).Returns(serviceProvider);
        appBuilder.Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Callback<Func<RequestDelegate, RequestDelegate>>(middleware => registeredMiddleware = middleware)
            .Returns(appBuilder.Object);

        // Act
        appBuilder.Object.UseFeishuUserAuthentication();

        // Assert
        Assert.NotNull(registeredMiddleware);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_FullWorkflow_WorksCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var context = serviceProvider.GetRequiredService<IFeishuCurrentUserContext>();
        context.SetUser("test_open_id", "test_union_id", "test_user_id", "Test User");

        // Assert
        Assert.Equal("test_open_id", context.OpenId);
        Assert.Equal("test_union_id", context.UnionId);
        Assert.Equal("test_user_id", context.UserId);
        Assert.Equal("Test User", context.Name);
        Assert.True(context.IsAuthenticated);

        // Cleanup
        context.Clear();
        Assert.False(context.IsAuthenticated);
    }

    [Fact]
    public void Integration_ServiceCollectionChaining_WorksCorrectly()
    {
        // Arrange & Act
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuUserContext()
            .AddSingleton<TestClass>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var testClass = serviceProvider.GetRequiredService<TestClass>();
        Assert.NotNull(testClass.UserContext);
    }

    [Fact]
    public void Integration_WithOptions_MiddlewareReceivesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeishuUserContext(options =>
        {
            options.OpenIdClaimType = "my_open_id";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var options = serviceProvider.GetRequiredService<IOptions<FeishuUserAuthenticationOptions>>();

        // Assert
        Assert.Equal("my_open_id", options.Value.OpenIdClaimType);
    }

    #endregion

    #region Helper Classes

    private class TestClass
    {
        public IFeishuCurrentUserContext UserContext { get; }

        public TestClass(IFeishuCurrentUserContext userContext)
        {
            UserContext = userContext;
        }
    }

    #endregion
}
