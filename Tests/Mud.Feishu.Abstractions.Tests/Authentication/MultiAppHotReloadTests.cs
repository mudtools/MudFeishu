// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Tests.Helpers;
using Xunit;

namespace Mud.Feishu.Abstractions.Tests.Authentication;

/// <summary>
/// 验证多应用配置热更新（ARC-1）。
/// </summary>
/// <remarks>
/// 修复前 <c>FeishuAppManager</c> 在构造时捕获启动期配置快照、不订阅
/// <c>IOptionsMonitor&lt;List&lt;FeishuAppConfig&gt;&gt;.OnChange</c>，修改配置后管理器与
/// 所有 HttpClient 仍使用旧配置（文档中明确记录「配置变更需重启」）。
/// </remarks>
public class MultiAppHotReloadTests
{
    private const string SectionName = "FeishuApps";

    private static FeishuAppConfig CreateConfig(
        string appKey,
        string appId,
        string appSecret,
        bool isDefault = false,
        int timeOut = 30) => new()
        {
            AppKey = appKey,
            AppId = appId,
            AppSecret = appSecret,
            IsDefault = isDefault,
            TimeOut = timeOut
        };

    private static (ServiceProvider Provider, FeishuAppManager Manager) CreateManager(params FeishuAppConfig[] configs)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configs.ToList());
        var provider = services.BuildServiceProvider();
        var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();
        return (provider, manager);
    }

    [Fact]
    public void OnConfigurationChanged_ShouldAddNewApp_WhenAppKeyIsNew()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)));

        ctx.Manager.HasApp("app2").Should().BeFalse();

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
        });

        ctx.Manager.HasApp("app2").Should().BeTrue();
        ctx.Manager.GetAllApps().Select(a => a.Config.AppKey).Should().Contain("app2");
    }

    [Fact]
    public void OnConfigurationChanged_ShouldRemoveApp_WhenAppDisappearsFromConfig()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)));

        ctx.Manager.HasApp("app2").Should().BeTrue();

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)
        });

        ctx.Manager.HasApp("app2").Should().BeFalse();
    }

    [Fact]
    public void OnConfigurationChanged_ShouldRebuildContext_WhenAppConfigUpdated()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 30)));

        var before = ctx.Manager.GetApp("app1");
        before.Config.TimeOut.Should().Be(30);

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 60)
        });

        var after = ctx.Manager.GetApp("app1");
        after.Should().NotBeSameAs(before, "配置变更必须重建应用上下文，否则仍会走旧配置");
        after.Config.TimeOut.Should().Be(60);
    }

    [Fact]
    public void OnConfigurationChanged_ShouldSwitchDefaultApp_WhenIsDefaultMoves()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr, isDefault: false)));

        ctx.Manager.GetDefaultApp().Config.AppKey.Should().Be("app1");

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: false),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr, isDefault: true)
        });

        ctx.Manager.GetDefaultApp().Config.AppKey.Should().Be("app2");
    }

    [Fact]
    public void OnConfigurationChanged_ShouldNotRebuild_WhenSnapshotIsIdentical()
    {
        var snapshot = new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)
        };
        using var ctx = new ProviderScope(CreateManager(snapshot.ToArray()));

        var before = ctx.Manager.GetApp("app1");

        // 连续两次推送完全相同的快照：节流应阻止重建
        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
        {
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)
        });

        ctx.Manager.GetApp("app1").Should().BeSameAs(before);
    }

    [Fact]
    public void OnConfigurationChanged_ShouldIgnoreEmptyConfig_WhenNoAppRemains()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true)));

        ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>());

        ctx.Manager.HasApp("app1").Should().BeTrue("空配置会清空全部应用，应被忽略以保持服务可用");
    }

    [Fact]
    public void OnConfigurationChanged_ShouldNotThrow_WhenConcurrentWithReads()
    {
        using var ctx = new ProviderScope(CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true),
            CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)));

        var errors = new System.Collections.Concurrent.ConcurrentBag<Exception>();

        Parallel.For(0, 64, i =>
        {
            try
            {
                if (i % 4 == 0)
                {
                    ctx.Manager.OnConfigurationChanged(new List<FeishuAppConfig>
                    {
                        CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true, timeOut: 30 + i),
                        CreateConfig("app2", TestDataFactory.AppConfigs.AppIds.Hr, TestDataFactory.AppConfigs.Secrets.Hr)
                    });
                }
                else
                {
                    _ = ctx.Manager.GetApp("app1");
                    _ = ctx.Manager.GetAllApps().ToList();
                }
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        });

        errors.Should().BeEmpty("热更新与并发读取必须线程安全，且旧上下文延迟回收不得抛 ObjectDisposedException");
    }

    [Fact]
    public void FeishuAppOptions_ShouldDefaultToEnabledReload()
    {
        var options = new FeishuAppOptions();

        options.EnableConfigReload.Should().BeTrue("本项目无存量行为需要保持，默认应启用热更新");
    }

    [Fact]
    public void AddFeishuApp_ShouldReloadManager_WhenConfigurationReloaded()
    {
        var data = new Dictionary<string, string?>
        {
            [$"{SectionName}:0:AppKey"] = "app1",
            [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
            [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
            [$"{SectionName}:0:IsDefault"] = "true"
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);

        var provider = services.BuildServiceProvider();
        try
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

            manager.GetApp("app1").Config.TimeOut.Should().Be(30);

            // 通过 IConfiguration 真实触发 IOptionsMonitor.OnChange
            var memoryProvider = configuration.Providers
                .OfType<MemoryConfigurationProvider>()
                .First();
            memoryProvider.Set($"{SectionName}:0:TimeOut", "90");
            ((IConfigurationRoot)configuration).Reload();

            manager.GetApp("app1").Config.TimeOut.Should().Be(90,
                "启用热更新后，IConfiguration 变更必须传导到 FeishuAppManager");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void AddFeishuApp_ShouldNotReloadManager_WhenReloadDisabled()
    {
        var data = new Dictionary<string, string?>
        {
            [$"{SectionName}:0:AppKey"] = "app1",
            [$"{SectionName}:0:AppId"] = TestDataFactory.AppConfigs.AppIds.Default,
            [$"{SectionName}:0:AppSecret"] = TestDataFactory.AppConfigs.Secrets.Default,
            [$"{SectionName}:0:IsDefault"] = "true"
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(data).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(configuration, SectionName);
        services.Configure<FeishuAppOptions>(o => o.EnableConfigReload = false);

        var provider = services.BuildServiceProvider();
        try
        {
            var manager = (FeishuAppManager)provider.GetRequiredService<IFeishuAppManager>();

            var memoryProvider = configuration.Providers
                .OfType<MemoryConfigurationProvider>()
                .First();
            memoryProvider.Set($"{SectionName}:0:TimeOut", "90");
            ((IConfigurationRoot)configuration).Reload();

            manager.GetApp("app1").Config.TimeOut.Should().Be(30,
                "EnableConfigReload = false 时应保持旧语义：配置变更需重启");
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        var (provider, manager) = CreateManager(
            CreateConfig("app1", TestDataFactory.AppConfigs.AppIds.Default, TestDataFactory.AppConfigs.Secrets.Default, isDefault: true));

        var act = () =>
        {
            manager.Dispose();
            manager.Dispose();
        };

        act.Should().NotThrow();
        provider.Dispose();
    }

    private sealed class ProviderScope : IDisposable
    {
        public ProviderScope((ServiceProvider Provider, FeishuAppManager Manager) pair)
        {
            Provider = pair.Provider;
            Manager = pair.Manager;
        }

        public ServiceProvider Provider { get; }

        public FeishuAppManager Manager { get; }

        public void Dispose() => Provider.Dispose();
    }
}
