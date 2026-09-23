// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.Authentication;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

using static Mud.Feishu.Abstractions.Tests.Helpers.TestDataFactory;

/// <summary>
/// P2-3：FeishuAppManager 并发懒初始化压力测试。
/// 验证多线程并发调用 GetApp 时，同一 appKey 返回同一 FeishuAppContext 实例
/// （Lazy&lt;T&gt; 原子占位 + ReferenceEquals 身份校验，TMA-08/I14）。
/// </summary>
public class FeishuAppManagerParallelTests
{
    private static ServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient());

        services.AddSingleton(httpClientFactoryMock.Object);
        services.AddSingleton<IFeishuAuthentication>(new Mock<IFeishuAuthentication>().Object);
        services.AddSingleton<IFeishuCurrentUserContext, CurrentUserContext>();
        services.AddSingleton(_ => HttpClientExtensions.GetDefaultJsonSerializerOptions());
        services.AddLogging();
        services.AddMemoryCache();
        services.AddTransient<IHttpRequestExecutor>(sp => new Mock<IHttpRequestExecutor>().Object);
        services.TryAddSingleton<IFeishuTokenStoreFactory, PerAppFeishuTokenStoreFactory>();
        services.TryAddSingleton<IFeishuTokenManagerFactory, DefaultFeishuTokenManagerFactory>();

        return services;
    }

    [Fact]
    public void GetApp_ShouldReturnSameInstance_WhenCalledConcurrentlyFromMultipleThreads()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = AppConfigs.AppKeys.App1,
                AppId = AppConfigs.AppIds.App1,
                AppSecret = AppConfigs.Secrets.App1,
                IsDefault = true
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        const int threadCount = 32;
        var results = new IFeishuAppContext?[threadCount];
        var exceptions = new Exception?[threadCount];

        // Act：多线程并发调用 GetApp
        Parallel.For(0, threadCount, i =>
        {
            try
            {
                results[i] = appManager.GetApp(AppConfigs.AppKeys.App1);
            }
            catch (Exception ex)
            {
                exceptions[i] = ex;
            }
        });

        // Assert：无异常
        foreach (var ex in exceptions)
        {
            ex.Should().BeNull("并发调用不应抛出异常");
        }

        // 所有结果应为同一实例
        results[0].Should().NotBeNull();
        for (var i = 1; i < threadCount; i++)
        {
            ReferenceEquals(results[0], results[i]).Should().BeTrue(
                $"并发调用 GetApp 应返回同一实例（index {i}）");
        }
    }

    [Fact]
    public void GetApp_ShouldReturnSameInstance_WhenCalledConcurrentlyForMultipleAppKeys()
    {
        // Arrange
        var services = CreateServiceCollection();
        var configs = new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = AppConfigs.AppKeys.App1,
                AppId = AppConfigs.AppIds.App1,
                AppSecret = AppConfigs.Secrets.App1,
                IsDefault = true
            },
            new()
            {
                AppKey = AppConfigs.AppKeys.App2,
                AppId = AppConfigs.AppIds.App2,
                AppSecret = AppConfigs.Secrets.App2
            }
        };

        services.AddSingleton<IFeishuAppManager>(sp => new FeishuAppManager(
            sp,
            configs,
            sp.GetRequiredService<ILogger<FeishuAppManager>>()));

        using var provider = services.BuildServiceProvider();
        var appManager = provider.GetRequiredService<IFeishuAppManager>();

        const int threadCount = 32;
        var app1Results = new IFeishuAppContext?[threadCount];
        var app2Results = new IFeishuAppContext?[threadCount];
        var exceptions = new ConcurrentQueue<Exception>();

        // Act：多线程并发调用两个 appKey
        Parallel.For(0, threadCount, i =>
        {
            try
            {
                app1Results[i] = appManager.GetApp(AppConfigs.AppKeys.App1);
                app2Results[i] = appManager.GetApp(AppConfigs.AppKeys.App2);
            }
            catch (Exception ex)
            {
                exceptions.Enqueue(ex);
            }
        });

        // Assert
        exceptions.Should().BeEmpty();

        // 同一 appKey 的所有结果应为同一实例
        app1Results[0].Should().NotBeNull();
        app2Results[0].Should().NotBeNull();
        for (var i = 1; i < threadCount; i++)
        {
            ReferenceEquals(app1Results[0], app1Results[i]).Should().BeTrue(
                $"App1 并发调用应返回同一实例（index {i}）");
            ReferenceEquals(app2Results[0], app2Results[i]).Should().BeTrue(
                $"App2 并发调用应返回同一实例（index {i}）");
        }

        // 不同 appKey 间应返回不同实例
        ReferenceEquals(app1Results[0], app2Results[0]).Should().BeFalse(
            "不同 appKey 应返回不同实例");
    }
}
