// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Authentication;
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
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ICurrentUserContext));
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
    public void AddFeishuUserContext_CanResolveICurrentUserContext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var context = serviceProvider.GetService<ICurrentUserContext>();

        // Assert
        Assert.NotNull(context);
        Assert.IsType<CurrentUserContext>(context);
    }

    [Fact]
    public void AddFeishuUserContext_MultipleCallsReturnSameInstance()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var context1 = serviceProvider.GetService<ICurrentUserContext>();
        var context2 = serviceProvider.GetService<ICurrentUserContext>();

        // Assert
        Assert.Same(context1, context2);
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
        services.AddFeishuUserContext();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var context = serviceProvider.GetRequiredService<ICurrentUserContext>();
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
        var services = new ServiceCollection()
            .AddFeishuUserContext()
            .AddSingleton<TestClass>();

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var testClass = serviceProvider.GetRequiredService<TestClass>();
        Assert.NotNull(testClass.UserContext);
    }

    #endregion

    #region Helper Classes

    private class TestClass
    {
        public ICurrentUserContext UserContext { get; }

        public TestClass(ICurrentUserContext userContext)
        {
            UserContext = userContext;
        }
    }

    #endregion
}
